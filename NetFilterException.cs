using System;

namespace VpnHood.Core.Tunneling.Exceptions;

public class NetFilterException : Exception
{
	public NetFilterException(string? message = null, Exception? innerException = null)
		: base(message, innerException)
	{
	}
}
