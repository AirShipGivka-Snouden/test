using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ciphra.VPN.Common.Utils;

public static class DomainTextFileParser
{
	private static readonly Regex DomainRuleRegex = new Regex("^(?:\\*\\.)?(?=.{1,253}$)(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

	public static string[]? Parse(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		text = Regex.Replace(text, "#.*|;.*", string.Empty);
		return (from x in text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
			select x.Trim() into x
			where x.Length > 0
			select x).Select(NormalizeAndValidate).ToArray();
	}

	public static string[] Parse(string? text, string[] defaultValue)
	{
		string[] array = Parse(text);
		return (array == null || array.Length == 0) ? defaultValue : array;
	}

	public static bool TryValidate(string? text, out string? error)
	{
		try
		{
			Parse(text);
			error = null;
			return true;
		}
		catch (FormatException ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private static string NormalizeAndValidate(string domain)
	{
		string text = domain.Trim().TrimEnd('.').ToLowerInvariant();
		if (text.Length == 0)
		{
			throw new FormatException("Domain rule is empty.");
		}
		if (text.Contains("://") || text.Contains('/') || text.Contains('\\') || text.Contains(':') || text.Any(char.IsWhiteSpace))
		{
			throw new FormatException("Invalid domain rule '" + domain + "'. Enter only a domain, for example example.com or *.example.com.");
		}
		if (!DomainRuleRegex.IsMatch(text))
		{
			throw new FormatException("Invalid domain rule '" + domain + "'. Enter only a domain, for example example.com or *.example.com.");
		}
		return text;
	}
}
