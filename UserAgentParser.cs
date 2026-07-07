using System;

namespace VpnHood.Core.Toolkit.Utils;

public static class UserAgentParser
{
	public static string GetOperatingSystem(string? userAgent)
	{
		if (userAgent == null)
		{
			userAgent = string.Empty;
		}
		if (userAgent.Contains("Windows 98"))
		{
			return "Windows 98";
		}
		if (userAgent.Contains("Windows NT 5.0"))
		{
			return "Windows 2000";
		}
		if (userAgent.Contains("Windows NT 5.1"))
		{
			return "Windows XP";
		}
		if (userAgent.Contains("Windows NT 6.0"))
		{
			return "Windows Vista";
		}
		if (userAgent.Contains("Windows NT 6.1"))
		{
			return "Windows 7";
		}
		if (userAgent.Contains("Windows NT 6.2"))
		{
			return "Windows 8";
		}
		if (userAgent.Contains("Windows"))
		{
			return GetOsVersion(userAgent, "Windows");
		}
		if (userAgent.Contains("Android"))
		{
			return GetOsVersion(userAgent, "Android");
		}
		if (userAgent.Contains("Linux"))
		{
			return GetOsVersion(userAgent, "Linux");
		}
		if (userAgent.Contains("iPhone"))
		{
			return GetOsVersion(userAgent, "iPhone");
		}
		if (userAgent.Contains("iPad"))
		{
			return GetOsVersion(userAgent, "iPad");
		}
		if (userAgent.Contains("Macintosh"))
		{
			return GetOsVersion(userAgent, "Macintosh");
		}
		return "Unknown OS";
	}

	private static string GetOsVersion(string userAgent, string osName)
	{
		if (userAgent.Split(new string[1] { osName }, StringSplitOptions.None)[1].Split(';', ')').Length == 0)
		{
			return osName;
		}
		return osName + userAgent.Split(new string[1] { osName }, StringSplitOptions.None)[1].Split(';', ')')[0];
	}
}
