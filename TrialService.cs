using System;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services.Interfaces;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public class TrialService : ITrialService
{
	private readonly ApiService _apiService;

	private readonly IDeviceIdService _deviceIdService;

	private readonly Settings _settings;

	public TrialService(ApiService apiService, IDeviceIdService deviceIdService, Settings settings)
	{
		_apiService = apiService ?? throw new ArgumentNullException("apiService");
		_deviceIdService = deviceIdService ?? throw new ArgumentNullException("deviceIdService");
		_settings = settings ?? throw new ArgumentNullException("settings");
	}

	public async Task<DateTime?> CheckTrialExpiryDateAsync(CancellationToken ct)
	{
		try
		{
			string deviceId = await _deviceIdService.GetDeviceId();
			TrialCheckResponse response = await _apiService.CheckTrialAsync(deviceId, ct);
			if (!response.Success)
			{
				Log.Warning<string>("Trial check failed: {Message}. Falling back to cached expiry date.", response.Message ?? response.Error);
				return GetCachedTrialExpiryDate();
			}
			if (!response.ExpiryDate.HasValue)
			{
				Log.Information("No trial expiry date found in response. Falling back to cached expiry date.");
				return GetCachedTrialExpiryDate();
			}
			DateTime expiryDate = response.ExpiryDate.Value.ToUniversalTime();
			_settings.TrialExpiryDate = expiryDate;
			Log.Information<DateTime>("Trial expiry date retrieved: {ExpiryDate}", expiryDate);
			return expiryDate;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Error checking trial status. Falling back to cached expiry date.");
			return GetCachedTrialExpiryDate();
		}
	}

	private DateTime? GetCachedTrialExpiryDate()
	{
		DateTime trialExpiryDate = _settings.TrialExpiryDate;
		if (trialExpiryDate == DateTime.MinValue)
		{
			Log.Warning("No cached trial expiry date found.");
			return null;
		}
		Log.Information<DateTime>("Using cached trial expiry date: {ExpiryDate}", trialExpiryDate);
		return trialExpiryDate;
	}
}
