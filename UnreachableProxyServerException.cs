using System;

namespace VpnHood.Core.Client.Abstractions.Exceptions;

public class UnreachableProxyServerException : UnreachableServerException
{
	public UnreachableProxyServerException(string? message = null, Exception? innerException = null)
		: base(message, innerException)
	{
	}
}
