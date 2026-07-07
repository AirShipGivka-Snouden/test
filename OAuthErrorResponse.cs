using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public sealed class OAuthErrorResponse
{
	[JsonProperty("error")]
	public string Error { get; set; } = "";

	[JsonProperty("error_description")]
	public string? ErrorDescription { get; set; }
}
