using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class CryptoCheckoutResponse : ApiResponse
{
	[JsonProperty("charge_id")]
	public string? ChargeId { get; set; }

	[JsonProperty("transaction_code")]
	public string? TransactionCode { get; set; }

	[JsonProperty("hosted_url")]
	public string? HostedUrl { get; set; }
}
