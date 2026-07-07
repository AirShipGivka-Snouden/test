using System;
using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class TokenCheckResponse : ApiResponse
{
	[JsonProperty("valid")]
	public bool Valid { get; set; }

	[JsonProperty("expires_at")]
	public DateTime? ExpiresAt { get; set; }

	[JsonProperty("user")]
	public UserDto? User { get; set; }

	public static TokenCheckResponse CreateWithActiveLicense(int userId = 1)
	{
		return new TokenCheckResponse
		{
			Success = true,
			Valid = true,
			ExpiresAt = DateTime.UtcNow.AddDays(30.0),
			User = UserDto.CreateWithActiveLicense(userId)
		};
	}

	public static TokenCheckResponse CreateWithExpiredLicense(int userId = 1)
	{
		return new TokenCheckResponse
		{
			Success = true,
			Valid = true,
			ExpiresAt = DateTime.UtcNow.AddDays(30.0),
			User = UserDto.CreateWithExpiredLicense(userId)
		};
	}

	public static TokenCheckResponse CreateWithInactiveLicense(int userId = 1)
	{
		return new TokenCheckResponse
		{
			Success = true,
			Valid = true,
			ExpiresAt = DateTime.UtcNow.AddDays(30.0),
			User = UserDto.CreateWithInactiveLicense(userId)
		};
	}
}
