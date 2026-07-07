using System;
using System.Collections.Generic;
using System.Globalization;

namespace VpnHood.Core.Common.Tokens;

public class ServerLocationInfo : IComparable<ServerLocationInfo>
{
	public const string AutoCountryCode = "*";

	public const string AutoRegionName = "*";

	public required string CountryCode { get; init; }

	public required string RegionName { get; init; }

	public string[]? Tags { get; set; }

	public string ServerLocation => CountryCode + "/" + RegionName;

	public string CountryName
	{
		get
		{
			if (!(CountryCode == "*"))
			{
				return GetCountryName(CountryCode);
			}
			return "*";
		}
	}

	public bool IsAuto => IsAutoLocation(ServerLocation);

	public bool HasRegion
	{
		get
		{
			if (!string.IsNullOrEmpty(RegionName))
			{
				return RegionName != "*";
			}
			return false;
		}
	}

	public int CompareTo(ServerLocationInfo? other)
	{
		if (other == null)
		{
			return 1;
		}
		int num = string.Compare(CountryName, other.CountryName, StringComparison.OrdinalIgnoreCase);
		if (num == 0)
		{
			return string.Compare(RegionName, other.RegionName, StringComparison.OrdinalIgnoreCase);
		}
		return num;
	}

	public override bool Equals(object? obj)
	{
		return ServerLocation == (obj as ServerLocationInfo)?.ServerLocation;
	}

	public override int GetHashCode()
	{
		return ServerLocation.GetHashCode();
	}

	public override string ToString()
	{
		string[]? tags = Tags;
		if (tags == null || tags.Length == 0)
		{
			return ServerLocation;
		}
		return ServerLocation + " [" + string.Join(" ", Tags) + "]";
	}

	public static ServerLocationInfo Parse(string value)
	{
		int num = value.IndexOf('[');
		int num2 = value.LastIndexOf(']');
		if (num == -1 || num2 == -1 || num2 < num)
		{
			num = value.Length;
			num2 = value.Length;
		}
		string text = value.Substring(0, num).Trim();
		string text2;
		if (num >= num2)
		{
			text2 = string.Empty;
		}
		else
		{
			int num3 = num + 1;
			text2 = value.Substring(num3, num2 - num3).Trim();
		}
		string[] tags = text2.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		string[] parts = text.Split('/');
		return new ServerLocationInfo
		{
			CountryCode = ParseLocationPart(parts, 0).ToUpper(),
			RegionName = ParseLocationPart(parts, 1),
			Tags = tags
		};
	}

	public static ServerLocationInfo? TryParse(string value)
	{
		try
		{
			return Parse(value);
		}
		catch
		{
			return null;
		}
	}

	public bool IsMatch(ServerLocationInfo serverLocationInfo)
	{
		if (MatchLocationPart(CountryCode, serverLocationInfo.CountryCode))
		{
			return MatchLocationPart(RegionName, serverLocationInfo.RegionName);
		}
		return false;
	}

	public bool LocationEquals(string? serverLocation)
	{
		if (serverLocation == null)
		{
			return IsAuto;
		}
		ServerLocationInfo obj = TryParse(serverLocation);
		return Equals(obj);
	}

	private static string ParseLocationPart(IReadOnlyList<string> parts, int index)
	{
		if (parts.Count > index && !string.IsNullOrWhiteSpace(parts[index]))
		{
			return parts[index].Trim();
		}
		return "*";
	}

	private static bool MatchLocationPart(string serverPart, string requestPart)
	{
		if (!(requestPart == "*"))
		{
			return requestPart.Equals(serverPart, StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static string GetCountryName(string countryCode)
	{
		try
		{
			if (countryCode == "*")
			{
				return "(auto)";
			}
			return new RegionInfo(countryCode).EnglishName;
		}
		catch (Exception)
		{
			return countryCode.ToUpper();
		}
	}

	public static bool IsAutoLocation(string? serverLocation)
	{
		if (string.IsNullOrEmpty(serverLocation))
		{
			return true;
		}
		ServerLocationInfo serverLocationInfo = TryParse(serverLocation);
		if (serverLocationInfo != null && serverLocationInfo.CountryCode == "*")
		{
			return serverLocationInfo.RegionName == "*";
		}
		return false;
	}
}
