using System;
using System.Buffers.Binary;

namespace VpnHood.Core.Packets;

public class IcmpV4Packet : IChecksumPayloadPacket, IPayloadPacket
{
	private readonly Memory<byte> _buffer;

	public Memory<byte> Buffer => _buffer;

	public IcmpV4Type Type
	{
		get
		{
			return (IcmpV4Type)_buffer.Span[0];
		}
		set
		{
			_buffer.Span[0] = (byte)value;
		}
	}

	public byte Code
	{
		get
		{
			return _buffer.Span[1];
		}
		set
		{
			_buffer.Span[1] = value;
		}
	}

	public uint MessageSpecific
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

	public ushort Checksum
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

	public ushort Identifier
	{
		get
		{
			if (!IsEcho)
			{
				return 0;
			}
			return BinaryPrimitives.ReadUInt16BigEndian(_buffer.Span.Slice(4, 2));
		}
		set
		{
			if (!IsEcho)
			{
				throw new InvalidOperationException("Identifier is only valid for Echo messages.");
			}
			BinaryPrimitives.WriteUInt16BigEndian(_buffer.Span.Slice(4, 2), value);
		}
	}

	public ushort SequenceNumber
	{
		get
		{
			if (!IsEcho)
			{
				return 0;
			}
			return BinaryPrimitives.ReadUInt16BigEndian(_buffer.Span.Slice(6, 2));
		}
		set
		{
			if (!IsEcho)
			{
				throw new InvalidOperationException("SequenceNumber is only valid for Echo messages.");
			}
			BinaryPrimitives.WriteUInt16BigEndian(_buffer.Span.Slice(6, 2), value);
		}
	}

	public bool IsEcho
	{
		get
		{
			IcmpV4Type type = Type;
			if (type == IcmpV4Type.EchoReply || type == IcmpV4Type.EchoRequest)
			{
				return true;
			}
			return false;
		}
	}

	public Memory<byte> Payload
	{
		get
		{
			Memory<byte> buffer = _buffer;
			return buffer.Slice(8);
		}
	}

	public IcmpV4Packet(Memory<byte> buffer, bool building)
	{
		if (buffer.Length < 8)
		{
			throw new ArgumentException("Buffer too small for ICMPv4 header.");
		}
		if (buffer.Length > 65535)
		{
			throw new ArgumentException("Buffer too large for ICMPv4 packet.");
		}
		if (building)
		{
			buffer.Span.Clear();
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
			Span<byte> span = _buffer.Span;
			span[2] = 0;
			span[3] = 0;
			return ChecksumUtils.OnesComplementSum(span);
		}
		finally
		{
			Checksum = checksum;
		}
	}

	public override string ToString()
	{
		string text = $"ICMPv4 Packet: Type={Type}, Code={Code}, PayloadLen={Payload.Length}";
		if (IsEcho)
		{
			return text + $", Id={Identifier}, Seq={SequenceNumber}";
		}
		return text + $", MsgSpec={MessageSpecific}";
	}
}
