using System.Collections.Generic;

namespace Ga4.Trackers;

public class TrackEvent
{
	public required string EventName { get; init; }

	public Dictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object>();
}
