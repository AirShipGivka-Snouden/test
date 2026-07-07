using System;

namespace Ciphra.VPN.Common.Services;

public sealed record OAuthSignInStartResult(OAuthSignInStartStatus Status, Exception? Exception = null);
