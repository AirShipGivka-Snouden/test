using System;

namespace Ciphra.VPN.Common.App;

public class PromoNotifier
{
	private const int PromoThreshold = 10;

	private readonly Settings _settings;

	public PromoNotifier(Settings settings)
	{
		_settings = settings ?? throw new ArgumentNullException("settings");
	}

	public bool TriggerNotification()
	{
		if (_settings.PromoCounter >= 10)
		{
			_settings.PromoCounter = 0;
			return true;
		}
		_settings.PromoCounter++;
		return false;
	}
}
