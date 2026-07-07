using System;

namespace VpnHood.Core.Toolkit.Graphics;

public record struct VhColor(byte R, byte G, byte B, byte A)
{
	public static VhColor FromArgb(byte a, byte r, byte g, byte b)
	{
		return new VhColor(r, g, b, a);
	}

	public static VhColor FromRgb(byte r, byte g, byte b)
	{
		return new VhColor(r, g, b, byte.MaxValue);
	}

	public static VhColor Parse(string value)
	{
		value = value.Replace("#", "");
		switch (value.Length)
		{
		case 6:
		{
			byte r2 = Convert.ToByte(value.Substring(0, 2), 16);
			byte g2 = Convert.ToByte(value.Substring(2, 2), 16);
			byte b2 = Convert.ToByte(value.Substring(4, 2), 16);
			return FromRgb(r2, g2, b2);
		}
		case 8:
		{
			byte a = Convert.ToByte(value.Substring(0, 2), 16);
			byte r = Convert.ToByte(value.Substring(2, 2), 16);
			byte g = Convert.ToByte(value.Substring(4, 2), 16);
			byte b = Convert.ToByte(value.Substring(6, 2), 16);
			return FromArgb(a, r, g, b);
		}
		default:
			throw new FormatException("Invalid color format.");
		}
	}
}
