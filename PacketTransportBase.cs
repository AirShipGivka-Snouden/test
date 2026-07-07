using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Packets;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.PacketTransports;

public abstract class PacketTransportBase : IPacketTransport, IDisposable
{
	private readonly Channel<IpPacket> _sendChannel;

	private readonly int _queueCapacity;

	private readonly bool _autoDisposePackets;

	private readonly bool _blocking;

	private readonly bool _singleMode;

	private readonly bool _passthrough;

	private int _isDisposed;

	private int _isDisposing;

	private readonly PacketTransportStat _stat = new PacketTransportStat();

	private bool _isSending;

	private readonly IpPacket[] _singlePacketBuffer = new IpPacket[1];

	protected bool IsDisposed => _isDisposed == 1;

	protected bool IsDisposing => _isDisposing == 1;

	protected virtual string Name => VhLogger.FormatType(this);

	public ReadOnlyPacketTransportStat PacketStat { get; }

	public int QueueLength => _sendChannel.Reader.Count;

	public bool IsSending
	{
		get
		{
			if (!_isSending)
			{
				return QueueLength > 0;
			}
			return true;
		}
	}

	public event EventHandler<IpPacket>? PacketReceived;

	protected abstract ValueTask SendPacketsAsync(IReadOnlyList<IpPacket> ipPackets);

	protected PacketTransportBase(PacketTransportOptions options, bool singleMode, bool passthrough)
	{
		if (passthrough && !singleMode)
		{
			throw new ArgumentException("Passthrough mode should be used with single mode only.", "passthrough");
		}
		_queueCapacity = options.QueueCapacity ?? 255;
		_autoDisposePackets = options.AutoDisposePackets;
		_blocking = options.Blocking;
		_singleMode = singleMode;
		_passthrough = passthrough;
		_sendChannel = Channel.CreateBounded<IpPacket>(new BoundedChannelOptions(_queueCapacity)
		{
			SingleReader = true,
			SingleWriter = false,
			FullMode = ((!options.Blocking) ? BoundedChannelFullMode.DropWrite : BoundedChannelFullMode.Wait)
		});
		PacketStat = new ReadOnlyPacketTransportStat(_stat);
		Task.Run((Func<Task?>)StartSendingPacketsAsync);
	}

	protected virtual void OnPacketReceived(IpPacket ipPacket)
	{
		try
		{
			ObjectDisposedException.ThrowIf(IsDisposed || IsDisposing, this);
			_stat.LastReceivedTime = FastDateTime.Now;
			_stat.ReceivedBytes += ipPacket.PacketLength;
			_stat.ReceivedPackets++;
			LogPacket(ipPacket, "Received a packet.");
			this.PacketReceived?.Invoke(this, ipPacket);
		}
		catch (Exception exception)
		{
			LogPacket(ipPacket, exception, "Error while invoking the received packets.");
			if (_autoDisposePackets)
			{
				ipPacket.Dispose();
			}
		}
	}

	private async ValueTask SendPacketQueuedPassthroughAsync(IpPacket ipPacket)
	{
		ObjectDisposedException.ThrowIf(IsDisposed || IsDisposing, this);
		_singlePacketBuffer[0] = ipPacket;
		await SendPacketsInternalAsync(_singlePacketBuffer);
	}

	public ValueTask SendPacketQueuedAsync(IpPacket ipPacket)
	{
		ObjectDisposedException.ThrowIf(IsDisposed || IsDisposing, this);
		if (!_passthrough)
		{
			return _sendChannel.Writer.WriteAsync(ipPacket);
		}
		return SendPacketQueuedPassthroughAsync(ipPacket);
	}

	public bool SendPacketQueued(IpPacket ipPacket)
	{
		ObjectDisposedException.ThrowIf(IsDisposed || IsDisposing, this);
		LogPacket(ipPacket, "Sending a packet to queue.");
		if (_passthrough)
		{
			lock (_singlePacketBuffer)
			{
				_singlePacketBuffer[0] = ipPacket;
				ValueTask<bool> valueTask = SendPacketsInternalAsync(_singlePacketBuffer);
				if (!valueTask.IsCompleted)
				{
					throw new InvalidOperationException("A passthrough PacketTransport should not return an incomplete task.");
				}
				return valueTask.GetAwaiter().GetResult();
			}
		}
		if (_sendChannel.Writer.TryWrite(ipPacket))
		{
			return true;
		}
		if (_blocking)
		{
			return SendPacketQueuedBlocking(ipPacket);
		}
		LogPacket(ipPacket, LogLevel.Debug, null, "Dropping a packet. Send queue is full.");
		if (_autoDisposePackets)
		{
			ipPacket.Dispose();
		}
		return false;
	}

	private bool SendPacketQueuedBlocking(IpPacket ipPacket)
	{
		try
		{
			_sendChannel.Writer.WriteAsync(ipPacket).VhBlock();
			return true;
		}
		catch (Exception exception)
		{
			LogPacket(ipPacket, exception, "Dropping packet. Could not write the packet to queue.");
			if (_autoDisposePackets)
			{
				ipPacket.Dispose();
			}
			return false;
		}
	}

	private async Task StartSendingPacketsAsync()
	{
		_ = 1;
		try
		{
			List<IpPacket> ipPackets = new List<IpPacket>(_singleMode ? 1 : _queueCapacity);
			while (await _sendChannel.Reader.WaitToReadAsync() && !IsDisposed && !IsDisposing)
			{
				ipPackets.Clear();
				IpPacket item;
				while (ipPackets.Count < ipPackets.Capacity && _sendChannel.Reader.TryRead(out item))
				{
					ipPackets.Add(item);
				}
				ValueTask<bool> valueTask = SendPacketsInternalAsync(ipPackets);
				if (!valueTask.IsCompleted)
				{
					await valueTask;
				}
			}
			if (_autoDisposePackets)
			{
				IpPacket item2;
				while (_sendChannel.Reader.TryRead(out item2))
				{
					item2.Dispose();
				}
			}
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogError(exception, "Error in SendingPacketsAsync loop. Type: {Type}", VhLogger.FormatType(this));
		}
	}

	private async ValueTask<bool> SendPacketsInternalAsync(IReadOnlyList<IpPacket> ipPackets)
	{
		try
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(VhLogger.FormatType(this));
			}
			_isSending = true;
			_stat.LastSentTime = FastDateTime.Now;
			ValueTask valueTask = SendPacketsAsync(ipPackets);
			if (!valueTask.IsCompletedSuccessfully)
			{
				await valueTask;
			}
			for (int i = 0; i < ipPackets.Count; i++)
			{
				_stat.SentBytes += ipPackets[i].PacketLength;
				_stat.SentPackets++;
				if (_autoDisposePackets)
				{
					ipPackets[i].Dispose();
				}
			}
			return true;
		}
		catch (Exception exception)
		{
			for (int j = 0; j < ipPackets.Count; j++)
			{
				LogPacket(ipPackets[j], exception, "Error in sending packet via channel.");
				_stat.DroppedPackets++;
				if (_autoDisposePackets)
				{
					ipPackets[j].Dispose();
				}
			}
			return false;
		}
		finally
		{
			_isSending = false;
		}
	}

	protected void LogPacket(IpPacket ipPacket, Exception exception, string message)
	{
		LogPacket(ipPacket, LogLevel.Error, exception, message);
	}

	protected void LogPacket(IpPacket ipPacket, string message)
	{
		LogPacket(ipPacket, LogLevel.Trace, null, message);
	}

	protected virtual void LogPacket(IpPacket ipPacket, LogLevel logLevel, Exception? exception, string message, params object?[] args)
	{
		if (VhLogger.MinLogLevel <= LogLevel.Trace)
		{
			ILogger instance = VhLogger.Instance;
			string message2 = $"{Name}: {message} {ipPacket}";
			instance.Log(logLevel, exception, message2, args);
		}
	}

	public virtual void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposing, 1) != 1)
		{
			PreDispose();
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}

	protected void Dispose(bool disposing)
	{
		if (Interlocked.Exchange(ref _isDisposed, 1) != 1)
		{
			if (disposing)
			{
				DisposeManaged();
			}
			DisposeUnmanaged();
			_isDisposing = 0;
		}
	}

	protected virtual void PreDispose()
	{
	}

	protected virtual void DisposeManaged()
	{
		this.PacketReceived = null;
		_sendChannel.Writer.TryComplete();
	}

	protected virtual void DisposeUnmanaged()
	{
	}
}
