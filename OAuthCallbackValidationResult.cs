namespace Ciphra.VPN.Common.Services;

public sealed record OAuthCallbackValidationResult(OAuthCallbackStatus Status, string? Code = null, string? CodeVerifier = null, string? ErrorMessage = null);
