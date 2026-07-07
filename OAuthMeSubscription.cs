using System;
using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public sealed class OAuthMeSubscription
{
	[JsonProperty("is_premium")]
	public bool IsPremium { get; set; }

	[JsonProperty("expires_at")]
	public DateTime? ExpiresAt { get; set; }

	[JsonProperty("source")]
	public string? Source { get; set; }

	[JsonProperty("overdue")]
	public bool Overdue { get; set; }

	[JsonProperty("data_cap_bytes")]
	public long? DataCapBytes { get; set; }
}
