using System;
using VpnHood.Core.Common.Tokens;

namespace VpnHood.Core.Client.Abstractions.Exceptions;

public class UnreachableServerLocationException : UnreachableServerException
{
	public UnreachableServerLocationException(string? message = null, Exception? innerException = null)
		: base(message, innerException)
	{
	}

	public static UnreachableServerLocationException Create(string? serverLocation)
	{
		return new UnreachableServerLocationException(ServerLocationInfo.IsAutoLocation(serverLocation) ? "There is no reachable server at this moment. Please try again later." : ("There is no reachable server at this moment. Please try again later. Location: " + serverLocation))
		{
			Data = 
			{
				{
					(object)"ServerLocation",
					(object?)serverLocation
				},
				{
					(object)"IsAutoLocation",
					(object?)ServerLocationInfo.IsAutoLocation(serverLocation)
				}
			}
		};
	}
}
