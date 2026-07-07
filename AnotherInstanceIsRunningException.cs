using System;

namespace VpnHood.Core.Common.Exceptions;

public class AnotherInstanceIsRunningException : Exception
{
	public AnotherInstanceIsRunningException(string? message = null, Exception? innerException = null)
		: base(message ?? "Another instance is running.", innerException)
	{
	}
}
