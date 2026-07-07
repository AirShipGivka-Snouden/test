using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class StripeCheckoutResponse : ApiResponse
{
	[JsonProperty("checkout_url")]
	public string? CheckoutUrl { get; set; }

	[JsonProperty("customer_id")]
	public string? CustomerId { get; set; }
}
