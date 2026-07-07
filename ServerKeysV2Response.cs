using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class ServerKeysV2Response : ApiResponse
{
	[JsonProperty("servers")]
	public List<VpnServerDto>? Servers { get; set; }

	[JsonProperty("subscription_status")]
	public int SubscriptionStatus { get; set; }

	[JsonProperty("subscription_overdue")]
	public bool SubscriptionOverdue { get; set; }

	[JsonProperty("subscription_source")]
	public string? SubscriptionSource { get; set; }
}
