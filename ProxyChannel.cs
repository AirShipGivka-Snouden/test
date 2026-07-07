using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Toolkit.Jobs;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.Channels.Streams;
using VpnHood.Core.Tunneling.Connections;

namespace VpnHood.Core.Tunneling.Channels;

public class ProxyChannel : IProxyChannel, IChannel, IDisposable
{
	private int _isDisposed;

	private readonly IStreamConnection _hostStreamConnection;

	private readonly IStreamConnection _tunnelStreamConnection;

	private readonly TransferBufferSize _tunnelBufferSize;

	private const int BufferSizeMax = 81920;

	private const int BufferSizeMin = 2048;

	private bool _started;

	private Traffic _traffic = new Traffic();

	private readonly Lock _trafficLock = new Lock();

	private bool _isTunnelReadTaskFinished;

	private readonly Job _checkAliveJob;

	private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

	private readonly TrafficMeter? _trafficMeter;

	private bool IsDisposed => _isDisposed == 1;

	public DateTime LastActivityTime { get; private set; } = FastDateTime.Now;

	public string ChannelId { get; }

	public Traffic Traffic
	{
		get
		{
			using (_trafficLock.EnterScope())
			{
				return _traffic;
			}
		}
	}

	public PacketChannelState State
	{
		get
		{
			if (IsDisposed)
			{
				return PacketChannelState.Disposed;
			}
			if (!_started)
			{
				return PacketChannelState.NotStarted;
			}
			return PacketChannelState.Connected;
		}
	}

	public ProxyChannel(string channelId, IStreamConnection orgStreamConnection, IStreamConnection tunnelStreamConnection, TransferBufferSize tunnelBufferSize, TrafficMeter? trafficMeter = null)
	{
		_hostStreamConnection = orgStreamConnection;
		_tunnelStreamConnection = tunnelStreamConnection;
		_tunnelBufferSize = tunnelBufferSize;
		_trafficMeter = trafficMeter;
		int receive = _tunnelBufferSize.Receive;
		if ((receive < 2048 || receive > 81920) ? true : false)
		{
			throw new ArgumentOutOfRangeException($"Proxy receive buffer size must be greater than or equal to {2048} and less than {81920}. It was {_tunnelBufferSize.Receive}");
		}
		receive = _tunnelBufferSize.Send;
		if ((receive < 2048 || receive > 81920) ? true : false)
		{
			throw new ArgumentOutOfRangeException($"Proxy send buffer size must be greater than or equal to {2048} and less than {81920}. It was {_tunnelBufferSize.Send}");
		}
		ChannelId = channelId;
		_checkAliveJob = new Job(CheckAlive, TunnelDefaults.TcpCheckInterval, "ProxyChannel");
	}

	public void Start()
	{
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		if (_started)
		{
			throw new InvalidOperationException("ProxyChannel is already started.");
		}
		StartInternal(_cancellationTokenSource.Token);
	}

	private async Task StartInternal(CancellationToken cancellationToken)
	{
		_ = 1;
		try
		{
			_started = true;
			Task tunnelReadTask = CopyFromTunnelAsync(_tunnelStreamConnection.Stream, _hostStreamConnection.Stream, _tunnelBufferSize.Receive, cancellationToken, cancellationToken);
			Task task = CopyToTunnelAsync(_hostStreamConnection.Stream, _tunnelStreamConnection.Stream, _tunnelBufferSize.Send, cancellationToken, cancellationToken);
			_isTunnelReadTaskFinished = await Task.WhenAny(tunnelReadTask, task).Vhc() == tunnelReadTask;
			InlineArray2<Task> buffer = default(InlineArray2<Task>);
			buffer[0] = _hostStreamConnection.Stream.DisposeAsync().AsTask();
			buffer[1] = _tunnelStreamConnection.Stream.DisposeAsync().AsTask();
			await Task.WhenAll(buffer).Vhc();
		}
		catch (Exception ex) when (IsDisposed && VhLogger.IsSocketCloseException(ex))
		{
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogDebug(GeneralEventId.ProxyChannel, exception, "Error while using a ProxyChannel. ChannelId: {ChannelId}, IsDisposed: {IsDisposed}", ChannelId, IsDisposed);
		}
		finally
		{
			Dispose();
		}
	}

	private async Task CopyFromTunnelAsync(Stream source, Stream destination, int bufferSize, CancellationToken sourceCancellationToken, CancellationToken destinationCancellationToken)
	{
		try
		{
			await CopyToInternalAsync(source, destination, isSendingToTunnel: false, bufferSize, sourceCancellationToken, destinationCancellationToken).Vhc();
		}
		catch (Exception ex) when (IsDisposed && VhLogger.IsSocketCloseException(ex))
		{
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogDebug(exception, "ProxyChannel: Error while copying from tunnel. ChannelId: {ChannelId}, IsDisposed: {IsDisposed}", ChannelId, IsDisposed);
			throw;
		}
	}

	private async Task CopyToTunnelAsync(Stream source, Stream destination, int bufferSize, CancellationToken sourceCancellationToken, CancellationToken destinationCancellationToken)
	{
		try
		{
			await CopyToInternalAsync(source, destination, isSendingToTunnel: true, bufferSize, sourceCancellationToken, destinationCancellationToken).Vhc();
		}
		catch (Exception ex) when (IsDisposed && VhLogger.IsSocketCloseException(ex))
		{
		}
		catch (Exception ex2)
		{
			if (_isTunnelReadTaskFinished && VhLogger.IsSocketCloseException(ex2))
			{
				return;
			}
			VhLogger.Instance.LogDebug(ex2, "ProxyChannel: Error while copying to tunnel. ChannelId: {ChannelId}", ChannelId);
			throw;
		}
	}

	private async Task CopyToInternalAsync(Stream source, Stream destination, bool isSendingToTunnel, int bufferSize, CancellationToken sourceCt, CancellationToken destinationCt)
	{
		if (bufferSize > 81920)
		{
			throw new ArgumentException($"Buffer is too big, maximum supported size is {81920}", "bufferSize");
		}
		IPreservedChunkStream destinationPreserved = destination as IPreservedChunkStream;
		int preserveCount = destinationPreserved?.PreserveWriteBufferLength ?? 0;
		Memory<byte> readBuffer = new byte[bufferSize];
		while (!sourceCt.IsCancellationRequested && !destinationCt.IsCancellationRequested)
		{
			int bytesRead = await source.ReadAsync(readBuffer.Slice(preserveCount), sourceCt).Vhc();
			if (bytesRead == 0)
			{
				break;
			}
			if (destinationPreserved != null)
			{
				await destinationPreserved.WritePreservedAsync(readBuffer.Slice(0, preserveCount + bytesRead), destinationCt).Vhc();
			}
			else
			{
				int num = preserveCount;
				await destination.WriteAsync(readBuffer.Slice(num, bytesRead - num), destinationCt).Vhc();
			}
			using (_trafficLock.EnterScope())
			{
				if (isSendingToTunnel)
				{
					_traffic += new Traffic(bytesRead, 0L);
				}
				else
				{
					_traffic += new Traffic(0L, bytesRead);
				}
				LastActivityTime = FastDateTime.Now;
			}
			if (_trafficMeter != null)
			{
				if (isSendingToTunnel)
				{
					_trafficMeter.OnSent(bytesRead);
					await _trafficMeter.ThrottleSendAsync(sourceCt).Vhc();
				}
				else
				{
					_trafficMeter.OnReceived(bytesRead);
					await _trafficMeter.ThrottleReceiveAsync(sourceCt).Vhc();
				}
			}
		}
	}

	private ValueTask CheckAlive(CancellationToken cancellationToken)
	{
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		if (!_started)
		{
			return default(ValueTask);
		}
		if (_hostStreamConnection.Connected && _tunnelStreamConnection.Connected)
		{
			return default(ValueTask);
		}
		VhLogger.Instance.LogInformation(GeneralEventId.ProxyChannel, "Disposing a ProxyChannel due to its error state. ChannelId: {ChannelId}", ChannelId);
		Dispose();
		return default(ValueTask);
	}

	public override string ToString()
	{
		return ChannelId;
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, 1) != 1)
		{
			_cancellationTokenSource.Cancel();
			_cancellationTokenSource.Dispose();
			_checkAliveJob.Dispose();
			_started = false;
			_hostStreamConnection.Dispose();
			_tunnelStreamConnection.Dispose();
		}
	}
}
