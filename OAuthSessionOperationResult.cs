using Ciphra.VPN.Common.Models;

namespace Ciphra.VPN.Common.Services;

public sealed record OAuthSessionOperationResult(OAuthSessionOperationStatus Status, OAuthMeResponse? Account = null, string? ErrorMessage = null);
