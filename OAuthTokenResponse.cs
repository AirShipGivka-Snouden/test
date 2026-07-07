using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public sealed class OAuthTokenResponse
{
	[JsonProperty("access_token")]
	public string AccessToken { get; set; } = "";

	[JsonProperty("refresh_token")]
	public string RefreshToken { get; set; } = "";

	[JsonProperty("session_id")]
	public string? SessionId { get; set; }

	[JsonProperty("token_type")]
	public string TokenType { get; set; } = "Bearer";

	[JsonProperty("expires_in")]
	public int ExpiresInSeconds { get; set; }
}
