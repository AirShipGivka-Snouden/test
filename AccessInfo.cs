using System;

namespace VpnHood.Core.Common.Messaging;

public class AccessInfo
{
	public bool IsNew { get; init; }

	public DateTime CreatedTime { get; init; }

	public DateTime LastUsedTime { get; init; }

	public DateTime? ExpirationTime { get; init; }

	public bool IsPremium { get; init; }

	public long MaxCycleTraffic { get; set; }

	public long MaxTotalTraffic { get; set; }

	public int MaxDeviceCount { get; init; }

	public Traffic? MaxSpeedMbps { get; init; }

	public int DeviceLifeSpan { get; init; }

	public AccessDevicesSummary? DevicesSummary { get; init; }
}
