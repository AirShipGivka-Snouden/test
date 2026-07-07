using System;
using Ciphra.VPN.Common.Models;

namespace Ciphra.VPN.Common.Services;

public class OAuthApiException : Exception
{
	public int StatusCode { get; }

	public OAuthErrorResponse Error { get; }

	public OAuthApiException(int statusCode, OAuthErrorResponse error)
		: base($"OAuth API error {statusCode}: {error.Error} {error.ErrorDescription}")
	{
		StatusCode = statusCode;
		Error = error;
	}
}
