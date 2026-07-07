using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class PurchaseCheckoutRequest
{
	[JsonProperty("token")]
	public string Token { get; set; } = string.Empty;

	[JsonProperty("device_id")]
	public string DeviceId { get; set; } = string.Empty;

	[JsonProperty("purchase_id")]
	public string PurchaseId { get; set; } = string.Empty;

	[JsonProperty("locale_iso")]
	public string LocaleIso { get; set; } = string.Empty;

	[JsonProperty("transaction_id")]
	public string TransactionId { get; set; } = string.Empty;
}
