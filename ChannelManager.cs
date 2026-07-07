using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Packets;
using VpnHood.Core.Toolkit.Jobs;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Tunneling.Channels;

namespace VpnHood.Core.Tunneling;

internal class ChannelManager : IDisposable
{
	private readonly Lock _channelListLock = new Lock();

	private readonly HashSet<IChannel> _disposingChannels = new HashSet<IChannel>();

	private readonly HashSet<IProxyChannel> _proxyChannels = new HashSet<IProxyChannel>();

	private readonly List<IPacketChannel> _packetChannels = new List<IPacketChannel>();

	private Traffic _trafficUsage = new Traffic();

	private readonly EventHandler<IpPacket> _channelPacketReceived;

	private bool _disposed;

	private readonly Job _cleanupJob;

	[CompilerGenerated]
	private int _003CMaxPacketChannelCount_003Ek__BackingField;

	public IReadOnlyList<IPacketChannel> PacketChannels => _packetChannels;

	public Traffic Traffic
	{
		get
		{
			using (_channelListLock.EnterScope())
			{
				long sent = _trafficUsage.Sent;
				long received = _trafficUsage.Received;
				sent += _proxyChannels.Sum((IProxyChannel x) => x.Traffic.Sent);
				received += _proxyChannels.Sum((IProxyChannel x) => x.Traffic.Received);
				sent += _packetChannels.Sum((IPacketChannel x) => x.Traffic.Sent);
				received += _packetChannels.Sum((IPacketChannel x) => x.Traffic.Received);
				sent += _disposingChannels.Sum((IChannel x) => x.Traffic.Sent);
				received += _disposingChannels.Sum((IChannel x) => x.Traffic.Received);
				return new Traffic
				{
					Sent = sent,
					Received = received
				};
			}
		}
	}

	public int MaxPacketChannelCount
	{
		[CompilerGenerated]
		get
		{
			return _003CMaxPacketChannelCount_003Ek__BackingField;
		}
		set
		{
			if (value < 1)
			{
				throw new ArgumentException("Value must equals or greater than 1", "MaxPacketChannelCount");
			}
			_003CMaxPacketChannelCount_003Ek__BackingField = value;
		}
	}

	public int ProxyChannelCount
	{
		get
		{
			using (_channelListLock.EnterScope())
			{
				return _proxyChannels.Count;
			}
		}
	}

	public int PacketChannelCount
	{
		get
		{
			using (_channelListLock.EnterScope())
			{
				return _packetChannels.Count;
			}
		}
	}

	public ChannelManager(int maxPacketChannelCount, EventHandler<IpPacket> channelPacketReceived)
	{
		MaxPacketChannelCount = maxPacketChannelCount;
		_channelPacketReceived = channelPacketReceived;
		_cleanupJob = new Job(Cleanup, "ChannelManager");
	}

	private void RemoveExcessPacketChannels()
	{
		using (_channelListLock.EnterScope())
		{
			while (_packetChannels.Count > MaxPacketChannelCount)
			{
				RemoveChannel(_packetChannels[0]);
			}
		}
	}

	private bool IsChannelExists(IChannel channel)
	{
		using (_channelListLock.EnterScope())
		{
			if (!(channel is PacketChannel item))
			{
				if (channel is IProxyChannel item2)
				{
					return _proxyChannels.Contains(item2);
				}
				throw new ArgumentOutOfRangeException("channel");
			}
			return _packetChannels.Contains(item);
		}
	}

	public void AddChannel(IChannel channel)
	{
		using (_channelListLock.EnterScope())
		{
			if (!(channel is PacketChannel channel2))
			{
				if (!(channel is IProxyChannel channel3))
				{
					throw new ArgumentOutOfRangeException("channel");
				}
				AddProxyChannel(channel3);
			}
			else
			{
				AddPacketChannel(channel2);
			}
		}
	}

	private void AddPacketChannel(IPacketChannel channel)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("Tunnel");
		}
		if (IsChannelExists(channel))
		{
			throw new Exception("the channel already exists in the collection.");
		}
		channel.PacketReceived += _channelPacketReceived;
		channel.Start();
		using (_channelListLock.EnterScope())
		{
			_packetChannels.Add(channel);
			VhLogger.Instance.LogDebug(GeneralEventId.PacketChannel, "A channel has been added. Channel: {Channel}, ChannelId: {ChannelId}, ChannelCount: {ChannelCount}, State: {State}", VhLogger.FormatType(channel), channel.ChannelId, _packetChannels.Count, channel.State);
		}
		RemoveExcessPacketChannels();
	}

	private void AddProxyChannel(IProxyChannel channel)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("Tunnel");
		}
		if (IsChannelExists(channel))
		{
			throw new Exception("the channel already exists in the collection.");
		}
		channel.Start();
		using (_channelListLock.EnterScope())
		{
			_proxyChannels.Add(channel);
			VhLogger.Instance.LogDebug(GeneralEventId.ProxyChannel, "A channel has been added. Channel: {Channel}, ChannelId: {ChannelId}, ChannelCount: {ChannelCount}, State: {State}", VhLogger.FormatType(channel), channel.ChannelId, _proxyChannels.Count, channel.State);
		}
	}

	public void RemoveChannel(IChannel channel, bool dispose = true)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (!(channel is PacketChannel packetChannel))
		{
			if (!(channel is IProxyChannel channel2))
			{
				throw new ArgumentOutOfRangeException("channel");
			}
			RemoveChannel(_proxyChannels, channel2, GeneralEventId.ProxyChannel, dispose);
		}
		else
		{
			packetChannel.PacketReceived -= _channelPacketReceived;
			RemoveChannel(_packetChannels, packetChannel, GeneralEventId.PacketChannel, dispose);
		}
	}

	private void RemoveChannel<T>(ICollection<T> channels, T channel, EventId eventId, bool dispose) where T : IChannel
	{
		using (_channelListLock.EnterScope())
		{
			_disposingChannels.Remove(channel);
			channels.Remove(channel);
			_trafficUsage += channel.Traffic;
			VhLogger.Instance.LogDebug(eventId, "A channel has been removed. Channel: {Channel}, ChannelId: {ChannelId}, ChannelCount: {ChannelCount}, State: {State}", VhLogger.FormatType(channel), channel.ChannelId, channels.Count, channel.State);
		}
		if (dispose)
		{
			channel.Dispose();
		}
	}

	private ValueTask Cleanup(CancellationToken cancellationToken)
	{
		CleanupChannels();
		return default(ValueTask);
	}

	public void CleanupChannels()
	{
		CleanupChannels(_proxyChannels);
		CleanupChannels(_packetChannels);
		using (_channelListLock.EnterScope())
		{
			IChannel[] array = _disposingChannels.Where((IChannel x) => x.State == PacketChannelState.Disposed).ToArray();
			foreach (IChannel channel in array)
			{
				RemoveChannel(channel);
			}
		}
	}

	private void CleanupChannels<T>(ICollection<T> channels) where T : IChannel
	{
		using (_channelListLock.EnterScope())
		{
			T[] array = channels.Where((T x) => x.State != PacketChannelState.Connected).ToArray();
			foreach (T val in array)
			{
				_disposingChannels.Add(val);
				channels.Remove(val);
			}
		}
	}

	public Task RunJob()
	{
		CleanupChannels();
		return Task.CompletedTask;
	}

	public IPacketChannel GetPacketChannel(int channelIndex)
	{
		using (_channelListLock.EnterScope())
		{
			if (channelIndex >= _packetChannels.Count)
			{
				channelIndex = _packetChannels.Count - 1;
			}
			if (channelIndex < 0)
			{
				throw new ArgumentOutOfRangeException("channelIndex", "Channel index is out of range.");
			}
			return _packetChannels[channelIndex];
		}
	}

	public void RemoveChannels<T>(bool dispose = true) where T : IChannel
	{
		IPacketChannel[] array = _packetChannels.Where((IPacketChannel x) => x is T).ToArray();
		foreach (IPacketChannel channel in array)
		{
			RemoveChannel(channel, dispose);
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		using (_channelListLock.EnterScope())
		{
			IProxyChannel[] array = _proxyChannels.ToArray();
			foreach (IProxyChannel channel in array)
			{
				RemoveChannel(channel);
			}
			IPacketChannel[] array2 = _packetChannels.ToArray();
			foreach (IPacketChannel channel2 in array2)
			{
				RemoveChannel(channel2);
			}
			foreach (IChannel disposingChannel in _disposingChannels)
			{
				if (disposingChannel is IPacketChannel packetChannel)
				{
					packetChannel.PacketReceived -= _channelPacketReceived;
				}
				disposingChannel.Dispose();
			}
			_disposingChannels.Clear();
		}
		_cleanupJob.Dispose();
		_disposed = true;
	}
}
