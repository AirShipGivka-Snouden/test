using System;

namespace VpnHood.Core.Client.Abstractions.Exceptions;

public class UnreachableServerException : Exception
{
	public UnreachableServerException(string? message = null, Exception? innerException = null)
		: base(message, innerException)
	{
	}
}
