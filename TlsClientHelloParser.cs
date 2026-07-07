using System;
using System.Text;

namespace VpnHood.Core.Filtering.DomainFiltering.SniExtractors;

public static class TlsClientHelloParser
{
	public static string? ExtractSni(ReadOnlySpan<byte> data, bool hasRecordHeader = true)
	{
		if (data.Length == 0)
		{
			return null;
		}
		int num = 0;
		if (hasRecordHeader)
		{
			if (data.Length < 5)
			{
				return null;
			}
			if (data[0] != 22)
			{
				return null;
			}
			num = 5;
		}
		if (num + 4 > data.Length)
		{
			return null;
		}
		if (data[num] != 1)
		{
			return null;
		}
		int num2 = (data[num + 1] << 16) | (data[num + 2] << 8) | data[num + 3];
		num += 4;
		int num3 = num + num2;
		if (num3 > data.Length)
		{
			num3 = data.Length;
		}
		num += 34;
		if (num >= num3)
		{
			return null;
		}
		if (num + 1 > num3)
		{
			return null;
		}
		byte b = data[num++];
		num += b;
		if (num > num3)
		{
			return null;
		}
		if (num + 2 > num3)
		{
			return null;
		}
		int num4 = (data[num] << 8) | data[num + 1];
		num += 2 + num4;
		if (num > num3)
		{
			return null;
		}
		if (num + 1 > num3)
		{
			return null;
		}
		byte b2 = data[num++];
		num += b2;
		if (num > num3)
		{
			return null;
		}
		if (num + 2 > num3)
		{
			return null;
		}
		int num5 = (data[num] << 8) | data[num + 1];
		num += 2;
		int num6 = num + num5;
		if (num6 > num3)
		{
			num6 = num3;
		}
		while (num + 4 <= num6)
		{
			int num7 = (data[num] << 8) | data[num + 1];
			int num8 = (data[num + 2] << 8) | data[num + 3];
			num += 4;
			if (num + num8 > num6)
			{
				return null;
			}
			if (num7 == 0)
			{
				return ParseSniExtension(data.Slice(num, num8));
			}
			num += num8;
		}
		return null;
	}

	private static string? ParseSniExtension(ReadOnlySpan<byte> extData)
	{
		if (extData.Length < 2)
		{
			return null;
		}
		int num = (extData[0] << 8) | extData[1];
		int num2 = 2;
		if (num2 + num > extData.Length)
		{
			num = extData.Length - num2;
		}
		int num3 = num2 + num;
		while (num2 + 3 <= num3)
		{
			byte b = extData[num2++];
			int num4 = (extData[num2] << 8) | extData[num2 + 1];
			num2 += 2;
			if (num2 + num4 > num3)
			{
				return null;
			}
			if (b == 0)
			{
				return Encoding.ASCII.GetString(extData.Slice(num2, num4));
			}
			num2 += num4;
		}
		return null;
	}
}
