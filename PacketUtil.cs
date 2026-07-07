using System;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Packets;

public static class PacketUtil
{
	public static int ReadPacketLength(ReadOnlySpan<byte> buffer)
	{
		return IpPacket.GetPacketVersion(buffer) switch
		{
			IpVersion.IPv4 => IpV4Packet.GetPacketLength(buffer), 
			IpVersion.IPv6 => IpV6Packet.GetPacketLength(buffer), 
			_ => throw new Exception("Unknown packet version."), 
		};
	}

	public static ushort ComputeChecksum(ReadOnlySpan<byte> sourceAddress, ReadOnlySpan<byte> destinationAddress, byte protocol, ReadOnlySpan<byte> data)
	{
		if (sourceAddress.Length == 4 && destinationAddress.Length == 4)
		{
			Span<byte> span = stackalloc byte[12];
			sourceAddress.CopyTo(span.Slice(0, 4));
			destinationAddress.CopyTo(span.Slice(4, 4));
			span[8] = 0;
			span[9] = protocol;
			span[10] = (byte)(data.Length >> 8);
			span[11] = (byte)data.Length;
			return OnesComplementSum(span, data);
		}
		if (sourceAddress.Length == 16 && destinationAddress.Length == 16)
		{
			Span<byte> span2 = stackalloc byte[40];
			sourceAddress.CopyTo(span2.Slice(0, 16));
			destinationAddress.CopyTo(span2.Slice(16, 16));
			span2[32] = (byte)(data.Length >> 8);
			span2[33] = (byte)data.Length;
			span2[34] = 0;
			span2[35] = 0;
			span2[36] = 0;
			span2[37] = 0;
			span2[38] = 0;
			span2[39] = protocol;
			return OnesComplementSum(span2, data);
		}
		throw new ArgumentException("Invalid address lengths for checksum calculation.");
	}

	public static ushort OnesComplementSum(ReadOnlySpan<byte> data)
	{
		return OnesComplementSum(ReadOnlySpan<byte>.Empty, data);
	}

	public static ushort OnesComplementSum(ReadOnlySpan<byte> pseudoHeader, ReadOnlySpan<byte> data)
	{
		return ChecksumUtilsUnsafe.OnesComplementSum(pseudoHeader, data);
	}
}
