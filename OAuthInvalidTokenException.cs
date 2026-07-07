using Ciphra.VPN.Common.Models;

namespace Ciphra.VPN.Common.Services;

public sealed class OAuthInvalidTokenException : OAuthApiException
{
	public OAuthInvalidTokenException(OAuthErrorResponse error)
		: base(401, error)
	{
	}
}
