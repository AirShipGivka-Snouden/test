using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class StripePortalResponse : ApiResponse
{
	[JsonProperty("portal_url")]
	public string? PortalUrl { get; set; }
}
