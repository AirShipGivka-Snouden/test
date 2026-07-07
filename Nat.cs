using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Packets;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Tunneling;

public class Nat(bool isDestinationSensitive) : IDisposable
{
	private readonly Lock _lockObject = new Lock();

	private readonly Dictionary<(IpVersion, IpProtocol), ushort> _lastNatIds = new Dictionary<(IpVersion, IpProtocol), ushort>();

	private readonly Dictionary<(IpVersion, IpProtocol, ushort), NatItem> _map = new Dictionary<(IpVersion, IpProtocol, ushort), NatItem>();

	private readonly Dictionary<NatItem, NatItem> _mapR = new Dictionary<NatItem, NatItem>();

	private bool _disposed;

	private DateTime _lastCleanupTime = FastDateTime.Now;

	public TimeSpan TcpTimeout { get; set; } = TimeSpan.FromMinutes(15L);

	public TimeSpan UdpTimeout { get; set; } = TimeSpan.FromMinutes(2L);

	public TimeSpan IcmpTimeout { get; set; } = TimeSpan.FromSeconds(30L);

	public int ItemCount
	{
		get
		{
			using (_lockObject.EnterScope())
			{
				return _map.Count;
			}
		}
	}

	public int GetItemCount(IpProtocol protocol)
	{
		using (_lockObject.EnterScope())
		{
			return _map.Count<KeyValuePair<(IpVersion, IpProtocol, ushort), NatItem>>((KeyValuePair<(IpVersion, IpProtocol, ushort), NatItem> x) => x.Value.Protocol == protocol);
		}
	}

	private NatItem CreateNatItemFromPacket(IpPacket ipPacket)
	{
		if (!isDestinationSensitive)
		{
			return new NatItem(ipPacket);
		}
		return new NatItemEx(ipPacket);
	}

	private bool IsExpired(NatItem natItem)
	{
		if (natItem.Protocol == IpProtocol.Tcp)
		{
			return FastDateTime.Now - natItem.AccessTime > TcpTimeout;
		}
		IpProtocol protocol = natItem.Protocol;
		if ((protocol == IpProtocol.IcmpV4 || protocol == IpProtocol.IcmpV6) ? true : false)
		{
			return FastDateTime.Now - natItem.AccessTime > IcmpTimeout;
		}
		return FastDateTime.Now - natItem.AccessTime > UdpTimeout;
	}

	public void Cleanup()
	{
		if (!(FastDateTime.Now - _lastCleanupTime < IcmpTimeout))
		{
			_lastCleanupTime = FastDateTime.Now;
			NatItem[] array;
			using (_lockObject.EnterScope())
			{
				array = _mapR.Values.Where(IsExpired).ToArray();
			}
			NatItem[] array2 = array;
			foreach (NatItem natItem in array2)
			{
				Remove(natItem);
			}
		}
	}

	private void Remove(NatItem natItem)
	{
		NatItem value;
		using (_lockObject.EnterScope())
		{
			_mapR.Remove(natItem, out value);
			_map.Remove((natItem.IpVersion, natItem.Protocol, natItem.NatId), out NatItem _);
		}
		if (value != null && VhLogger.MinLogLevel == LogLevel.Trace)
		{
			VhLogger.Instance.LogTrace(GeneralEventId.Nat, "NatItem has been removed. {NatItem}", value);
		}
	}

	private ushort GetFreeNatId(IpVersion ipVersion, IpProtocol protocol)
	{
		(IpVersion, IpProtocol) key = (ipVersion, protocol);
		using (_lockObject.EnterScope())
		{
			if (!_lastNatIds.TryGetValue(key, out var value))
			{
				value = 8000;
			}
			if (value > 65534)
			{
				value = 0;
			}
			for (ushort num = (ushort)(value + 1); num != value; num++)
			{
				if (num == 0)
				{
					num++;
				}
				if (!_map.ContainsKey((ipVersion, protocol, num)))
				{
					_lastNatIds[key] = num;
					return num;
				}
			}
		}
		throw new OverflowException("No more free NatId is available.");
	}

	public NatItem? Get(IpPacket ipPacket)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("Nat");
		}
		NatItem key = CreateNatItemFromPacket(ipPacket);
		using (_lockObject.EnterScope())
		{
			if (!_mapR.TryGetValue(key, out NatItem value))
			{
				return null;
			}
			value.AccessTime = FastDateTime.Now;
			return value;
		}
	}

	public NatItem GetOrAdd(IpPacket ipPacket)
	{
		using (_lockObject.EnterScope())
		{
			return Get(ipPacket) ?? Add(ipPacket);
		}
	}

	public NatItem Add(IpPacket ipPacket, bool overwrite = false)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("Nat");
		}
		ushort freeNatId = GetFreeNatId(ipPacket.Version, ipPacket.Protocol);
		return Add(ipPacket, freeNatId, overwrite);
	}

	public NatItem Add(IpPacket ipPacket, ushort natId, bool overwrite = false)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("Nat");
		}
		Cleanup();
		NatItem natItem = CreateNatItemFromPacket(ipPacket);
		natItem.NatId = natId;
		try
		{
			using (_lockObject.EnterScope())
			{
				if (_disposed)
				{
					throw new ObjectDisposedException("Nat");
				}
				_map.Add((natItem.IpVersion, natItem.Protocol, natItem.NatId), natItem);
				_mapR.Add(natItem, natItem);
			}
		}
		catch (ArgumentException) when (overwrite)
		{
			Remove(natItem);
			using (_lockObject.EnterScope())
			{
				if (_disposed)
				{
					throw new ObjectDisposedException("Nat");
				}
				_map.Add((natItem.IpVersion, natItem.Protocol, natItem.NatId), natItem);
				_mapR.Add(natItem, natItem);
			}
		}
		if (VhLogger.MinLogLevel <= LogLevel.Trace)
		{
			VhLogger.Instance.LogTrace(GeneralEventId.Nat, "New NAT record. NatItem: {NatItem}", natItem);
		}
		return natItem;
	}

	public void RemoveOldest(IpProtocol protocol)
	{
		using (_lockObject.EnterScope())
		{
			NatItem natItem = _map.Values.FirstOrDefault();
			if (natItem == null)
			{
				return;
			}
			foreach (NatItem item in _map.Values.Where((NatItem x) => x.Protocol == protocol))
			{
				if (item.AccessTime < natItem.AccessTime)
				{
					natItem = item;
				}
			}
			Remove(natItem);
		}
	}

	public NatItem? Resolve(IpVersion ipVersion, IpProtocol protocol, ushort id)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("Nat");
		}
		using (_lockObject.EnterScope())
		{
			(IpVersion, IpProtocol, ushort) key = (ipVersion, protocol, id);
			if (!_map.TryGetValue(key, out NatItem value))
			{
				return null;
			}
			value.AccessTime = FastDateTime.Now;
			return value;
		}
	}

	public void RemoveAll()
	{
		NatItem[] array;
		using (_lockObject.EnterScope())
		{
			array = _mapR.Values.ToArray();
		}
		NatItem[] array2 = array;
		foreach (NatItem natItem in array2)
		{
			Remove(natItem);
		}
	}

	public void Dispose()
	{
		using (_lockObject.EnterScope())
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
		}
		RemoveAll();
	}
}
