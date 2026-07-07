using System;
using System.Buffers;
using System.Buffers.Binary;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Packets;

public class IpV6Packet : IpPacket
{
	private bool _disposed;

	private readonly IMemoryOwner<byte>? _memoryOwner;

	private Span<byte> Span => base.Buffer.Span;

	public override IpProtocol Protocol => NextHeader;

	public IpProtocol NextHeader => (IpProtocol)Span[6];

	public override Memory<byte> Header => base.Buffer.Slice(0, 40);

	public override byte TimeToLive
	{
		get
		{
			return HopLimit;
		}
		set
		{
			HopLimit = value;
		}
	}

	public byte HopLimit
	{
		get
		{
			return Span[7];
		}
		set
		{
			Span[7] = value;
		}
	}

	protected override Span<byte> SourceAddressBuffer
	{
		get
		{
			return Span.Slice(8, 16);
		}
		set
		{
			value.CopyTo(Span.Slice(8, 16));
			SourceAddressField = null;
		}
	}

	protected override Span<byte> DestinationAddressBuffer
	{
		get
		{
			return Span.Slice(24, 16);
		}
		set
		{
			value.CopyTo(Span.Slice(24, 16));
			DestinationAddressField = null;
		}
	}

	public int FlowLabel
	{
		get
		{
			return ((Span[1] & 0xF) << 16) | (Span[2] << 8) | Span[3];
		}
		set
		{
			if ((value < 0 || value > 1048575) ? true : false)
			{
				throw new ArgumentOutOfRangeException("value", "Flow Label must be a 20-bit unsigned value (0 to 1048575).");
			}
			Span[1] = (byte)((Span[1] & 0xF0) | ((value >> 16) & 0xF));
			Span[2] = (byte)((value >> 8) & 0xFF);
			Span[3] = (byte)(value & 0xFF);
		}
	}

	public byte TrafficClass
	{
		get
		{
			return (byte)(((Span[0] & 0xF) << 4) | (Span[1] >> 4));
		}
		set
		{
			Span[0] = (byte)((Span[0] & 0xF0) | (value >> 4));
			Span[1] = (byte)((Span[1] & 0xF) | (value << 4));
		}
	}

	public IpV6Packet(IMemoryOwner<byte> memoryOwner)
		: this(memoryOwner.Memory)
	{
		_memoryOwner = memoryOwner;
	}

	public IpV6Packet(IMemoryOwner<byte> memoryOwner, int packetLength, IpProtocol protocol)
		: this(memoryOwner.Memory.Slice(0, packetLength), protocol)
	{
		_memoryOwner = memoryOwner;
	}

	public IpV6Packet(Memory<byte> buffer, IpProtocol protocol)
		: base(buffer)
	{
		if (buffer.Length < 40)
		{
			throw new ArgumentException("Buffer too small for IPv6 header.");
		}
		buffer.Span.Clear();
		base.Version = IpVersion.IPv6;
		Span[6] = (byte)protocol;
		ushort value = (ushort)(buffer.Length - 40);
		BinaryPrimitives.WriteUInt16BigEndian(Span.Slice(4, 2), value);
	}

	private static Memory<byte> AdjustBuffer(Memory<byte> buffer)
	{
		int packetLength = GetPacketLength(buffer.Span);
		if (packetLength < 40)
		{
			throw new ArgumentException("Invalid IPv6 packet length.", "buffer");
		}
		if (packetLength > buffer.Length)
		{
			throw new ArgumentException("Buffer too small for IPv6 packet.", "buffer");
		}
		return buffer.Slice(0, packetLength);
	}

	public IpV6Packet(Memory<byte> buffer)
		: base(AdjustBuffer(buffer))
	{
		buffer = base.Buffer;
		ushort num = BinaryPrimitives.ReadUInt16BigEndian(buffer.Span.Slice(4, 2));
		if (buffer.Length != 40 + num)
		{
			throw new ArgumentException("Buffer length does not match IPv6 payload length field.");
		}
	}

	public static int GetPacketLength(ReadOnlySpan<byte> buffer)
	{
		if (IpPacket.GetPacketVersion(buffer) != IpVersion.IPv6)
		{
			throw new ArgumentException("Buffer is not an IPv6 packet.", "buffer");
		}
		if (buffer.Length < 6)
		{
			throw new ArgumentException("IPv6 header requires at least 6 bytes to determine packet length.", "buffer");
		}
		ushort num = (ushort)((buffer[4] << 8) | buffer[5]);
		return 40 + num;
	}

	protected override void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			_memoryOwner?.Dispose();
			base.Dispose(disposing);
			_disposed = true;
		}
	}

	~IpV6Packet()
	{
		Dispose(disposing: false);
	}
}
