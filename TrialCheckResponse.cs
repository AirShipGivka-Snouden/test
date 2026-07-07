using System;
using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class TrialCheckResponse : ApiResponse
{
	[JsonProperty("expiry_date")]
	public DateTime? ExpiryDate { get; set; }

	[JsonProperty("created_at")]
	public DateTime? CreatedAt { get; set; }
}
