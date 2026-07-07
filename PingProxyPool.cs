using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.PacketTransports;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Collections;
using VpnHood.Core.Toolkit.Jobs;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.Utils;

namespace VpnHood.Core.Tunneling.Proxies;

public class PingProxyPool : PassthroughPacketTransport, IPacketProxyPool, IPacketTransport, IDisposable
{
	private readonly bool _autoDisposeSentPackets;

	private readonly IPacketProxyCallbacks? _packetProxyCallbacks;

	private readonly List<PingProxy> _pingProxies = new List<PingProxy>();

	private readonly EventReporter _maxWorkerEventReporter;

	private readonly TimeoutDictionary<IPEndPoint, TimeoutItem<bool>> _remoteEndPoints;

	private readonly TimeSpan _workerTimeout = TimeSpan.FromMinutes(5L);

	private readonly int _maxClientCount;

	private readonly Job _cleanupJob;

	public DateTime LastUsedTime { get; set; }

	public int RemoteEndPointCount => _remoteEndPoints.Count;

	public int ClientCount
	{
		get
		{
			lock (_pingProxies)
			{
				return _pingProxies.Count;
			}
		}
	}

	public PingProxyPool(PingProxyPoolOptions options)
	{
		_autoDisposeSentPackets = options.AutoDisposePackets;
		_maxClientCount = options.MaxClientCount;
		_packetProxyCallbacks = options.PacketProxyCallbacks;
		_remoteEndPoints = new TimeoutDictionary<IPEndPoint, TimeoutItem<bool>>(options.IcmpTimeout);
		LogScope logScope = options.LogScope;
		_maxWorkerEventReporter = new EventReporter("Session has reached to the maximum ping workers.", default(EventId), logScope);
		_cleanupJob = new Job(Cleanup, "PingProxyPool");
	}

	private PingProxy GetFreePingProxy(out bool isNew)
	{
		lock (_pingProxies)
		{
			isNew = false;
			PingProxy pingProxy = _pingProxies.FirstOrDefault((PingProxy x) => !x.IsSending);
			if (pingProxy != null)
			{
				return pingProxy;
			}
			if (_pingProxies.Count < _maxClientCount)
			{
				pingProxy = new PingProxy(_autoDisposeSentPackets);
				pingProxy.PacketReceived += PingProxy_PacketReceived;
				_pingProxies.Add(pingProxy);
				isNew = true;
				return pingProxy;
			}
			_maxWorkerEventReporter.Raise();
			pingProxy = _pingProxies.OrderBy((PingProxy x) => x.PacketStat.LastActivityTime).First();
			pingProxy.Cancel();
			return pingProxy;
		}
	}

	private void PingProxy_PacketReceived(object? sender, IpPacket ipPacket)
	{
		OnPacketReceived(ipPacket);
	}

	protected override void SendPacket(IpPacket ipPacket)
	{
		if (ipPacket.Version == IpVersion.IPv4 && ipPacket.ExtractIcmpV4().Type != IcmpV4Type.EchoRequest)
		{
			throw new NotSupportedException("The icmp is not supported. Packet: " + PacketLogger.Format(ipPacket) + ".");
		}
		if (ipPacket.Version == IpVersion.IPv6 && ipPacket.ExtractIcmpV6().Type != IcmpV6Type.EchoRequest)
		{
			throw new NotSupportedException("The icmp is not supported. Packet: " + PacketLogger.Format(ipPacket) + ".");
		}
		IPEndPoint iPEndPoint = new IPEndPoint(ipPacket.DestinationAddress, 0);
		bool isNewRemoteEndPoint = false;
		_remoteEndPoints.GetOrAdd(iPEndPoint, delegate
		{
			isNewRemoteEndPoint = true;
			return new TimeoutItem<bool>(value: true);
		});
		if (isNewRemoteEndPoint)
		{
			_packetProxyCallbacks?.OnConnectionRequested(ipPacket.Protocol, iPEndPoint.ToValue());
		}
		GetFreePingProxy(out var isNew).SendPacketQueued(ipPacket);
		if (isNew || isNewRemoteEndPoint)
		{
			_packetProxyCallbacks?.OnConnectionEstablished(ipPacket.Protocol, new IpEndPointValue(ipPacket.SourceAddress, 0), new IpEndPointValue(ipPacket.DestinationAddress, 0), isNew, isNewRemoteEndPoint);
		}
	}

	private ValueTask Cleanup(CancellationToken cancellationToken)
	{
		lock (_pingProxies)
		{
			TimeoutItemUtil.CleanupTimeoutList(_pingProxies, _workerTimeout);
		}
		return default(ValueTask);
	}

	protected override void DisposeManaged()
	{
		lock (_pingProxies)
		{
			foreach (PingProxy pingProxy in _pingProxies)
			{
				pingProxy.PacketReceived -= PingProxy_PacketReceived;
				pingProxy.Dispose();
			}
		}
		_cleanupJob.Dispose();
		_maxWorkerEventReporter.Dispose();
		_remoteEndPoints.Dispose();
		base.DisposeManaged();
	}
}
