using System;
using VpnHood.Core.Toolkit.Graphics;

namespace VpnHood.Core.Client.VpnServices.Host;

public sealed class VpnServiceLocalization
{
	public string Disconnect { get; set; } = "Disconnect";

	public string Manage { get; set; } = "Manage";

	public string? WindowBackgroundColor { get; set; }

	public static VhColor? TryParseColorFromHex(string hex)
	{
		if (string.IsNullOrWhiteSpace(hex))
		{
			return null;
		}
		try
		{
			hex = hex.Replace("#", "");
			if (hex.Length == 6)
			{
				return VhColor.FromArgb(byte.MaxValue, Convert.ToByte(hex.Substring(0, 2), 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16));
			}
			if (hex.Length == 8)
			{
				return VhColor.FromArgb(Convert.ToByte(hex.Substring(0, 2), 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16), Convert.ToByte(hex.Substring(6, 2), 16));
			}
			return null;
		}
		catch
		{
			return null;
		}
	}
}
