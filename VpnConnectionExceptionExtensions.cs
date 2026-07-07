using System;
using VpnHood.Core.Common.Exceptions;

namespace Ciphra.VPN.Common.Exceptions;

public static class VpnConnectionExceptionExtensions
{
	public const string SmartHealNowSentinel = "smart_heal_now";

	public static bool IsUnrecoverable(this VpnConnectionException ex)
	{
		for (Exception innerException = ex.InnerException; innerException != null; innerException = innerException.InnerException)
		{
			if (HasAdminMessage(innerException.Message))
			{
				return true;
			}
			if (innerException is UnauthorizedAccessException)
			{
				return true;
			}
		}
		return false;
		static bool HasAdminMessage(string? msg)
		{
			return msg?.Contains("admin privilege", StringComparison.OrdinalIgnoreCase) ?? false;
		}
	}

	public static bool IsSmartHealNowSentinel(this Exception? ex)
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			if (ex2 is SessionException ex3)
			{
				string text = ex3.SessionResponse?.ErrorMessage;
				if (text != null && text.StartsWith("smart_heal_now", StringComparison.Ordinal))
				{
					return true;
				}
			}
		}
		return false;
	}
}
