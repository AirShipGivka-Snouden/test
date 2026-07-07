using System;

namespace VpnHood.Core.Common.Exceptions;

public class UiContextNotAvailableException : Exception
{
	public UiContextNotAvailableException()
		: base("UiContext is not available.")
	{
	}
}
