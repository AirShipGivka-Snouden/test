using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class CryptoCheckoutRequest
{
	[JsonProperty("product_type")]
	public string ProductType { get; set; } = string.Empty;

	[JsonProperty("user_token")]
	public string UserToken { get; set; } = string.Empty;
}
