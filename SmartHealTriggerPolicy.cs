using System;

namespace Ciphra.VPN.Common.Services;

public static class SmartHealTriggerPolicy
{
	public const int ReconnectFailureThreshold = 2;

	public const int UnstableCountThreshold = 3;

	public const int WaitingCountThreshold = 2;

	public static readonly TimeSpan InstabilityWindow = TimeSpan.FromMinutes(3L);

	public static readonly TimeSpan ZeroTrafficWhileConnectedThreshold = TimeSpan.FromSeconds(30L);

	public static readonly TimeSpan MinOutageAgeForSignal3 = TimeSpan.FromSeconds(60L);

	public static SmartHealTriggerReason Evaluate(SmartHealTriggerInput input)
	{
		if (!input.SmartHealEnabled)
		{
			return SmartHealTriggerReason.None;
		}
		if (input.SmartHealAlreadyAttemptedThisSession)
		{
			return SmartHealTriggerReason.None;
		}
		if (input.ConsecutiveReconnectFailures >= 2)
		{
			TimeSpan? timeSinceFirstReconnectFailure = input.TimeSinceFirstReconnectFailure;
			if (timeSinceFirstReconnectFailure.HasValue)
			{
				TimeSpan valueOrDefault = timeSinceFirstReconnectFailure.GetValueOrDefault();
				if (valueOrDefault >= MinOutageAgeForSignal3)
				{
					return SmartHealTriggerReason.ConsecutiveReconnectFailures;
				}
			}
		}
		if (input.IsClientConnected)
		{
			if (input.UnstableCountInWindow >= 3 || input.WaitingCountInWindow >= 2)
			{
				return SmartHealTriggerReason.VpnHoodInstability;
			}
			TimeSpan? timeSinceFirstReconnectFailure = input.TimeSinceConnected;
			if (timeSinceFirstReconnectFailure.HasValue)
			{
				TimeSpan valueOrDefault2 = timeSinceFirstReconnectFailure.GetValueOrDefault();
				if (valueOrDefault2 >= ZeroTrafficWhileConnectedThreshold && input.BytesSent == 0L && input.BytesReceived == 0)
				{
					return SmartHealTriggerReason.ZeroTrafficWhileConnected;
				}
			}
		}
		return SmartHealTriggerReason.None;
	}
}
