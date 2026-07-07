using System;
using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class UpdateSubscriptionResponse : ApiResponse
{
	[JsonProperty("subscription_level")]
	public int SubscriptionLevel { get; set; }

	[JsonProperty("updated_at")]
	public DateTime? UpdatedAt { get; set; }
}
