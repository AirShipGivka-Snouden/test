using System;
using System.Collections.Generic;
using Ga4.Trackers;
using Ga4.Trackers.Ga4Tags;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Client.VpnServices.Abstractions.Tracking;

public class BuiltInTrackerFactory : ITrackerFactory
{
	public ITracker CreateTracker(TrackerCreateParams createParams)
	{
		if (string.IsNullOrEmpty(createParams.Ga4MeasurementId))
		{
			throw new InvalidOperationException("AppGa4MeasurementId is required to create a built-in tracker.");
		}
		Ga4TagTracker ga4TagTracker = new Ga4TagTracker
		{
			MeasurementId = createParams.Ga4MeasurementId,
			SessionCount = 1,
			ClientId = createParams.ClientId,
			SessionId = Guid.NewGuid().ToString(),
			UserProperties = new Dictionary<string, object> { 
			{
				"client_version",
				createParams.ClientVersion.ToString(3)
			} }
		};
		if (!string.IsNullOrEmpty(createParams.UserAgent))
		{
			ga4TagTracker.UserAgent = createParams.UserAgent;
		}
		ga4TagTracker.TryTrack(new TrackEvent
		{
			EventName = "session_start"
		});
		return ga4TagTracker;
	}
}
