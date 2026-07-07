namespace Ciphra.VPN.Common.Services;

public enum OAuthCallbackStatus
{
	Accepted,
	Ignored,
	Expired,
	AuthorizationError,
	StateMismatch,
	MissingCode
}
