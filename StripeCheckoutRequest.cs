using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class StripeCheckoutRequest
{
	[JsonProperty("token")]
	public string Token { get; set; } = string.Empty;

	[JsonProperty("device_id")]
	public string DeviceId { get; set; } = string.Empty;

	[JsonProperty("price_id")]
	public string PriceId { get; set; } = string.Empty;

	[JsonProperty("success_url")]
	public string SuccessUrl { get; set; } = string.Empty;

	[JsonProperty("cancel_url")]
	public string CancelUrl { get; set; } = string.Empty;
}
