using System;
using Ga4.Trackers;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;

namespace VpnHood.Core.Client.VpnServices.Abstractions.Tracking;

public static class BuiltInTrackerFactoryExtensions
{
	public static ITracker TryCreateTracker(this ITrackerFactory trackerFactory, TrackerCreateParams createParams)
	{
		try
		{
			return trackerFactory.CreateTracker(createParams);
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogWarning(exception, "Failed to create a tracker. Returning a null tracker instead.");
			return NullTrackerFactory.CreateNullTracker(createParams);
		}
	}
}
