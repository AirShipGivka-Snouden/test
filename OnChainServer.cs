using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class OnChainServer
{
	[JsonProperty("id")]
	public string Id { get; set; } = "";

	[JsonProperty("access_key")]
	public string? AccessKey { get; set; }

	[JsonProperty("country")]
	public string Country { get; set; } = "";

	[JsonProperty("city")]
	public string City { get; set; } = "";

	[JsonProperty("category")]
	public int Category { get; set; }

	[JsonProperty("requires_upgrade")]
	public bool RequiresUpgrade { get; set; }

	[JsonProperty("expires_at")]
	public string? ExpiresAt { get; set; }

	public VpnServerDto ToVpnServerDto()
	{
		return new VpnServerDto
		{
			Id = Id,
			AccessKey = AccessKey,
			CountryIso = Country,
			City = City,
			ServerCategory = Category,
			RequiresUpgrade = RequiresUpgrade
		};
	}
}
