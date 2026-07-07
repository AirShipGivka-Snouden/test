using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Streams;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.WebSockets;

namespace VpnHood.Core.Tunneling.Channels.Streams;

public class WebSocketStream : ChunkStream, IPreservedChunkStream
{
	private readonly bool _isServer;

	private const int ChunkHeaderLength = 14;

	private int _remainingChunkBytes;

	private bool _finished;

	private Exception? _exception;

	private ValueTask<int> _readTask;

	private ValueTask _writeTask;

	private bool _isConnectionClosed;

	private readonly Memory<byte> _readChunkHeaderBuffer = new byte[14];

	private readonly Memory<byte> _writeChunkHeaderBuffer = new byte[14];

	private Task? _closeStreamTask;

	private readonly Lock _closeStreamLock = new Lock();

	private int _isDisposed;

	public override bool CanReuse
	{
		get
		{
			if (!_isConnectionClosed && _exception == null)
			{
				return AllowReuse;
			}
			return false;
		}
	}

	public int PreserveWriteBufferLength => 14;

	public WebSocketStream(Stream sourceStream, string streamId, bool useBuffer, bool isServer)
		: base(useBuffer ? new ReadBufferedStream(sourceStream, leaveOpen: false, 512) : sourceStream, streamId)
	{
		_isServer = isServer;
	}

	private WebSocketStream(Stream sourceStream, string streamId, int reusedCount, bool isServer)
		: base(sourceStream, streamId, reusedCount)
	{
		_isServer = isServer;
	}

	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		cancellationToken.ThrowIfCancellationRequested();
		ObjectDisposedException.ThrowIf(base.IsDisposed || _isDisposed == 1, this);
		try
		{
			_readTask = ((buffer.Length == 0) ? SourceStream.ReadAsync(buffer, cancellationToken) : ReadInternalAsync(buffer, cancellationToken));
			return await _readTask.Vhc();
		}
		catch (Exception ex)
		{
			CloseByError(ex);
			throw;
		}
	}

	private async ValueTask<int> ReadInternalAsync(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		if (_finished)
		{
			return 0;
		}
		if (_remainingChunkBytes == 0)
		{
			WebSocketHeader webSocketHeader = await ReadChunkHeaderAsync(cancellationToken).Vhc();
			if (webSocketHeader.PayloadLength > int.MaxValue)
			{
				throw new InvalidOperationException($"WebSocket payload length is too big: {webSocketHeader.PayloadLength}. StreamId: {base.StreamId}");
			}
			_remainingChunkBytes = (int)webSocketHeader.PayloadLength;
			if (_remainingChunkBytes == 0)
			{
				_finished = true;
				return 0;
			}
		}
		int length = Math.Min(_remainingChunkBytes, buffer.Length);
		int num = await SourceStream.ReadAsync(buffer.Slice(0, length), cancellationToken).Vhc();
		if (num == 0)
		{
			throw new Exception("WebSocketStream has been closed unexpectedly.");
		}
		_remainingChunkBytes -= num;
		if (_remainingChunkBytes == 0)
		{
			base.ReadChunkCount++;
		}
		return num;
	}

	public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		cancellationToken.ThrowIfCancellationRequested();
		ObjectDisposedException.ThrowIf(base.IsDisposed || _isDisposed == 1, this);
		try
		{
			_writeTask = ((buffer.Length == 0) ? SourceStream.WriteAsync(buffer, cancellationToken) : WriteInternalAsync(buffer, cancellationToken));
			await _writeTask.Vhc();
		}
		catch (Exception ex)
		{
			CloseByError(ex);
			throw;
		}
	}

	public async ValueTask WritePreservedAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		cancellationToken.ThrowIfCancellationRequested();
		ObjectDisposedException.ThrowIf(base.IsDisposed || _isDisposed == 1, this);
		try
		{
			_writeTask = ((buffer.Length == PreserveWriteBufferLength) ? SourceStream.WriteAsync(buffer, cancellationToken) : WriteInternalPreserverAsync(buffer, cancellationToken));
			await _writeTask.Vhc();
		}
		catch (Exception ex)
		{
			CloseByError(ex);
			throw;
		}
	}

	private async ValueTask WriteInternalPreserverAsync(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		if (buffer.Length < PreserveWriteBufferLength)
		{
			throw new ArgumentException("Buffer length is less than required preserved header length.", "buffer");
		}
		Span<byte> span = _writeChunkHeaderBuffer.Span;
		int payloadLength = buffer.Length - PreserveWriteBufferLength;
		Span<byte> buffer2 = stackalloc byte[4];
		Span<byte> span2 = BuildHeader(span, payloadLength, GenerateNewMaskKey(buffer2));
		Memory<byte> memory = buffer.Slice(PreserveWriteBufferLength - span2.Length);
		span2.CopyTo(memory.Span);
		await SourceStream.WriteAsync(memory, cancellationToken).Vhc();
		base.WroteChunkCount++;
	}

	private async ValueTask WriteInternalAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
	{
		int num = 14 + buffer.Length;
		using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(num);
		Memory<byte> buffer2 = memoryOwner.Memory.Slice(0, num);
		buffer.CopyTo(buffer2.Slice(14));
		await WriteInternalPreserverAsync(buffer2, cancellationToken);
	}

	private Span<byte> BuildHeader(Span<byte> header, int payloadLength, ReadOnlySpan<byte> maskKey)
	{
		if (!_isServer)
		{
			return WebSocketUtils.BuildWebSocketFrameHeader(header, payloadLength, maskKey);
		}
		return WebSocketUtils.BuildWebSocketFrameHeader(header, payloadLength);
	}

	private static Span<byte> GenerateNewMaskKey(Span<byte> buffer)
	{
		if (buffer.Length < 4)
		{
			throw new ArgumentException("Buffer must be at least 4 bytes long.", "buffer");
		}
		Random shared = Random.Shared;
		for (int i = 0; i < 4; i++)
		{
			buffer[i] = (byte)shared.Next(0, 256);
		}
		return buffer;
	}

	private void CloseByError(Exception ex)
	{
		if (_exception == null)
		{
			_exception = ex;
			Dispose();
		}
	}

	private async Task<WebSocketHeader> ReadChunkHeaderAsync(CancellationToken cancellationToken)
	{
		_ = 2;
		try
		{
			WebSocketHeader webSocketHeader;
			while (true)
			{
				webSocketHeader = await WebSocketUtils.ReadWebSocketHeader(SourceStream, _readChunkHeaderBuffer, cancellationToken);
				if (webSocketHeader.IsCloseConnection)
				{
					VhLogger.Instance.LogDebug(GeneralEventId.Stream, "WebSocketStream has been closed by WebSocket close frame. StreamId: {StreamId}", base.StreamId);
					await DiscardWebSocketFrame(webSocketHeader, cancellationToken).Vhc();
					_isConnectionClosed = true;
					return new WebSocketHeader
					{
						IsCloseConnection = true,
						IsBinary = true,
						PayloadLength = 0L
					};
				}
				if (!webSocketHeader.IsPing && !webSocketHeader.IsPing && webSocketHeader.IsBinary)
				{
					break;
				}
				VhLogger.Instance.LogDebug(GeneralEventId.Stream, "WebSocketStream has received a WebSocket frame that is not binary. StreamId: {StreamId}", base.StreamId);
				await DiscardWebSocketFrame(webSocketHeader, cancellationToken).Vhc();
			}
			return webSocketHeader;
		}
		catch (EndOfStreamException)
		{
			VhLogger.Instance.LogDebug(GeneralEventId.Stream, "WebSocketStream has been closed without terminator. StreamId: {StreamId}", base.StreamId);
			_isConnectionClosed = true;
			return new WebSocketHeader
			{
				IsCloseConnection = true,
				IsBinary = true,
				PayloadLength = 0L
			};
		}
	}

	private async Task DiscardWebSocketFrame(WebSocketHeader webSocketHeader, CancellationToken cancellationToken)
	{
		if (webSocketHeader.PayloadLength > int.MaxValue)
		{
			throw new InvalidOperationException($"WebSocket close frame payload length is too big: {webSocketHeader.PayloadLength}. StreamId: {base.StreamId}");
		}
		using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(8192);
		Memory<byte> memory = memoryOwner.Memory;
		int num;
		for (int read = 0; read < webSocketHeader.PayloadLength; read += num)
		{
			int length = (int)Math.Min(webSocketHeader.PayloadLength - read, memory.Length);
			num = await SourceStream.ReadAsync(memory.Slice(0, length), cancellationToken).Vhc();
			if (num == 0)
			{
				break;
			}
		}
	}

	public override async Task<ChunkStream> CreateReuse()
	{
		await DisposeAsync().Vhc();
		if (_exception != null)
		{
			throw _exception;
		}
		if (_isConnectionClosed)
		{
			throw new EndOfStreamException("Could not reuse a WebSocketStream that its underling stream has been closed . StreamId: " + base.StreamId);
		}
		if (!CanReuse)
		{
			throw new InvalidOperationException("Can not reuse the stream.");
		}
		return new WebSocketStream(SourceStream, base.StreamId, base.ReusedCount + 1, _isServer);
	}

	private Task CloseStreamAsync()
	{
		using (_closeStreamLock.EnterScope())
		{
			if (_closeStreamTask == null)
			{
				_closeStreamTask = CloseStreamInternal();
			}
		}
		return _closeStreamTask;
	}

	private async Task CloseStreamInternal()
	{
		try
		{
			using CancellationTokenSource timeoutCts = new CancellationTokenSource(TunnelDefaults.TcpGracefulTimeout);
			await _writeTask.AsTask().WaitAsync(timeoutCts.Token).Vhc();
			await WriteInternalAsync(ReadOnlyMemory<byte>.Empty, timeoutCts.Token).Vhc();
			await _readTask.AsTask().WaitAsync(timeoutCts.Token).Vhc();
			await DiscardRemainingStreamData(timeoutCts.Token).Vhc();
		}
		catch (Exception exception)
		{
			LoggerExtensions.LogDebug(exception: _exception = exception, logger: VhLogger.Instance, eventId: GeneralEventId.Stream, message: "Could not close the stream gracefully. StreamId: {StreamId}", args: new object[1] { base.StreamId });
			await SourceStream.DisposeAsync().Vhc();
			throw;
		}
	}

	private async ValueTask DiscardRemainingStreamData(CancellationToken cancellationToken)
	{
		using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(500);
		int trashedLength = 0;
		while (true)
		{
			int num = await ReadInternalAsync(memoryOwner.Memory, cancellationToken).Vhc();
			if (num == 0)
			{
				break;
			}
			trashedLength += num;
		}
		if (trashedLength > 0)
		{
			VhLogger.Instance.LogDebug(GeneralEventId.Stream, "Trashing unexpected binary stream data. StreamId: {StreamId}, TrashedLength: {TrashedLength}", base.StreamId, trashedLength);
		}
	}

	protected override void Dispose(bool disposing)
	{
		_isDisposed = 1;
		if (disposing)
		{
			if (CanReuse)
			{
				VhUtils.TryInvokeAsync("Closing Stream: " + base.StreamId, (Func<Task>)CloseStreamAsync);
			}
			else
			{
				SourceStream.Dispose();
			}
		}
		base.Dispose(disposing);
	}

	public override async ValueTask DisposeAsync()
	{
		_isDisposed = 1;
		if (CanReuse)
		{
			await VhUtils.TryInvokeAsync((string?)null, (Func<Task>)CloseStreamAsync).Vhc();
		}
		await base.DisposeAsync().Vhc();
	}
}
