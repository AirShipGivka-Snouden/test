using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.TcpStack;

public sealed class LocalTcpStream : Stream
{
	private readonly LocalTcpConnection _connection;

	private readonly LocalTcpStack _stack;

	private readonly CancellationTokenSource _cts = new CancellationTokenSource();

	private bool _disposed;

	private bool IsDisposed => Volatile.Read(in _disposed);

	public override bool CanRead => !IsDisposed;

	public override bool CanWrite => !IsDisposed;

	public override bool CanSeek => false;

	public override long Length
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override long Position
	{
		get
		{
			throw new NotSupportedException();
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	internal LocalTcpStream(LocalTcpConnection connection, LocalTcpStack stack)
	{
		_connection = connection;
		_stack = stack;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException("Read is not supported in synchronous mode. Use ReadAsync instead.");
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
	}

	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		CancellationToken cancellationToken2 = (cancellationToken.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken).Token : _cts.Token);
		try
		{
			PipeReader reader = _connection.NetToAppReader;
			ReadResult readResult = await reader.ReadAsync(cancellationToken2);
			if (readResult.IsCanceled || (readResult.IsCompleted && readResult.Buffer.IsEmpty))
			{
				return 0;
			}
			int num = (int)Math.Min(buffer.Length, readResult.Buffer.Length);
			readResult.Buffer.Slice(0, num).CopyTo(buffer.Span);
			reader.AdvanceTo(readResult.Buffer.GetPosition(num));
			return num;
		}
		catch (OperationCanceledException) when (_cts.IsCancellationRequested)
		{
			return 0;
		}
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException("Write is not supported in synchronous mode. Use WriteAsync instead.");
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
	}

	public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		CancellationToken ct = (cancellationToken.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken).Token : _cts.Token);
		try
		{
			await _connection.SendAppDataAsync(buffer, ct);
		}
		catch (OperationCanceledException) when (_cts.IsCancellationRequested)
		{
		}
	}

	public override void Flush()
	{
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

	protected override void Dispose(bool disposing)
	{
		if (!Interlocked.Exchange(ref _disposed, value: true))
		{
			if (disposing)
			{
				_cts.TryCancel();
				_connection.TryStartFin(_stack);
				_cts.Dispose();
			}
			base.Dispose(disposing);
		}
	}

	public override async ValueTask DisposeAsync()
	{
		if (!Interlocked.Exchange(ref _disposed, value: true))
		{
			await VhUtils.TryInvokeAsync("GracefulCloseAsync", () => _connection.GracefulCloseAsync(_stack)).ConfigureAwait(continueOnCapturedContext: false);
			await _cts.TryCancelAsync();
			_cts.Dispose();
			base.Dispose(disposing: false);
		}
	}
}
