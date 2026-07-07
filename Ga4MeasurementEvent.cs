using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ga4.Trackers.Ga4Measurements;

public class Ga4MeasurementEvent : ICloneable
{
	[JsonPropertyName("name")]
	public required string EventName { get; init; }

	[JsonPropertyName("params")]
	public Dictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object>();

	public object Clone()
	{
		return new Ga4MeasurementEvent
		{
			EventName = EventName,
			Parameters = new Dictionary<string, object>(Parameters, StringComparer.OrdinalIgnoreCase)
		};
	}
}
