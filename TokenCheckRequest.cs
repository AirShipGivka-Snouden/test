using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class TokenCheckRequest
{
	[JsonProperty("token")]
	public string Token { get; set; } = string.Empty;

	[JsonProperty("device_id")]
	public string DeviceId { get; set; } = string.Empty;
}
