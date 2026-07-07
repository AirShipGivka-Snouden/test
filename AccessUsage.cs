using System;
using System.Text.Json.Serialization;

namespace VpnHood.Core.Common.Messaging;

public class AccessUsage
{
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool CanExtendByRewardedAd { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[Obsolete]
	public bool IsUserReviewRecommended { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int UserReviewRecommended { get; set; }

	public bool IsPremium { get; set; }

	public Traffic CycleTraffic { get; set; } = new Traffic();

	public Traffic TotalTraffic { get; set; } = new Traffic();

	public long MaxTraffic { get; set; }

	public DateTime? ExpirationTime { get; set; }

	public int? ActiveClientCount { get; set; }
}
