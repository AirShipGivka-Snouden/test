using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ga4.Trackers;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;

namespace VpnHood.Core.Toolkit.Trackers;

public class NullTracker : TrackerBase
{
	public override Task Track(IEnumerable<TrackEvent> trackEvents, CancellationToken cancellationToken)
	{
		trackEvents = trackEvents.Select((TrackEvent x) => new TrackEvent
		{
			EventName = x.EventName,
			Parameters = x.Parameters.ToDictionary<KeyValuePair<string, object>, string, object>((KeyValuePair<string, object> kvp) => kvp.Key, (KeyValuePair<string, object> kvp) => kvp.Value?.ToString())
		});
		VhLogger.Instance.LogDebug("TrackEvent. {TrackEvent}", JsonSerializer.Serialize(trackEvents));
		return Task.CompletedTask;
	}
}
