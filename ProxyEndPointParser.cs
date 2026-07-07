using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Web;

namespace VpnHood.Core.Proxies.EndPointManagement.Abstractions;

public static class ProxyEndPointParser
{
	private sealed class UrlParts
	{
		public string? Scheme { get; set; }

		public string? Host { get; set; }

		public int? Port { get; set; }

		public string? User { get; set; }

		public string? Password { get; set; }
	}

	private const string PseudoScheme = "unknown";

	public static ProxyEndPoint FromUrl(Uri uri)
	{
		if (!Enum.TryParse<ProxyProtocol>(uri.Scheme, ignoreCase: true, out var result))
		{
			throw new NotSupportedException("Unsupported scheme: " + uri.Scheme);
		}
		if (string.IsNullOrWhiteSpace(uri.Host))
		{
			throw new ArgumentException("Proxy URL must include a host.", "uri");
		}
		string username = null;
		string password = null;
		if (!string.IsNullOrEmpty(uri.UserInfo))
		{
			string[] array = uri.UserInfo.Split(':', 2);
			username = Uri.UnescapeDataString(array[0]);
			if (array.Length > 1)
			{
				password = Uri.UnescapeDataString(array[1]);
			}
		}
		int port = ((uri.Port > 0) ? uri.Port : GetDefaultPort(result));
		string text = HttpUtility.ParseQueryString(uri.Query)["enabled"]?.ToLower() ?? "true";
		ProxyEndPoint proxyEndPoint = new ProxyEndPoint
		{
			Protocol = result,
			Host = uri.Host,
			Port = port,
			Username = username,
			Password = password
		};
		ProxyEndPoint proxyEndPoint2 = proxyEndPoint;
		bool flag = ((text == "false" || text == "0") ? true : false);
		proxyEndPoint2.IsEnabled = !flag;
		return proxyEndPoint;
	}

	public static ProxyEndPoint Normalize(ProxyEndPoint proxyEndPoint)
	{
		ProxyEndPointDefaults defaults = new ProxyEndPointDefaults
		{
			IsEnabled = proxyEndPoint.IsEnabled,
			Password = proxyEndPoint.Password,
			Port = proxyEndPoint.Port,
			Protocol = proxyEndPoint.Protocol,
			Username = proxyEndPoint.Username
		};
		ProxyEndPoint proxyEndPoint2 = FromUrl(ParseHostToUrl(proxyEndPoint.Host, defaults));
		proxyEndPoint2.Username = proxyEndPoint.Username?.Trim();
		proxyEndPoint2.Password = proxyEndPoint.Password?.Trim();
		return proxyEndPoint2;
	}

	public static Uri? TryParseHostToUrl(string value, ProxyEndPointDefaults? defaults)
	{
		try
		{
			return ParseHostToUrl(value, defaults);
		}
		catch
		{
			return null;
		}
	}

	public static Uri ParseHostToUrl(string value, ProxyEndPointDefaults? defaults)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException("Value cannot be null or whitespace.", "value");
		}
		value = value.Trim();
		if (!TryCreateUri(value, out Uri uri))
		{
			throw new ArgumentException("Invalid URL format.", "value");
		}
		UrlParts urlParts = ExtractParts(uri);
		UrlParts urlParts2;
		if (defaults != null && defaults.Protocol.HasValue)
		{
			urlParts2 = urlParts;
			if (urlParts2.Scheme == null)
			{
				string text = (urlParts2.Scheme = ProtocolToScheme(defaults.Protocol.Value));
			}
		}
		urlParts2 = urlParts;
		if (urlParts2.User == null)
		{
			string text = (urlParts2.User = defaults?.Username);
		}
		urlParts2 = urlParts;
		if (urlParts2.Password == null)
		{
			string text = (urlParts2.Password = defaults?.Password);
		}
		urlParts2 = urlParts;
		if (!urlParts2.Port.HasValue)
		{
			int? num = (urlParts2.Port = defaults?.Port);
		}
		if (string.IsNullOrWhiteSpace(urlParts.Scheme) || string.IsNullOrWhiteSpace(urlParts.Host))
		{
			throw new ArgumentException("URL must include scheme and host.", "value");
		}
		if (!urlParts.Port.HasValue)
		{
			ProxyProtocol protocol = ParseProtocolLenient(urlParts.Scheme);
			urlParts.Port = GetDefaultPort(protocol);
		}
		UriBuilder uriBuilder = new UriBuilder
		{
			Scheme = urlParts.Scheme,
			Host = urlParts.Host,
			Port = urlParts.Port.Value,
			Path = string.Empty
		};
		if (!string.IsNullOrEmpty(urlParts.User))
		{
			uriBuilder.UserName = urlParts.User;
		}
		if (!string.IsNullOrEmpty(urlParts.Password))
		{
			uriBuilder.Password = urlParts.Password;
		}
		NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(uri.Query);
		if (defaults != null && defaults.IsEnabled == false && nameValueCollection["enabled"] == null)
		{
			nameValueCollection["enabled"] = "false";
		}
		uriBuilder.Query = nameValueCollection.ToString();
		return uriBuilder.Uri;
	}

	public static Uri? ExtractFromText(string text, string defaultScheme = "http", bool preferHttpsWhenPort443 = true)
	{
		return ProxyEndPointExtractor.Extract(text, defaultScheme, preferHttpsWhenPort443);
	}

	public static Uri[] ExtractFromContent(string content, string defaultScheme = "http", bool preferHttpsWhenPort443 = true)
	{
		content = FastReplaceCharsWithNewline(content);
		string[] array = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		List<Uri> list = new List<Uri>();
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Uri uri = ProxyEndPointExtractor.Extract(array2[i], defaultScheme, preferHttpsWhenPort443);
			if (uri != null)
			{
				list.Add(uri);
			}
		}
		return list.ToArray();
	}

	private static bool TryCreateUri(string input, [NotNullWhen(true)] out Uri? uri)
	{
		uri = null;
		int num = input.IndexOf("://", StringComparison.Ordinal);
		if (num == -1 || input.Substring(0, num).Contains('.'))
		{
			input = "unknown://" + input;
		}
		return Uri.TryCreate(input, UriKind.Absolute, out uri);
	}

	private static UrlParts ExtractParts(Uri url)
	{
		UrlParts urlParts = new UrlParts();
		if (!string.Equals(url.Scheme, "unknown", StringComparison.OrdinalIgnoreCase))
		{
			urlParts.Scheme = url.Scheme;
		}
		if (!string.IsNullOrEmpty(url.Host))
		{
			urlParts.Host = url.Host;
		}
		if ((object)url != null && url.Port > 0 && !url.IsDefaultPort)
		{
			urlParts.Port = url.Port;
		}
		if (!string.IsNullOrEmpty(url.UserInfo))
		{
			string[] array = url.UserInfo.Split(':', 2);
			urlParts.User = Uri.UnescapeDataString(array[0]);
			if (array.Length > 1)
			{
				urlParts.Password = Uri.UnescapeDataString(array[1]);
			}
		}
		return urlParts;
	}

	private static ProxyProtocol ParseProtocolLenient(string scheme)
	{
		switch (scheme.ToLowerInvariant())
		{
		case "socks":
		case "socks5":
		case "socks5h":
			return ProxyProtocol.Socks5;
		case "socks4":
			return ProxyProtocol.Socks4;
		case "http":
			return ProxyProtocol.Http;
		case "https":
			return ProxyProtocol.Https;
		default:
			throw new ArgumentException("Unknown protocol: " + scheme);
		}
	}

	private static string? ProtocolToScheme(ProxyProtocol protocol)
	{
		return protocol switch
		{
			ProxyProtocol.Socks4 => "socks4", 
			ProxyProtocol.Socks5 => "socks5", 
			ProxyProtocol.Http => "http", 
			ProxyProtocol.Https => "https", 
			_ => null, 
		};
	}

	private static int GetDefaultPort(ProxyProtocol protocol)
	{
		return protocol switch
		{
			ProxyProtocol.Socks4 => 1080, 
			ProxyProtocol.Socks5 => 1080, 
			ProxyProtocol.Http => 8080, 
			ProxyProtocol.Https => 443, 
			_ => 8080, 
		};
	}

	private static string FastReplaceCharsWithNewline(string content)
	{
		if (string.IsNullOrEmpty(content))
		{
			return content;
		}
		char[] array = content.ToCharArray();
		for (int i = 0; i < array.Length; i++)
		{
			bool flag;
			switch (array[i])
			{
			case '\r':
			case ' ':
			case '"':
			case '(':
			case ')':
			case ',':
			case '[':
			case ']':
			case '`':
			case '{':
			case '}':
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				array[i] = '\n';
			}
		}
		return new string(array);
	}
}
