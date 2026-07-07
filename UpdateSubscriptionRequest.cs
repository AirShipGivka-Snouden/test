using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class UpdateSubscriptionRequest
{
	[JsonProperty("platform")]
	public string Platform { get; set; } = string.Empty;

	[JsonProperty("token")]
	public string Token { get; set; } = string.Empty;

	[JsonProperty("subscription_status")]
	public int SubscriptionStatus { get; set; }

	[JsonProperty("subscription_expiry_day")]
	public string? SubscriptionExpiryDay { get; set; }

	[JsonProperty("device_id")]
	public string DeviceId { get; set; } = string.Empty;

	[JsonProperty("purchase_token")]
	public string? PurchaseToken { get; set; }
}
