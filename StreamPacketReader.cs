using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Packets;
using VpnHood.Core.Toolkit.Streams;

namespace VpnHood.Core.Tunneling;

public class StreamPacketReader(Stream stream, int bufferSize) : IDisposable
{
	private readonly ReadBufferedStream _stream = new ReadBufferedStream(stream, leaveOpen: true, bufferSize);

	private readonly Memory<byte> _minHeader = new byte[20];

	public async Task<IpPacket?> ReadAsync(CancellationToken cancellationToken)
	{
		try
		{
			await _stream.ReadExactlyAsync(_minHeader, cancellationToken);
		}
		catch (EndOfStreamException)
		{
			return null;
		}
		int num = PacketUtil.ReadPacketLength(_minHeader.Span);
		if (num > 1500)
		{
			throw new InvalidOperationException($"Packet size exceeds the maximum allowed limit. PacketLength: {num}");
		}
		IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(num);
		try
		{
			_minHeader.CopyTo(memoryOwner.Memory);
			ReadBufferedStream stream = _stream;
			Memory<byte> memory = memoryOwner.Memory;
			int length = _minHeader.Length;
			await stream.ReadExactlyAsync(memory.Slice(length, num - length), cancellationToken);
			return PacketBuilder.Attach(memoryOwner);
		}
		catch
		{
			memoryOwner.Dispose();
			throw;
		}
	}

	public void Dispose()
	{
		_stream.Dispose();
	}
}
