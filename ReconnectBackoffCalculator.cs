using System;

namespace Ciphra.VPN.Common.Services;

public static class ReconnectBackoffCalculator
{
	public static TimeSpan Calculate(int consecutiveFailures, TimeSpan baseDelay, TimeSpan maxDelay)
	{
		if (consecutiveFailures < 1)
		{
			consecutiveFailures = 1;
		}
		int num = Math.Min(5, consecutiveFailures - 1);
		double val = baseDelay.TotalSeconds * Math.Pow(2.0, num);
		val = Math.Min(val, maxDelay.TotalSeconds);
		return TimeSpan.FromSeconds(val);
	}
}
