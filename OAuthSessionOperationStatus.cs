namespace Ciphra.VPN.Common.Services;

public enum OAuthSessionOperationStatus
{
	Success,
	NoStoredSession,
	Unavailable,
	SessionExpired,
	RefreshFailed,
	Failed
}
