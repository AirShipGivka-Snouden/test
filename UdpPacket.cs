using System;
using System.Buffers.Binary;

namespace VpnHood.Core.Packets;

public class UdpPacket : IChecksumPayloadPacket, IPayloadPacket
{
	private readonly Memory<byte> _buffer;

	public Memory<byte> Buffer => _buffer;

	public Memory<byte> Payload
	{
		get
		{
			Memory<byte> buffer = _buffer;
			return buffer.Slice(8);
		}
	}

	public ushort SourcePort
	{
		get
		{
			return BinaryPrimitives.ReadUInt16BigEndian(_buffer.Span.Slice(0, 2));
		}
		set
		{
			BinaryPrimitives.WriteUInt16BigEndian(_buffer.Span.Slice(0, 2), value);
		}
	}

	public ushort DestinationPort
	{
		get
		{
			return BinaryPrimitives.ReadUInt16BigEndian(_buffer.Span.Slice(2, 2));
		}
		set
		{
			BinaryPrimitives.WriteUInt16BigEndian(_buffer.Span.Slice(2, 2), value);
		}
	}

	public ushort Checksum
	{
		get
		{
			return BinaryPrimitives.ReadUInt16BigEndian(_buffer.Span.Slice(6, 2));
		}
		set
		{
			BinaryPrimitives.WriteUInt16BigEndian(_buffer.Span.Slice(6, 2), value);
		}
	}

	public UdpPacket(Memory<byte> buffer, bool building)
	{
		if (buffer.Length < 8)
		{
			throw new ArgumentException("Buffer too small for UDP header.", "buffer");
		}
		if (building)
		{
			buffer.Span.Clear();
			BinaryPrimitives.WriteUInt16BigEndian(buffer.Span.Slice(4, 2), (ushort)buffer.Length);
		}
		else if (BinaryPrimitives.ReadUInt16BigEndian(buffer.Span.Slice(4, 2)) != buffer.Length)
		{
			throw new ArgumentException("Buffer length does not match UDP length field.");
		}
		_buffer = buffer;
	}

	public bool IsChecksumValid(ReadOnlySpan<byte> sourceAddress, ReadOnlySpan<byte> destinationAddress)
	{
		return ComputeChecksum(sourceAddress, destinationAddress) == Checksum;
	}

	public void UpdateChecksum(ReadOnlySpan<byte> sourceAddress, ReadOnlySpan<byte> destinationAddress)
	{
		Checksum = ComputeChecksum(sourceAddress, destinationAddress);
	}

	public ushort ComputeChecksum(ReadOnlySpan<byte> sourceAddress, ReadOnlySpan<byte> destinationAddress)
	{
		ushort checksum = Checksum;
		Checksum = 0;
		try
		{
			return PacketUtil.ComputeChecksum(sourceAddress, destinationAddress, 17, _buffer.Span);
		}
		finally
		{
			Checksum = checksum;
		}
	}

	public override string ToString()
	{
		return $"UDP Packet: SrcPort={SourcePort}, DstPort={DestinationPort}, PayloadLen={Payload.Length}";
	}
}
