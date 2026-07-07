using System;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace VpnHood.Core.Proxies.EndPointManagement.Abstractions;

internal static class ProxyEndPointExtractor
{
	public static Uri? Extract(string text, string defaultScheme = "http", bool preferHttpsWhenPort443 = true)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		text = text.Trim();
		Match match = UrlRegex().Match(text);
		if (match.Success && Uri.TryCreate(match.Value, UriKind.Absolute, out Uri result) && IsValidProxyScheme(result.Scheme))
		{
			return NormalizeUri(result);
		}
		string text2 = DetectProtocolFromKeywords(text);
		Match match2 = Ipv6WithPortRegex().Match(text);
		if (match2.Success)
		{
			string value = match2.Groups["host"].Value;
			if (!int.TryParse(match2.Groups["port"].Value, out var result2) || result2 < 1 || result2 > 65535)
			{
				return null;
			}
			return BuildUri(DetermineScheme(text2, result2, defaultScheme, preferHttpsWhenPort443), value, result2);
		}
		Match match3 = Ipv4PortRegex().Match(text);
		if (match3.Success)
		{
			string value2 = match3.Groups["host"].Value;
			if (!IsValidIpv4(value2))
			{
				return null;
			}
			if (!int.TryParse(match3.Groups["port"].Value, out var result3) || result3 < 1 || result3 > 65535)
			{
				return null;
			}
			return BuildUri(DetermineScheme(text2, result3, defaultScheme, preferHttpsWhenPort443), value2, result3);
		}
		Match match4 = HostPortRegex().Match(text);
		if (match4.Success)
		{
			string value3 = match4.Groups["host"].Value;
			if (!int.TryParse(match4.Groups["port"].Value, out var result4) || result4 < 1 || result4 > 65535)
			{
				return null;
			}
			return BuildUri(DetermineScheme(text2, result4, defaultScheme, preferHttpsWhenPort443), value3, result4);
		}
		Match match5 = Ipv6OnlyRegex().Match(text);
		if (match5.Success)
		{
			string value4 = match5.Groups["host"].Value;
			string scheme = text2 ?? defaultScheme;
			int defaultPortForScheme = GetDefaultPortForScheme(scheme);
			return BuildUri(scheme, value4, defaultPortForScheme);
		}
		Match match6 = Ipv4OnlyRegex().Match(text);
		if (match6.Success)
		{
			string value5 = match6.Groups["host"].Value;
			if (!IsValidIpv4(value5))
			{
				return null;
			}
			if (text2 == null)
			{
				return null;
			}
			int defaultPortForScheme2 = GetDefaultPortForScheme(text2);
			return BuildUri(text2, value5, defaultPortForScheme2);
		}
		return null;
	}

	private static bool IsValidIpv4(string ip)
	{
		string[] array = ip.Split('.');
		if (array.Length != 4)
		{
			return false;
		}
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (string.IsNullOrEmpty(text) || (text.Length > 1 && text[0] == '0'))
			{
				return false;
			}
			if (!int.TryParse(text, out var result) || result < 0 || result > 255)
			{
				return false;
			}
		}
		return true;
	}

	private static string? DetectProtocolFromKeywords(string text)
	{
		string text2 = text.ToLowerInvariant();
		if (text2.Contains("socks5") || text2.Contains("socks 5"))
		{
			return "socks5";
		}
		if (text2.Contains("socks4") || text2.Contains("socks 4"))
		{
			return "socks4";
		}
		if (text2.Contains("socks"))
		{
			return "socks5";
		}
		if (text2.Contains("https"))
		{
			return "https";
		}
		if (text2.Contains("http"))
		{
			return "http";
		}
		return null;
	}

	private static string DetermineScheme(string? detectedProtocol, int port, string defaultScheme, bool preferHttpsWhenPort443)
	{
		if (!string.IsNullOrEmpty(detectedProtocol))
		{
			return detectedProtocol;
		}
		return port switch
		{
			1080 => "socks5", 
			1081 => "socks5", 
			9050 => "socks5", 
			443 => "https", 
			80 => "http", 
			8080 => "http", 
			3128 => "http", 
			8888 => "http", 
			_ => defaultScheme, 
		};
	}

	private static int GetDefaultPortForScheme(string scheme)
	{
		return scheme.ToLowerInvariant() switch
		{
			"socks4" => 1080, 
			"socks5" => 1080, 
			"socks" => 1080, 
			"http" => 8080, 
			"https" => 443, 
			_ => 8080, 
		};
	}

	private static bool IsValidProxyScheme(string scheme)
	{
		switch (scheme.ToLowerInvariant())
		{
		case "http":
		case "https":
		case "socks4":
		case "socks5":
		case "socks":
		case "socks5h":
			return true;
		default:
			return false;
		}
	}

	private static Uri NormalizeUri(Uri uri)
	{
		string text = uri.Scheme.ToLowerInvariant();
		string text2 = ((text == "socks") ? "socks5" : ((!(text == "socks5h")) ? uri.Scheme.ToLowerInvariant() : "socks5"));
		string scheme = text2;
		int port = (((object)uri != null && uri.Port > 0 && !uri.IsDefaultPort) ? uri.Port : GetDefaultPortForScheme(scheme));
		return BuildUri(scheme, uri.Host, port, uri.UserInfo);
	}

	private static Uri BuildUri(string scheme, string host, int port, string? userInfo = null)
	{
		UriBuilder uriBuilder = new UriBuilder
		{
			Scheme = scheme.ToLowerInvariant(),
			Host = host,
			Port = port
		};
		if (!string.IsNullOrEmpty(userInfo))
		{
			string[] array = userInfo.Split(':', 2);
			uriBuilder.UserName = Uri.UnescapeDataString(array[0]);
			if (array.Length > 1)
			{
				uriBuilder.Password = Uri.UnescapeDataString(array[1]);
			}
		}
		return uriBuilder.Uri;
	}

	[GeneratedRegex("(?<url>(?:https?|socks5?h?|socks)://(?:[^:@\\s]+(?::[^@\\s]*)?@)?(?:\\[[^\\]]+\\]|[a-zA-Z0-9\\.\\-]+)(?::\\d{1,5})?)", RegexOptions.IgnoreCase)]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.23019")]
	private static Regex UrlRegex()
	{
		return _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__UrlRegex_0.Instance;
	}

	[GeneratedRegex("\\[(?<host>[0-9a-fA-F:]+)\\]:(?<port>\\d{1,5})\\b")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.23019")]
	private static Regex Ipv6WithPortRegex()
	{
		return _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Ipv6WithPortRegex_1.Instance;
	}

	[GeneratedRegex("(?<!\\d)(?<!\\.)(?<host>\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})(?!\\.)(?!\\d)[\\s:]+(?<port>\\d{1,5})\\b")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.23019")]
	private static Regex Ipv4PortRegex()
	{
		return _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Ipv4PortRegex_2.Instance;
	}

	[GeneratedRegex("\\b(?<host>(?:[a-zA-Z0-9](?:[a-zA-Z0-9\\-]{0,61}[a-zA-Z0-9])?\\.)+[a-zA-Z][a-zA-Z0-9\\-]{0,61}[a-zA-Z0-9]?)[\\s:]+(?<port>\\d{1,5})\\b")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.23019")]
	private static Regex HostPortRegex()
	{
		return _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__HostPortRegex_3.Instance;
	}

	[GeneratedRegex("\\[(?<host>[0-9a-fA-F:]+)\\]")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.23019")]
	private static Regex Ipv6OnlyRegex()
	{
		return _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Ipv6OnlyRegex_4.Instance;
	}

	[GeneratedRegex("(?<!\\d)(?<!\\.)(?<host>\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})(?!\\.)(?!\\d)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.23019")]
	private static Regex Ipv4OnlyRegex()
	{
		return _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Ipv4OnlyRegex_5.Instance;
	}
}
