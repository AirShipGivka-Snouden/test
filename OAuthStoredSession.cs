namespace Ciphra.VPN.Common.Services.Interfaces;

public sealed record OAuthStoredSession(string RefreshToken, string? SessionId);
