namespace Ciphra.VPN.Common.Services;

public sealed record OAuthCallback(string? State, string? Code, string? Error, string? ErrorDescription);
