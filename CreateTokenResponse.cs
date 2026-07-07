using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class CreateTokenResponse : ApiResponse
{
	[JsonProperty("token")]
	public string? Token { get; set; }
}
