using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Toolkit.Streams;

public class ReadBufferedStream : StreamDecoratorAsync
{
	private readonly Memory<byte> _buffer;

	private int _bufferRemain;

	private int _bufferOffset;

	private const int DefaultBufferSize = 1024;

	public override bool CanSeek => false;

	public bool AllowBufferRefill { get; set; } = true;

	public override bool? DataAvailable
	{
		get
		{
			if (_bufferRemain <= 0)
			{
				return base.DataAvailable;
			}
			return true;
		}
	}

	public override long Position
	{
		get
		{
			return base.Position - _bufferRemain;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public ReadBufferedStream(Stream sourceStream, bool leaveOpen, ReadOnlySpan<byte> initData)
		: this(sourceStream, leaveOpen, initData.Length, initData)
	{
		if (initData.Length == 0)
		{
			throw new ArgumentException("Cache data cannot be empty when using this constructor.", "initData");
		}
	}

	public ReadBufferedStream(Stream sourceStream, bool leaveOpen, int bufferSize = 1024, ReadOnlySpan<byte> initData = default(ReadOnlySpan<byte>))
		: base(sourceStream, leaveOpen)
	{
		if (bufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("bufferSize", "Cache size must be greater than zero.");
		}
		if (!initData.IsEmpty && initData.Length > bufferSize)
		{
			throw new ArgumentOutOfRangeException("initData", "Initial cache data exceeds cache size.");
		}
		_buffer = new byte[bufferSize];
		initData.CopyTo(_buffer.Span);
		_bufferRemain = initData.Length;
	}

	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_bufferRemain == 0 && (buffer.Length > _buffer.Length || !AllowBufferRefill))
		{
			return await base.ReadAsync(buffer, cancellationToken).Vhc();
		}
		if (_bufferRemain == 0 && buffer.Length <= _buffer.Length)
		{
			_bufferRemain = await base.ReadAsync(_buffer, cancellationToken).Vhc();
			if (_bufferRemain == 0)
			{
				return 0;
			}
			_bufferOffset = 0;
		}
		int num = Math.Min(buffer.Length, _bufferRemain);
		_buffer.Slice(_bufferOffset, num).CopyTo(buffer);
		_bufferOffset += num;
		_bufferRemain -= num;
		return num;
	}
}
