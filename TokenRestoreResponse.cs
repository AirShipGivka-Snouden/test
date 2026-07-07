using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class TokenRestoreResponse : ApiResponse
{
	[JsonProperty("restored")]
	public bool Restored { get; set; }

	[JsonProperty("token")]
	public string? Token { get; set; }
}
