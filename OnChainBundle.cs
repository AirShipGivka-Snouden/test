using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class OnChainBundle
{
	[JsonProperty("token")]
	public string? Token { get; set; }

	[JsonProperty("servers")]
	public List<OnChainServer> Servers { get; set; } = new List<OnChainServer>();

	[JsonProperty("subscription_status")]
	public int SubscriptionStatus { get; set; }

	[JsonProperty("subscription_overdue")]
	public bool SubscriptionOverdue { get; set; }

	[JsonProperty("subscription_expiry_day")]
	public string? SubscriptionExpiryDay { get; set; }

	[JsonProperty("subscription_source")]
	public string? SubscriptionSource { get; set; }

	[JsonProperty("is_premium_user")]
	public bool IsPremiumUser { get; set; }

	[JsonProperty("data_cap_bytes")]
	public long? DataCapBytes { get; set; }

	[JsonProperty("bundle_ttl_seconds")]
	public int BundleTtlSeconds { get; set; }
}
