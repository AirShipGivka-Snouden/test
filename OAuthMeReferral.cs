using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public sealed class OAuthMeReferral
{
	[JsonProperty("code")]
	public string? Code { get; set; }

	[JsonProperty("share_url")]
	public string? ShareUrl { get; set; }
}
