namespace Ciphra.VPN.Common.Services;

public readonly record struct HealthSnapshot(bool NeedsReconnect, bool IsClientConnected, long BytesSent, long BytesReceived, int UnstableCountCumulative, int WaitingCountCumulative);
