namespace Ciphra.VPN.Common.Services;

public sealed record OAuthCallbackParseResult(OAuthCallbackParseStatus Status, OAuthCallback? Callback = null, string? Scheme = null, string? Host = null, string? Path = null);
