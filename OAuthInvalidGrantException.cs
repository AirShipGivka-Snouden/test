using Ciphra.VPN.Common.Models;

namespace Ciphra.VPN.Common.Services;

public sealed class OAuthInvalidGrantException : OAuthApiException
{
	public OAuthInvalidGrantException(OAuthErrorResponse error)
		: base(400, error)
	{
	}
}
