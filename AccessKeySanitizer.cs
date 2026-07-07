using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Serilog;

namespace Ciphra.VPN.Common.Services;

internal static class AccessKeySanitizer
{
	private static readonly string[] VhPrefixes = new string[4] { "vhkey://", "vh://", "vhkey:", "vh:" };

	public static string Sanitize(string accessKey)
	{
		if (string.IsNullOrWhiteSpace(accessKey))
		{
			return accessKey;
		}
		string text = accessKey.Trim().Trim('"');
		string text2 = null;
		string[] vhPrefixes = VhPrefixes;
		foreach (string text3 in vhPrefixes)
		{
			if (text.StartsWith(text3, StringComparison.Ordinal))
			{
				text2 = text3;
				text = text.Substring(text3.Length);
				break;
			}
		}
		if (text2 == null)
		{
			return accessKey;
		}
		try
		{
			byte[] bytes = Convert.FromBase64String(PadBase64(text));
			string json = Encoding.UTF8.GetString(bytes);
			if (!(JsonNode.Parse(json) is JsonObject jsonObject))
			{
				return accessKey;
			}
			if (!((jsonObject["ser"] as JsonObject)?["ep"] is JsonArray { Count: not 0 } jsonArray))
			{
				return accessKey;
			}
			bool flag = false;
			for (int j = 0; j < jsonArray.Count; j++)
			{
				string text4 = jsonArray[j]?.GetValue<string>();
				if (text4 != null)
				{
					string text5 = FixUnbracketedIPv6(text4);
					if (text5 != text4)
					{
						jsonArray[j] = text5;
						flag = true;
					}
				}
			}
			if (!flag)
			{
				return accessKey;
			}
			string s = jsonObject.ToJsonString(new JsonSerializerOptions
			{
				WriteIndented = false
			});
			string text6 = Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
			Log.Debug("AccessKeySanitizer: repaired unbracketed IPv6 endpoint(s) in access key.");
			return text2 + text6;
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "AccessKeySanitizer: pass-through (could not parse access key).");
			return accessKey;
		}
	}

	internal static string FixUnbracketedIPv6(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}
		if (value[0] == '[')
		{
			return value;
		}
		int num = 0;
		foreach (char c in value)
		{
			if (c == ':')
			{
				num++;
			}
		}
		if (num < 2)
		{
			return value;
		}
		if (IPAddress.TryParse(value, out IPAddress _))
		{
			return value;
		}
		int num2 = value.LastIndexOf(':');
		if (num2 <= 0 || num2 == value.Length - 1)
		{
			return value;
		}
		string text = value.Substring(0, num2);
		string text2 = value.Substring(num2 + 1);
		if (!IPAddress.TryParse(text, out IPAddress address2) || address2.AddressFamily != AddressFamily.InterNetworkV6)
		{
			return value;
		}
		if (!ushort.TryParse(text2, NumberStyles.None, CultureInfo.InvariantCulture, out var result) || result == 0)
		{
			return value;
		}
		return "[" + text + "]:" + text2;
	}

	private static string PadBase64(string base64)
	{
		int num = base64.Length % 4;
		return (num == 0) ? base64 : (base64 + new string('=', 4 - num));
	}
}
