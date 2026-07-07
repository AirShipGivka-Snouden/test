using System;

namespace VpnHood.Core.Packets;

public static class ChecksumUtils
{
	public static ushort OnesComplementSum(ReadOnlySpan<byte> data)
	{
		return OnesComplementSum(ReadOnlySpan<byte>.Empty, data);
	}

	public static ushort OnesComplementSum(ReadOnlySpan<byte> pseudoHeader, ReadOnlySpan<byte> data)
	{
		uint num = 0u;
		num += ComputeSumWords(pseudoHeader);
		num += ComputeSumWords(data);
		while (num >> 16 != 0)
		{
			num = (num & 0xFFFF) + (num >> 16);
		}
		ushort num2 = (ushort)(~num & 0xFFFF);
		if (num2 == 0)
		{
			num2 = ushort.MaxValue;
		}
		return num2;
	}

	private static uint ComputeSumWords(ReadOnlySpan<byte> data)
	{
		uint num = 0u;
		for (int i = 0; i < data.Length; i += 2)
		{
			ushort num2 = (ushort)((data[i] << 8) | ((i + 1 < data.Length) ? data[i + 1] : 0));
			num += num2;
		}
		return num;
	}
}
