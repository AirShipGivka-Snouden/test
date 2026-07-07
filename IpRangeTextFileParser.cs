using System;
using System.Linq;
using System.Text.RegularExpressions;
using VpnHood.Core.Toolkit.Net;

namespace Ciphra.VPN.Common.Utils;

public static class IpRangeTextFileParser
{
	public static IpRange[]? Parse(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		text = Regex.Replace(text, "#.*|;.*", string.Empty);
		return (from x in text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
			select x.Trim() into x
			where x.Length > 0
			select x).Select(IpRange.Parse).ToArray();
	}

	public static IpRange[] Parse(string? text, IpRange[] defaultValue)
	{
		IpRange[] array = Parse(text);
		return (array == null || array.Length == 0) ? defaultValue : array;
	}

	public static IpRange[] ParseIncludes(string? text)
	{
		return Parse(text, IpNetwork.All.ToIpRanges().ToArray());
	}

	public static IpRange[] ParseExcludes(string? text)
	{
		return Parse(text, IpNetwork.None.ToIpRanges().ToArray());
	}
}
