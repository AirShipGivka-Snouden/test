using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public sealed class OAuthLogoutRequest
{
	[JsonProperty("refresh_token")]
	public string RefreshToken { get; set; } = "";
}
