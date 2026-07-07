using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class CreateTokenRequest
{
	[JsonProperty("device_id")]
	public string DeviceId { get; set; } = string.Empty;
}
