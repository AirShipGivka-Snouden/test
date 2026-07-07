using System;

namespace VpnHood.Core.Client.VpnServices.Abstractions.Exceptions;

public class VpnServiceRevokedException : Exception
{
	public VpnServiceRevokedException(string? message = null, Exception? innerException = null)
		: base(message ?? "VPN connection has been revoked by the system or another VPN app.", innerException)
	{
	}
}
