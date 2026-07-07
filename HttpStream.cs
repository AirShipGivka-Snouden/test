using System;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Streams;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.Utils;

namespace VpnHood.Core.Tunneling.Channels.Streams;

public class HttpStream : ChunkStream
{
	private readonly byte[] _newLineBytes = "\r\n"u8.ToArray();

	private int _remainingChunkBytes;

	private readonly byte[] _chunkHeaderBuffer = new byte[10];

	private readonly byte[] _nextLineBuffer = new byte[2];

	private bool _isHttpHeaderSent;

	private bool _isHttpHeaderRead;

	private bool _isFinished;

	private bool _hasError;

	private readonly string? _host;

	private readonly CancellationTokenSource _readCts = new CancellationTokenSource();

	private readonly CancellationTokenSource _writeCts = new CancellationTokenSource();

	private ValueTask<int> _readTask;

	private ValueTask _writeTask;

	private bool _isConnectionClosed;

	private bool _keepOpen;

	private bool _disposed;

	private readonly AsyncLock _disposeLock = new AsyncLock();

	private bool IsServer => _host == null;

	public override bool CanReuse
	{
		get
		{
			if (!_hasError)
			{
				return !_isConnectionClosed;
			}
			return false;
		}
	}

	public HttpStream(Stream sourceStream, string streamId, string? host, bool keepSourceOpen = false)
		: base(new ReadBufferedStream(sourceStream, keepSourceOpen, 512), streamId)
	{
		_host = host;
	}

	private HttpStream(Stream sourceStream, string streamId, string? host)
		: base(sourceStream, streamId)
	{
		_host = host;
	}

	private string CreateHttpHeader()
	{
		if (IsServer)
		{
			return "HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nCache-Control: no-store\r\nTransfer-Encoding: chunked\r\n\r\n";
		}
		return $"POST /{Guid.NewGuid()} HTTP/1.1\r\nHost: {_host}\r\n" + "Content-Type: application/octet-stream\r\nCache-Control: no-store\r\nTransfer-Encoding: chunked\r\n\r\n";
	}

	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().Name);
		}
		using CancellationTokenSource tokenSource = CancellationTokenSource.CreateLinkedTokenSource(_readCts.Token, cancellationToken);
		_readTask = ReadInternalAsync(buffer, tokenSource.Token);
		return await _readTask.Vhc();
	}

	private async ValueTask<int> ReadInternalAsync(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		_ = 2;
		try
		{
			if (!_isHttpHeaderRead)
			{
				using MemoryStream memoryStream = await HttpUtils.ReadHeadersAsync(SourceStream, cancellationToken).Vhc();
				if (memoryStream.Length == 0L)
				{
					_isFinished = true;
					_isConnectionClosed = true;
					return 0;
				}
				_isHttpHeaderRead = true;
			}
			if (_isFinished)
			{
				return 0;
			}
			if (_remainingChunkBytes == 0)
			{
				_remainingChunkBytes = await ReadChunkHeaderAsync(cancellationToken).Vhc();
			}
			_isFinished = _remainingChunkBytes == 0;
			if (_isFinished)
			{
				return 0;
			}
			int length = Math.Min(_remainingChunkBytes, buffer.Length);
			int num = await SourceStream.ReadAsync(buffer.Slice(0, length), cancellationToken).Vhc();
			if (num == 0 && buffer.Length != 0)
			{
				throw new Exception("HttpStream has been closed unexpectedly.");
			}
			_remainingChunkBytes -= num;
			return num;
		}
		catch (Exception ex)
		{
			CloseByError(ex);
			throw;
		}
	}

	private void CloseByError(Exception ex)
	{
		if (!_hasError)
		{
			_hasError = true;
			VhLogger.LogError(GeneralEventId.Stream, ex, "Disposing HttpStream. StreamId: {StreamId}", base.StreamId);
			DisposeAsync();
		}
	}

	private async Task<int> ReadChunkHeaderAsync(CancellationToken cancellationToken)
	{
		if (base.ReadChunkCount != 0)
		{
			await ReadNextLine(cancellationToken).Vhc();
		}
		int bufferOffset = 0;
		while (!cancellationToken.IsCancellationRequested)
		{
			if (bufferOffset == _chunkHeaderBuffer.Length)
			{
				throw new InvalidDataException("Chunk header exceeds the maximum size.");
			}
			if (await SourceStream.ReadAsync(_chunkHeaderBuffer, bufferOffset, 1, cancellationToken).Vhc() == 0)
			{
				throw new InvalidDataException("Could not read HTTP Chunk header.");
			}
			if (bufferOffset > 0 && _chunkHeaderBuffer[bufferOffset - 1] == 13 && _chunkHeaderBuffer[bufferOffset] == 10)
			{
				break;
			}
			bufferOffset++;
		}
		if (!int.TryParse(Encoding.ASCII.GetString(_chunkHeaderBuffer, 0, bufferOffset - 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var chunkSize))
		{
			throw new InvalidDataException("Invalid HTTP chunk size.");
		}
		if (chunkSize == 0)
		{
			await ReadNextLine(cancellationToken).Vhc();
		}
		else
		{
			base.ReadChunkCount++;
		}
		return chunkSize;
	}

	public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().Name);
		}
		if (_isFinished)
		{
			throw new SocketException(10053);
		}
		using CancellationTokenSource tokenSource = CancellationTokenSource.CreateLinkedTokenSource(_writeCts.Token, cancellationToken);
		try
		{
			_writeTask = ((buffer.Length == 0) ? SourceStream.WriteAsync(buffer, tokenSource.Token) : WriteInternalAsync(buffer, tokenSource.Token));
			await _writeTask.Vhc();
		}
		catch (Exception ex)
		{
			CloseByError(ex);
			throw;
		}
	}

	private async ValueTask WriteInternalAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
	{
		_ = 4;
		try
		{
			if (!_isHttpHeaderSent)
			{
				await SourceStream.WriteAsync(Encoding.UTF8.GetBytes(CreateHttpHeader()), cancellationToken).Vhc();
				_isHttpHeaderSent = true;
			}
			byte[] bytes = Encoding.ASCII.GetBytes(buffer.Length.ToString("X") + "\r\n");
			await SourceStream.WriteAsync(bytes, cancellationToken).Vhc();
			await SourceStream.WriteAsync(buffer, cancellationToken).Vhc();
			await SourceStream.WriteAsync(_newLineBytes, cancellationToken).Vhc();
			await FlushAsync(cancellationToken).Vhc();
			base.WroteChunkCount++;
		}
		catch (Exception ex)
		{
			CloseByError(ex);
			throw;
		}
	}

	private async Task ReadNextLine(CancellationToken cancellationToken)
	{
		if (await SourceStream.ReadAsync(_nextLineBuffer, 0, 2, cancellationToken).Vhc() < 2 || _nextLineBuffer[0] != 13 || _nextLineBuffer[1] != 10)
		{
			throw new InvalidDataException("Could not find expected line feed in HTTP chunk header.");
		}
	}

	public override async Task<ChunkStream> CreateReuse()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().Name);
		}
		if (_hasError)
		{
			throw new InvalidOperationException("Could not reuse a HttpStream that has error. StreamId: " + base.StreamId);
		}
		if (_isConnectionClosed)
		{
			throw new InvalidOperationException("Could not reuse a HttpStream that its underling stream has been closed . StreamId: " + base.StreamId);
		}
		_keepOpen = true;
		await DisposeAsync().Vhc();
		if (_isFinished && !_hasError)
		{
			return new HttpStream(SourceStream, base.StreamId, _host);
		}
		await base.DisposeAsync().Vhc();
		throw new InvalidOperationException("Could not reuse a HttpStream that has not been closed gracefully. StreamId: " + base.StreamId);
	}

	private async Task CloseStream(CancellationToken cancellationToken)
	{
		_readCts.CancelAfter(TunnelDefaults.TcpGracefulTimeout);
		_writeCts.CancelAfter(TunnelDefaults.TcpGracefulTimeout);
		try
		{
			await _writeTask.Vhc();
		}
		catch
		{
		}
		_writeCts.Dispose();
		try
		{
			await WriteInternalAsync(Memory<byte>.Empty, cancellationToken).Vhc();
		}
		catch (Exception ex)
		{
			VhLogger.LogError(GeneralEventId.Stream, ex, "Could not write the HTTP chunk terminator. StreamId: {StreamId}", base.StreamId);
		}
		try
		{
			await _readTask.Vhc();
		}
		catch
		{
		}
		try
		{
			if (_hasError)
			{
				throw new InvalidOperationException("Could not close a HttpStream due internal error.");
			}
			if (_remainingChunkBytes != 0)
			{
				throw new InvalidOperationException("Attempt to dispose a HttpStream before finishing the current chunk.");
			}
			if (!_isFinished)
			{
				Memory<byte> buffer = new byte[10];
				if (await ReadInternalAsync(buffer, cancellationToken).Vhc() != 0)
				{
					throw new InvalidDataException("HttpStream read unexpected data on end.");
				}
			}
		}
		catch (Exception ex2)
		{
			VhLogger.LogError(GeneralEventId.Stream, ex2, "HttpStream has not been closed gracefully. StreamId: {StreamId}", base.StreamId);
		}
	}

	public override async ValueTask DisposeAsync()
	{
		using (await _disposeLock.LockAsync().Vhc())
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			if (!_hasError && !_isConnectionClosed)
			{
				using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TunnelDefaults.TcpGracefulTimeout);
				await CloseStream(cancellationTokenSource.Token).Vhc();
			}
			if (!_keepOpen)
			{
				await base.DisposeAsync().Vhc();
			}
		}
	}
}
