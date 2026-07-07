using System;
using System.Collections.Specialized;
using System.Web;

namespace Ciphra.VPN.Common.Services;

public static class OAuthCallbackParser
{
	public static OAuthCallbackParseResult Parse(string? callbackUri)
	{
		if (string.IsNullOrWhiteSpace(callbackUri))
		{
			return new OAuthCallbackParseResult(OAuthCallbackParseStatus.Empty);
		}
		if (!Uri.TryCreate(callbackUri, UriKind.Absolute, out Uri result))
		{
			return new OAuthCallbackParseResult(OAuthCallbackParseStatus.InvalidUri);
		}
		if (!string.Equals(result.Scheme, "ciphra", StringComparison.OrdinalIgnoreCase) || !string.Equals(result.Host, "auth", StringComparison.OrdinalIgnoreCase) || !result.AbsolutePath.Equals("/callback", StringComparison.OrdinalIgnoreCase))
		{
			return new OAuthCallbackParseResult(OAuthCallbackParseStatus.UnexpectedShape, null, result.Scheme, result.Host, result.AbsolutePath);
		}
		NameValueCollection query = HttpUtility.ParseQueryString(result.Query);
		string queryValue = GetQueryValue(query, "state");
		string queryValue2 = GetQueryValue(query, "code");
		string queryValue3 = GetQueryValue(query, "error");
		string queryValue4 = GetQueryValue(query, "error_description");
		return new OAuthCallbackParseResult(OAuthCallbackParseStatus.Valid, new OAuthCallback(queryValue, queryValue2, queryValue3, queryValue4), result.Scheme, result.Host, result.AbsolutePath);
	}

	private static string? GetQueryValue(NameValueCollection query, string key)
	{
		string text = query[key];
		if (text != null)
		{
			return text;
		}
		string[] allKeys = query.AllKeys;
		foreach (string text2 in allKeys)
		{
			if (string.Equals(text2, key, StringComparison.OrdinalIgnoreCase))
			{
				return query[text2];
			}
		}
		return null;
	}
}
