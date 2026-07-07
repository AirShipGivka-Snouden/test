using System;
using System.Buffers.Binary;

namespace VpnHood.Core.Packets;

public static class ChecksumUtilsUnsafe
{
	public static ushort OnesComplementSum(ReadOnlySpan<byte> data)
	{
		return OnesComplementSum(ReadOnlySpan<byte>.Empty, data);
	}

	public unsafe static ushort OnesComplementSum(ReadOnlySpan<byte> pseudoHeader, ReadOnlySpan<byte> data)
	{
		ulong sum = 0uL;
		fixed (byte* ptr = pseudoHeader)
		{
			fixed (byte* ptr2 = data)
			{
				sum = SumFast(ptr2, sum: SumFast(ptr, pseudoHeader.Length, sum), len: data.Length);
			}
		}
		uint num = (uint)sum;
		uint num2 = (uint)(sum >> 32);
		num += num2;
		if (num < num2)
		{
			num++;
		}
		ushort num3 = (ushort)num;
		ushort num4 = (ushort)(num >> 16);
		num3 += num4;
		if (num3 < num4)
		{
			num3++;
		}
		ushort num5 = (ushort)(~num3);
		if (num5 != 0)
		{
			return num5;
		}
		return ushort.MaxValue;
	}

	private unsafe static ulong SumFast(byte* ptr, int len, ulong sum)
	{
		while (len >= 8)
		{
			ulong num = BinaryPrimitives.ReverseEndianness(*(ulong*)ptr);
			sum += num;
			if (sum < num)
			{
				sum++;
			}
			ptr += 8;
			len -= 8;
		}
		if (len >= 4)
		{
			uint num2 = BinaryPrimitives.ReverseEndianness(*(uint*)ptr);
			sum += num2;
			if (sum < num2)
			{
				sum++;
			}
			ptr += 4;
			len -= 4;
		}
		if (len >= 2)
		{
			ushort num3 = BinaryPrimitives.ReverseEndianness(*(ushort*)ptr);
			sum += num3;
			if (sum < num3)
			{
				sum++;
			}
			ptr += 2;
			len -= 2;
		}
		if (len == 1)
		{
			ushort num4 = (ushort)(*ptr << 8);
			sum += num4;
			if (sum < num4)
			{
				sum++;
			}
		}
		return sum;
	}
}
