namespace Ciphra.VPN.Common.Services;

public enum SmartHealTriggerReason
{
	None,
	ConsecutiveReconnectFailures,
	ZeroTrafficWhileConnected,
	VpnHoodInstability
}
