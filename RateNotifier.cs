using System;
using Serilog;

namespace Ciphra.VPN.Common.App;

public class RateNotifier
{
	private const int MaxRetry = 3;

	private const int RateThreshold = 10;

	private readonly Settings _settings;

	public RateNotifier(Settings settings)
	{
		_settings = settings ?? throw new ArgumentNullException("settings");
	}

	public void ResetToRetryNotification()
	{
		if (!_settings.IsRated && _settings.RatingRetryCounter <= 3)
		{
			Log.Information("Resetting rating notification retry counter.");
			_settings.RatingCounter = 0;
			_settings.RatingRetryCounter++;
		}
	}

	public bool TriggerNotification()
	{
		if (_settings.IsRated)
		{
			return false;
		}
		if (_settings.RatingRetryCounter > 3)
		{
			return false;
		}
		if (_settings.RatingCounter >= 10)
		{
			Log.Information<int>("Triggering rate notification after {Counter} attempts.", _settings.RatingCounter);
			return true;
		}
		_settings.RatingCounter++;
		return false;
	}
}
