using System;

namespace VpnHood.Core.Client.Abstractions.Exceptions;

public class AlwaysOnNotAllowedException : UnauthorizedAccessException
{
	public AlwaysOnNotAllowedException(string message)
		: base(message)
	{
	}
}
