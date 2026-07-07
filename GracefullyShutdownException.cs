using System;

namespace VpnHood.Core.Common.Exceptions;

public class GracefullyShutdownException : Exception
{
	public GracefullyShutdownException(string? message = null, Exception? innerException = null)
		: base(message ?? "The application is shutting down gracefully.", innerException)
	{
	}
}
