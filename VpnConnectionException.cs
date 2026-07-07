using System;

namespace Ciphra.VPN.Common.Exceptions;

public class VpnConnectionException : Exception
{
	public VpnConnectionException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
