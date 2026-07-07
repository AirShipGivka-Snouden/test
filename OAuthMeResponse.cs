using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public sealed class OAuthMeResponse
{
	[JsonProperty("id")]
	public string Id { get; set; } = "";

	[JsonProperty("email")]
	public string? Email { get; set; }

	[JsonProperty("email_verified")]
	public bool EmailVerified { get; set; }

	[JsonProperty("token")]
	public string? VpnToken { get; set; }

	[JsonProperty("subscription")]
	public OAuthMeSubscription? Subscription { get; set; }

	[JsonProperty("referral")]
	public OAuthMeReferral? Referral { get; set; }
}
