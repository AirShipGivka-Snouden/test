using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class PurchaseDto
{
	[JsonProperty("type")]
	public string? Type { get; set; }

	[JsonProperty("id")]
	public string? Id { get; set; }

	[JsonProperty("platform")]
	public string? Platform { get; set; }

	[JsonProperty("name")]
	public string? Name { get; set; }

	[JsonProperty("amount_cents")]
	public int AmountCents { get; set; }

	[JsonProperty("currency")]
	public string? Currency { get; set; }
}
