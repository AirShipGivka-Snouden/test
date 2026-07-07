using System;
using System.Collections.Generic;

namespace VpnHood.Core.Toolkit.Exceptions;

public sealed class NotExistsException : Exception
{
	public NotExistsException(string? message = null)
		: base(message)
	{
		Data["HttpStatusCode"] = 404;
	}

	public NotExistsException(string message, Exception innerException)
		: base(message, innerException)
	{
		Data["HttpStatusCode"] = 404;
	}

	public static bool Is(Exception ex)
	{
		if ((ex is NotExistsException || ex is KeyNotFoundException) ? true : false)
		{
			return true;
		}
		if (ex is InvalidOperationException && (ex.Message.Contains("Sequence contains no matching element") || ex.Message.Contains("Sequence contains no elements")))
		{
			return true;
		}
		if (ex.Message.Contains("The database operation was expected to affect") && ex.Message.Contains("but actually affected 0 row"))
		{
			return true;
		}
		if (ex.InnerException != null)
		{
			return Is(ex.InnerException);
		}
		return false;
	}
}
