using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class StripePortalRequest
{
	[JsonProperty("customer_id")]
	public string CustomerId { get; set; } = string.Empty;

	[JsonProperty("return_url")]
	public string ReturnUrl { get; set; } = string.Empty;
}
