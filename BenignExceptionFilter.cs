using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.WebSockets;

namespace Ciphra.VPN.Common.Utils;

public static class BenignExceptionFilter
{
	public static bool IsBenignTeardownException(Exception ex)
	{
		if (ex is ObjectDisposedException ex2)
		{
			string objectName = ex2.ObjectName;
			if (objectName != null && objectName.Contains("SslStream"))
			{
				return true;
			}
		}
		if (ex is NetworkInformationException)
		{
			string? stackTrace = ex.StackTrace;
			if (stackTrace == null || !stackTrace.Contains("NetworkChange"))
			{
				string? stackTrace2 = ex.StackTrace;
				if (stackTrace2 == null || !stackTrace2.Contains("VpnHood"))
				{
					goto IL_007f;
				}
			}
			return true;
		}
		goto IL_007f;
		IL_007f:
		if (ex is ObjectDisposedException ex3 && ex3.Message.Contains("CancellationTokenSource"))
		{
			string? stackTrace3 = ex3.StackTrace;
			if (stackTrace3 != null && stackTrace3.Contains("VpnHood"))
			{
				return true;
			}
		}
		IOException ex4 = ex as IOException;
		bool flag = ex4?.Message.Contains("read operation failed") ?? false;
		bool flag2 = flag;
		if (flag2)
		{
			Exception innerException = ex4.InnerException;
			bool flag3 = ((innerException is ObjectDisposedException || innerException is WebSocketException) ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			return true;
		}
		return false;
	}
}
