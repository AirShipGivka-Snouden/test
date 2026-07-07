using System;

namespace Ciphra.VPN.Common.Services;

public readonly record struct SmartHealTriggerInput(int ConsecutiveReconnectFailures, TimeSpan? TimeSinceFirstReconnectFailure, bool IsClientConnected, long BytesSent, long BytesReceived, TimeSpan? TimeSinceConnected, int UnstableCountInWindow, int WaitingCountInWindow, bool SmartHealAlreadyAttemptedThisSession, bool SmartHealEnabled);
