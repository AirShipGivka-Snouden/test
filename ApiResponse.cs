using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class ApiResponse
{
	[JsonProperty("success")]
	public bool Success { get; set; }

	[JsonProperty("message")]
	public string? Message { get; set; }

	[JsonProperty("error")]
	public string? Error { get; set; }
}
