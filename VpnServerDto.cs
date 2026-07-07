using System;
using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class VpnServerDto
{
	public const string BestServerCountry = "best";

	[JsonProperty("id")]
	public string? Id { get; set; }

	[JsonProperty("access_key")]
	public string? AccessKey { get; set; }

	[JsonProperty("country")]
	public string? CountryIso { get; set; }

	[JsonProperty("city")]
	public string? City { get; set; }

	[JsonProperty("category")]
	public int? ServerCategory { get; set; }

	[JsonProperty("requires_upgrade")]
	public bool RequiresUpgrade { get; set; }

	[JsonIgnore]
	public bool IsBestServer => string.Equals(CountryIso, "best", StringComparison.OrdinalIgnoreCase);
}
