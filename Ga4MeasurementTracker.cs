using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ga4.Trackers.Ga4Measurements;

public class Ga4MeasurementTracker : TrackerBase, IGa4MeasurementTracker, ITracker
{
	public required string ApiSecret { get; init; }

	public bool IsDebugEndPoint { get; set; }

	public Task Track(Ga4MeasurementEvent ga4Event, CancellationToken cancellationToken)
	{
		Ga4MeasurementEvent[] ga4Events = new Ga4MeasurementEvent[1] { ga4Event };
		return Track(ga4Events, cancellationToken);
	}

	public Task Track(IEnumerable<Ga4MeasurementEvent> ga4Events, CancellationToken cancellationToken)
	{
		if (!base.IsEnabled)
		{
			return Task.CompletedTask;
		}
		Ga4MeasurementEvent[] array = ga4Events.Select((Ga4MeasurementEvent x) => (Ga4MeasurementEvent)x.Clone()).ToArray();
		if (!array.Any())
		{
			throw new ArgumentException("Events can not be empty! ", "ga4Events");
		}
		Ga4MeasurementEvent[] array2 = array;
		foreach (Ga4MeasurementEvent ga4MeasurementEvent in array2)
		{
			if (base.IsAdminDebugView && !ga4MeasurementEvent.Parameters.TryGetValue("debug_mode", out object value))
			{
				ga4MeasurementEvent.Parameters.Add("debug_mode", 1);
			}
			if (!string.IsNullOrEmpty(base.SessionId) && !ga4MeasurementEvent.Parameters.TryGetValue("session_id", out value))
			{
				ga4MeasurementEvent.Parameters.Add("session_id", base.SessionId);
			}
		}
		Ga4MeasurementPayload jsonData = new Ga4MeasurementPayload
		{
			ClientId = base.ClientId,
			UserId = base.UserId,
			Events = array,
			UserProperties = (base.UserProperties.Any() ? base.UserProperties.ToDictionary<KeyValuePair<string, object>, string, Ga4MeasurementPayload.UserProperty>((KeyValuePair<string, object> p) => p.Key, (KeyValuePair<string, object> p) => new Ga4MeasurementPayload.UserProperty
			{
				Value = p.Value
			}) : null)
		};
		Uri baseUri = (IsDebugEndPoint ? new Uri("https://www.google-analytics.com/debug/mp/collect") : new Uri("https://www.google-analytics.com/mp/collect"));
		HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "?api_secret=" + ApiSecret + "&measurement_id=" + base.MeasurementId));
		PrepareHttpHeaders(httpRequestMessage.Headers);
		return SendHttpRequest(httpRequestMessage, "Measurement", jsonData, cancellationToken);
	}

	public override Task Track(IEnumerable<TrackEvent> trackEvents, CancellationToken cancellationToken)
	{
		IEnumerable<Ga4MeasurementEvent> ga4Events = trackEvents.Select((TrackEvent x) => new Ga4MeasurementEvent
		{
			EventName = x.EventName,
			Parameters = x.Parameters
		});
		return Track(ga4Events, cancellationToken);
	}
}
