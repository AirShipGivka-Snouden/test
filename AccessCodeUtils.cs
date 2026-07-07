using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VpnHood.Core.Toolkit.Utils;

public static class AccessCodeUtils
{
	public static string Build(string random)
	{
		int value = CalculateChecksum(random);
		return $"{1}{value}{random}";
	}

	public static string? TryValidate(string accessCode)
	{
		try
		{
			return Validate(accessCode);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static string Validate(string accessCode)
	{
		accessCode = Regex.Replace(accessCode, "[^a-zA-Z0-9]", "").Trim();
		if (string.IsNullOrEmpty(accessCode))
		{
			throw new ArgumentException("Access Code is empty.");
		}
		if (!int.TryParse(accessCode[0].ToString(), out var result) || result != 1)
		{
			throw new ArgumentException("Unrecognized Access Code. First digit should be 1.");
		}
		if (accessCode.Length != 20)
		{
			throw new ArgumentException("Access code must have 20 digit.");
		}
		if (!int.TryParse(accessCode[1].ToString(), out var result2))
		{
			throw new ArgumentException("Invalid Access Code.", "accessCode");
		}
		if (CalculateChecksum(accessCode.Substring(2, 18)) != result2)
		{
			throw new ArgumentException("Invalid Access Code.", "accessCode");
		}
		return accessCode;
	}

	private static int CalculateChecksum(string input)
	{
		int num;
		for (num = input.Sum((char c) => c); num >= 10; num = num.ToString().Sum((char c) => c - 48))
		{
		}
		return num;
	}

	public static string Format(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < value.Length; i++)
		{
			if (i > 0 && i % 4 == 0)
			{
				stringBuilder.Append('-');
			}
			stringBuilder.Append(value[i]);
		}
		return stringBuilder.ToString();
	}

	public static string? Redact(string? accessCode)
	{
		accessCode = TryValidate(accessCode ?? "");
		if (accessCode == null)
		{
			return null;
		}
		if (accessCode.Length <= 4)
		{
			return "***";
		}
		string text = new string('*', accessCode.Length - 4);
		string? text2 = accessCode;
		return text + text2.Substring(text2.Length - 4);
	}
}
