using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.PacketTransports;
using VpnHood.Core.Packets;
using VpnHood.Core.Toolkit.Jobs;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.DatagramMessaging;

namespace VpnHood.Core.Tunneling.Channels;

public abstract class PacketChannel : PacketTransport, IPacketChannel, IPacketTransport, IDisposable, IChannel
{
	private readonly TimeSpan? _lifespan;

	private DateTime? _closeSentTime;

	private DateTime? _closeReceivedTime;

	private bool _started;

	private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

	private readonly Job _checkLifetimeJob;

	protected TrafficMeter? TrafficMeter { get; }

	protected CancellationToken CancellationToken => _cancellationTokenSource.Token;

	protected override string Name { get; }

	public string ChannelId { get; }

	public DateTime LastActivityTime => base.PacketStat.LastActivityTime;

	public Traffic Traffic => new Traffic(base.PacketStat.SentBytes, base.PacketStat.ReceivedBytes);

	public abstract int OverheadLength { get; }

	public PacketChannelState State
	{
		get
		{
			if (base.IsDisposed)
			{
				return PacketChannelState.Disposed;
			}
			if (_closeSentTime.HasValue || _closeReceivedTime.HasValue)
			{
				return PacketChannelState.Disconnecting;
			}
			if (!_started)
			{
				return PacketChannelState.NotStarted;
			}
			return PacketChannelState.Connected;
		}
	}

	protected abstract Task StartReadTask();

	protected PacketChannel(PacketChannelOptions options)
		: base(new PacketTransportOptions
		{
			AutoDisposePackets = options.AutoDisposePackets,
			Blocking = options.Blocking,
			QueueCapacity = options.QueueCapacity
		})
	{
		_lifespan = options.Lifespan;
		TrafficMeter = options.TrafficMeter;
		Name = VhLogger.FormatType(this) + ": " + options.ChannelId;
		ChannelId = options.ChannelId;
		_checkLifetimeJob = new Job(CheckLifetime, "PacketChannel");
	}

	public void Start()
	{
		if (base.IsDisposed)
		{
			throw new ObjectDisposedException(GetType().Name);
		}
		if (_started)
		{
			throw new InvalidOperationException("The packet channel is already started.");
		}
		VhLogger.Instance.LogDebug(GeneralEventId.PacketChannel, "Starting a PacketChannel. ChannelId: {ChannelId}", ChannelId);
		StartTask();
		_started = true;
	}

	protected void Stop()
	{
		_cancellationTokenSource.TryCancel();
	}

	private async Task StartTask()
	{
		try
		{
			await StartReadTask();
			VhLogger.Instance.LogDebug(GeneralEventId.PacketChannel, "PacketChannel read task completed. ChannelId: {ChannelId}, Type: {Type}", ChannelId, VhLogger.FormatType(this));
		}
		catch (OperationCanceledException) when (base.IsDisposed || _cancellationTokenSource.IsCancellationRequested)
		{
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogError(GeneralEventId.PacketChannel, exception, "PacketChannel read task failed. ChannelId: {ChannelId}, Type: {Type}", ChannelId, VhLogger.FormatType(this));
		}
		finally
		{
			Dispose();
		}
	}

	private ValueTask CheckLifetime(CancellationToken cancellationToken)
	{
		if (base.IsDisposed)
		{
			throw new ObjectDisposedException("CheckLifetime");
		}
		if (_cancellationTokenSource.IsCancellationRequested)
		{
			return default(ValueTask);
		}
		if (_started && !_closeSentTime.HasValue && _lifespan.HasValue && FastDateTime.Now - base.PacketStat.CreatedTime > _lifespan.Value)
		{
			VhLogger.Instance.LogDebug(GeneralEventId.PacketChannel, "PacketChannel lifetime is over. ChannelId: {ChannelId}, CreatedTime: {CreatedTime}, Lifespan: {Lifespan}", ChannelId, base.PacketStat.CreatedTime, _lifespan);
			SendPacketQueued(PacketMessageHandler.CreateMessage(new ClosePacketMessage()));
			_closeSentTime = FastDateTime.Now;
		}
		DateTime now = FastDateTime.Now;
		DateTime? closeSentTime = _closeSentTime;
		if (now - closeSentTime > TunnelDefaults.TcpGracefulTimeout)
		{
			Dispose();
		}
		return default(ValueTask);
	}

	protected override void OnPacketReceived(IpPacket ipPacket)
	{
		if (PacketMessageHandler.ReadMessage(ipPacket) is ClosePacketMessage)
		{
			VhLogger.Instance.LogDebug(GeneralEventId.PacketChannel, "PacketChannel received close message. ChannelId: {ChannelId}, CreatedTime: {CreatedTime}, Lifespan: {Lifespan}", ChannelId, base.PacketStat.CreatedTime, _lifespan);
			if (!_closeReceivedTime.HasValue)
			{
				DateTime valueOrDefault = _closeReceivedTime.GetValueOrDefault();
				if (!_closeReceivedTime.HasValue)
				{
					valueOrDefault = FastDateTime.Now;
					_closeReceivedTime = valueOrDefault;
				}
				valueOrDefault = _closeSentTime.GetValueOrDefault();
				if (!_closeSentTime.HasValue)
				{
					valueOrDefault = FastDateTime.Now;
					_closeSentTime = valueOrDefault;
				}
				SendPacketQueued(PacketMessageHandler.CreateMessage(new ClosePacketMessage()));
			}
			ipPacket.Dispose();
		}
		else
		{
			base.OnPacketReceived(ipPacket);
		}
	}

	protected override void DisposeManaged()
	{
		Stop();
		_checkLifetimeJob.Dispose();
		_cancellationTokenSource.TryCancel();
		_cancellationTokenSource.Dispose();
		base.DisposeManaged();
	}
}
