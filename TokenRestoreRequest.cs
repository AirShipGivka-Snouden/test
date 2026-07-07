using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class TokenRestoreRequest
{
	[JsonProperty("device_id")]
	public string DeviceId { get; set; } = string.Empty;
}
