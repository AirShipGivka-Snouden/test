using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public sealed class OAuthExchangeRequest
{
	[JsonProperty("client_id")]
	public string ClientId { get; set; } = "";

	[JsonProperty("code")]
	public string Code { get; set; } = "";

	[JsonProperty("code_verifier")]
	public string CodeVerifier { get; set; } = "";

	[JsonProperty("redirect_uri")]
	public string RedirectUri { get; set; } = "";
}
