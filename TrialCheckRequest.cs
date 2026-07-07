using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class TrialCheckRequest
{
	[JsonProperty("device_id")]
	public string DeviceId { get; set; } = string.Empty;
}
