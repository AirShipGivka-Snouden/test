using VpnHood.Core.Common.Messaging;

namespace Ciphra.VPN.Common.Models;

public readonly record struct NewSessionInfo(bool PriorSessionExisted, long PriorSessionDurationMs, long PriorSessionTotalBytes, SessionSuppressType NewSessionSuppressedTo);
