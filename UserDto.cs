using System;
using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class UserDto
{
	[JsonProperty("user_id")]
	public int UserId { get; set; }

	[JsonProperty("subscription_status")]
	public int SubscriptionStatus { get; set; }

	[JsonProperty("subscription_overdue")]
	public bool SubscriptionOverdue { get; set; }

	[JsonProperty("subscription_expiry_day")]
	public DateTime? SubscriptionExpiryDay { get; set; }

	[JsonProperty("created_at")]
	public string? CreatedAt { get; set; }

	public static UserDto CreateWithActiveLicense(int userId = 1)
	{
		return new UserDto
		{
			UserId = userId,
			SubscriptionStatus = 2,
			SubscriptionOverdue = false,
			SubscriptionExpiryDay = DateTime.UtcNow.AddDays(30.0),
			CreatedAt = DateTime.UtcNow.ToString("o")
		};
	}

	public static UserDto CreateWithExpiredLicense(int userId = 1)
	{
		return new UserDto
		{
			UserId = userId,
			SubscriptionStatus = 2,
			SubscriptionOverdue = true,
			SubscriptionExpiryDay = DateTime.UtcNow.AddDays(-1.0),
			CreatedAt = DateTime.UtcNow.ToString("o")
		};
	}

	public static UserDto CreateWithInactiveLicense(int userId = 1)
	{
		return new UserDto
		{
			UserId = userId,
			SubscriptionStatus = 0,
			SubscriptionOverdue = false,
			SubscriptionExpiryDay = null,
			CreatedAt = DateTime.UtcNow.ToString("o")
		};
	}
}
