using System;
using System.Net.Sockets;

namespace VpnHood.Core.Tunneling.Utils;

public static class SocketUtils
{
	public static bool IsInvalidUdpStateException(Exception ex)
	{
		if (!(ex is ObjectDisposedException))
		{
			if (ex is SocketException { SocketErrorCode: var socketErrorCode })
			{
				if (socketErrorCode <= SocketError.Interrupted)
				{
					if (socketErrorCode == SocketError.OperationAborted || socketErrorCode == SocketError.Interrupted)
					{
						goto IL_0043;
					}
				}
				else if (socketErrorCode == SocketError.NotSocket || socketErrorCode == SocketError.ConnectionAborted)
				{
					goto IL_0043;
				}
			}
			return false;
		}
		goto IL_0043;
		IL_0043:
		return true;
	}
}
