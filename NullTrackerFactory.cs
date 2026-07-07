using System;
using System.Collections.Generic;
using Ga4.Trackers;
using VpnHood.Core.Toolkit.Trackers;

namespace VpnHood.Core.Client.VpnServices.Abstractions.Tracking;

public class NullTrackerFactory : ITrackerFactory
{
	public static ITracker CreateNullTracker(TrackerCreateParams createParams)
	{
		return new NullTracker
		{
			MeasurementId = "NullTracker",
			ClientId = createParams.ClientId,
			SessionId = Guid.NewGuid().ToString(),
			UserProperties = new Dictionary<string, object> { 
			{
				"client_version",
				createParams.ClientVersion.ToString(3)
			} }
		};
	}

	public ITracker CreateTracker(TrackerCreateParams createParams)
	{
		return CreateNullTracker(createParams);
	}
}
