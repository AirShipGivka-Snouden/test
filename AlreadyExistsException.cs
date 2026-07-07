using System;

namespace VpnHood.Core.Toolkit.Exceptions;

public sealed class AlreadyExistsException : Exception
{
	public string CollectionName { get; }

	public AlreadyExistsException(string collectionName)
		: base("Object already exists in " + collectionName + ".")
	{
		CollectionName = collectionName;
		Data["HttpStatusCode"] = 409;
	}

	public AlreadyExistsException(string collectionName, Exception innerException)
		: base("Object already exists in " + collectionName + ".", innerException)
	{
		CollectionName = collectionName;
		Data["HttpStatusCode"] = 409;
	}

	public static bool Is(Exception ex)
	{
		if (ex is AlreadyExistsException)
		{
			return true;
		}
		bool flag = ex.Data.Contains("HelpLink.EvtID");
		if (flag)
		{
			string text = ex.Data["HelpLink.EvtID"]?.ToString();
			bool flag2 = ((text == "2601" || text == "2627") ? true : false);
			flag = flag2;
		}
		if (flag)
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
