using System;
using System.Buffers;
using System.Buffers.Binary;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Packets;

public class IpV4Packet : IpPacket
{
	private bool _disposed;

	private readonly IMemoryOwner<byte>? _memoryOwner;

	private Span<byte> Span => base.Buffer.Span;

	public override Memory<byte> Header => base.Buffer.Slice(0, InternetHeaderLength * 4);

	public override IpProtocol Protocol => (IpProtocol)Span[9];

	public int InternetHeaderLength => Span[0] & 0xF;

	public byte TypeOfService
	{
		get
		{
			return Span[1];
		}
		set
		{
			Span[1] = value;
		}
	}

	public ushort Identification
	{
		get
		{
			return BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(4, 2));
		}
		set
		{
			BinaryPrimitives.WriteUInt16BigEndian(Span.Slice(4, 2), value);
		}
	}

	public byte FragmentFlags
	{
		get
		{
			return (byte)(Span[6] >> 5);
		}
		set
		{
			Span[6] = (byte)((value << 5) | (Span[6] & 0x1F));
		}
	}

	public bool DontFragment
	{
		get
		{
			return (Span[6] & 0x40) != 0;
		}
		set
		{
			if (value)
			{
				Span[6] |= 64;
			}
			else
			{
				Span[6] &= 191;
			}
		}
	}

	public bool MoreFragments
	{
		get
		{
			return (Span[6] & 0x20) != 0;
		}
		set
		{
			if (value)
			{
				Span[6] |= 32;
			}
			else
			{
				Span[6] &= 223;
			}
		}
	}

	public int FragmentOffset
	{
		get
		{
			return (ushort)(BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(6, 2)) & 0x1FFF);
		}
		set
		{
			ushort value2 = (ushort)((ushort)(BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(6, 2)) & 0xE000) | (value & 0x1FFF));
			BinaryPrimitives.WriteUInt16BigEndian(Span.Slice(6, 2), value2);
		}
	}

	public byte Dscp
	{
		get
		{
			return (byte)((Span[1] & 0xFC) >> 2);
		}
		set
		{
			Span[1] = (byte)((Span[1] & 3) | ((value & 0x3F) << 2));
		}
	}

	public IpEcnField Ecn
	{
		get
		{
			return (IpEcnField)(Span[1] & 3);
		}
		set
		{
			Span[1] = (byte)((uint)(Span[1] & 0xFC) | (uint)(value & IpEcnField.Ce));
		}
	}

	public override byte TimeToLive
	{
		get
		{
			return Span[8];
		}
		set
		{
			Span[8] = value;
		}
	}

	public ushort HeaderChecksum
	{
		get
		{
			return BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(10, 2));
		}
		set
		{
			BinaryPrimitives.WriteUInt16BigEndian(Span.Slice(10, 2), value);
		}
	}

	protected override Span<byte> SourceAddressBuffer
	{
		get
		{
			return Span.Slice(12, 4);
		}
		set
		{
			value.CopyTo(Span.Slice(12, 4));
			SourceAddressField = null;
		}
	}

	protected override Span<byte> DestinationAddressBuffer
	{
		get
		{
			return Span.Slice(16, 4);
		}
		set
		{
			value.CopyTo(Span.Slice(16, 4));
			DestinationAddressField = null;
		}
	}

	public Memory<byte> Options => base.Buffer.Slice(20, InternetHeaderLength * 4 - 20);

	public IpV4Packet(IMemoryOwner<byte> memoryOwner)
		: this(memoryOwner.Memory)
	{
		_memoryOwner = memoryOwner;
	}

	public IpV4Packet(IMemoryOwner<byte> memoryOwner, int packetLength, IpProtocol protocol, int optionsLength)
		: this(memoryOwner.Memory.Slice(0, packetLength), protocol, optionsLength)
	{
		_memoryOwner = memoryOwner;
	}

	public IpV4Packet(Memory<byte> buffer, IpProtocol protocol, int optionsLength)
		: base(buffer)
	{
		int length = buffer.Length;
		if ((length < 20 || length > 65535) ? true : false)
		{
			throw new ArgumentException("Buffer too small for IP header.", "buffer");
		}
		buffer.Span.Clear();
		if ((optionsLength < 0 || optionsLength > 40) ? true : false)
		{
			throw new ArgumentOutOfRangeException("optionsLength", "Options length must be between 0 and 40 bytes.");
		}
		if (optionsLength % 4 != 0)
		{
			throw new ArgumentOutOfRangeException("optionsLength", "Options length must be a multiple of 4 bytes.");
		}
		base.Version = IpVersion.IPv4;
		Span[9] = (byte)protocol;
		int num = (20 + optionsLength) / 4;
		Span[0] = (byte)(0x40 | num);
		BinaryPrimitives.WriteUInt16BigEndian(Span.Slice(2, 2), (ushort)buffer.Length);
	}

	private static Memory<byte> AdjustBuffer(Memory<byte> buffer)
	{
		int packetLength = GetPacketLength(buffer.Span);
		if (packetLength < 20)
		{
			throw new ArgumentException("Invalid IPv4 packet length.", "buffer");
		}
		if (packetLength > buffer.Length)
		{
			throw new ArgumentException("Buffer too small for IPv4 packet.", "buffer");
		}
		return buffer.Slice(0, packetLength);
	}

	public IpV4Packet(Memory<byte> buffer)
		: base(AdjustBuffer(buffer))
	{
		buffer = base.Buffer;
		int num = Span[0] & 0xF;
		if (num < 5)
		{
			throw new ArgumentException("Invalid Internet Header Length.");
		}
		int num2 = num * 4;
		if (buffer.Length < num2)
		{
			throw new ArgumentException("Buffer too small for IP header.");
		}
		ushort num3 = BinaryPrimitives.ReadUInt16BigEndian(buffer.Span.Slice(2, 2));
		if (buffer.Length != num3)
		{
			throw new ArgumentException("Buffer length does not match IP total length field.");
		}
	}

	public bool IsHeaderChecksumValid()
	{
		ushort headerChecksum = HeaderChecksum;
		Span[10] = 0;
		Span[11] = 0;
		ushort num = PacketUtil.OnesComplementSum(Span.Slice(0, Header.Length));
		Span[10] = (byte)(headerChecksum >> 8);
		Span[11] = (byte)(headerChecksum & 0xFF);
		return headerChecksum == num;
	}

	public void UpdateHeaderChecksum()
	{
		Span[10] = 0;
		Span[11] = 0;
		HeaderChecksum = PacketUtil.OnesComplementSum(Span.Slice(0, Header.Length));
	}

	public static int GetPacketLength(ReadOnlySpan<byte> buffer)
	{
		if (IpPacket.GetPacketVersion(buffer) != IpVersion.IPv4)
		{
			throw new ArgumentException("Buffer is not an IPv4 packet.", "buffer");
		}
		if (buffer.Length < 4)
		{
			throw new ArgumentException("IPv4 header requires at least 4 bytes to determine packet length.", "buffer");
		}
		return (ushort)((buffer[2] << 8) | buffer[3]);
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

	~IpV4Packet()
	{
		Dispose(disposing: false);
	}
}
