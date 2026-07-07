using System;
using System.Buffers.Binary;

namespace VpnHood.Core.Packets;

public class TcpPacket : IChecksumPayloadPacket, IPayloadPacket
{
	private readonly Memory<byte> _buffer;

	public Memory<byte> Buffer => _buffer;

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

	public uint SequenceNumber
	{
		get
		{
			return BinaryPrimitives.ReadUInt32BigEndian(_buffer.Span.Slice(4, 4));
		}
		set
		{
			BinaryPrimitives.WriteUInt32BigEndian(_buffer.Span.Slice(4, 4), value);
		}
	}

	public uint AcknowledgmentNumber
	{
		get
		{
			return BinaryPrimitives.ReadUInt32BigEndian(_buffer.Span.Slice(8, 4));
		}
		set
		{
			BinaryPrimitives.WriteUInt32BigEndian(_buffer.Span.Slice(8, 4), value);
		}
	}

	public byte Flags
	{
		get
		{
			return _buffer.Span[13];
		}
		set
		{
			_buffer.Span[13] = value;
		}
	}

	public bool Urgent
	{
		get
		{
			return (_buffer.Span[13] & 0x20) != 0;
		}
		set
		{
			SetFlag(32, value);
		}
	}

	public bool Acknowledgment
	{
		get
		{
			return (_buffer.Span[13] & 0x10) != 0;
		}
		set
		{
			SetFlag(16, value);
		}
	}

	public bool Push
	{
		get
		{
			return (_buffer.Span[13] & 8) != 0;
		}
		set
		{
			SetFlag(8, value);
		}
	}

	public bool Reset
	{
		get
		{
			return (_buffer.Span[13] & 4) != 0;
		}
		set
		{
			SetFlag(4, value);
		}
	}

	public bool Synchronize
	{
		get
		{
			return (_buffer.Span[13] & 2) != 0;
		}
		set
		{
			SetFlag(2, value);
		}
	}

	public bool Finish
	{
		get
		{
			return (_buffer.Span[13] & 1) != 0;
		}
		set
		{
			SetFlag(1, value);
		}
	}

	public ushort WindowSize
	{
		get
		{
			return BinaryPrimitives.ReadUInt16BigEndian(_buffer.Span.Slice(14, 2));
		}
		set
		{
			BinaryPrimitives.WriteUInt16BigEndian(_buffer.Span.Slice(14, 2), value);
		}
	}

	public ushort Checksum
	{
		get
		{
			return BinaryPrimitives.ReadUInt16BigEndian(_buffer.Span.Slice(16, 2));
		}
		set
		{
			BinaryPrimitives.WriteUInt16BigEndian(_buffer.Span.Slice(16, 2), value);
		}
	}

	public ushort UrgentPointer
	{
		get
		{
			return BinaryPrimitives.ReadUInt16BigEndian(_buffer.Span.Slice(18, 2));
		}
		set
		{
			BinaryPrimitives.WriteUInt16BigEndian(_buffer.Span.Slice(18, 2), value);
		}
	}

	private byte DataOffset => (byte)((_buffer.Span[12] >> 4) * 4);

	public Memory<byte> Options => _buffer.Slice(20, DataOffset - 20);

	public Memory<byte> Payload
	{
		get
		{
			Memory<byte> buffer = _buffer;
			return buffer.Slice(DataOffset);
		}
	}

	public TcpPacket(Memory<byte> buffer)
	{
		if (buffer.Length < 20)
		{
			throw new ArgumentException("Buffer too small for TCP header.", "buffer");
		}
		_buffer = buffer;
	}

	public TcpPacket(Memory<byte> buffer, int optionsLength)
	{
		if (buffer.Length < 20)
		{
			throw new ArgumentException("Buffer too small for TCP header.", "buffer");
		}
		if ((optionsLength < 0 || optionsLength > 40) ? true : false)
		{
			throw new ArgumentOutOfRangeException("optionsLength", "Options length must be between 0 and 40 bytes.");
		}
		if (optionsLength % 4 != 0)
		{
			throw new ArgumentOutOfRangeException("optionsLength", "Options length must be a multiple of 4 bytes.");
		}
		buffer.Span.Clear();
		int num = (20 + optionsLength) / 4;
		buffer.Span[12] = (byte)(num << 4);
		_buffer = buffer;
	}

	private void SetFlag(byte mask, bool value)
	{
		Flags = (value ? ((byte)(Flags | mask)) : ((byte)(Flags & ~mask)));
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
			return PacketUtil.ComputeChecksum(sourceAddress, destinationAddress, 6, _buffer.Span);
		}
		finally
		{
			Checksum = checksum;
		}
	}

	public override string ToString()
	{
		return $"TCP Packet: SrcPort={SourcePort}, DstPort={DestinationPort}, Seq={SequenceNumber}, Ack={AcknowledgmentNumber}, Flags=[Urgent={Urgent}, Ack={Acknowledgment}, Push={Push}, Reset={Reset}, Sync={Synchronize}, Fin={Finish}], PayloadLen={Payload.Length}";
	}
}
