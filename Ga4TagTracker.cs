using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ga4.Trackers.Ga4Tags;

public class Ga4TagTracker : TrackerBase, IGa4TagTracker, ITracker
{
	public required int SessionCount { get; set; } = 1;

	public bool? IsMobile { get; init; }

	public async Task TryTrack(Ga4TagEvent ga4Event, ILogger logger)
	{
		try
		{
			await Track(ga4Event, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception exception)
		{
			logger.LogDebug(exception, "Failed to track GA4 Tag Event: {EventName}", ga4Event.EventName);
		}
	}

	public Task Track(Ga4TagEvent ga4Event, CancellationToken cancellationToken)
	{
		if (!base.IsEnabled)
		{
			return Task.CompletedTask;
		}
		bool flag = IsMobile ?? CheckIsMobileByUserAgent(base.UserAgent);
		List<(string, object)> list = new List<(string, object)>
		{
			("v", 2),
			("tid", base.MeasurementId),
			("gtm", Environment.TickCount),
			("_p", Environment.TickCount + 10),
			("cid", base.ClientId),
			("ul", CultureInfo.CurrentCulture.Name.ToLower()),
			("uaa", RuntimeInformation.ProcessArchitecture.ToString().ToLower()),
			("uab", Environment.Is64BitOperatingSystem ? "64" : "32"),
			("uamb", flag ? 1 : 0),
			("uapv", Environment.OSVersion.Version.ToString(3)),
			("sid", base.SessionId),
			("sct", SessionCount),
			("seg", 1),
			("_s", 1),
			("en", ga4Event.EventName),
			("_ee", "1")
		};
		if (base.UserId != null)
		{
			list.Add(("uid", base.UserId));
		}
		if (ga4Event.DocumentLocation != null)
		{
			list.Add(("dl", ga4Event.DocumentLocation));
		}
		if (ga4Event.DocumentTitle != null)
		{
			list.Add(("dt", ga4Event.DocumentTitle));
		}
		if (ga4Event.DocumentReferrer != null)
		{
			list.Add(("dr", ga4Event.DocumentReferrer));
		}
		if (ga4Event.IsFirstVisit)
		{
			list.Add(("_fv", 1));
		}
		if (ga4Event.EngagementTime.HasValue)
		{
			list.Add(("_et ", ga4Event.EngagementTime));
		}
		if (base.IsAdminDebugView)
		{
			list.Add(("_dbg", 1));
		}
		foreach (KeyValuePair<string, object> userProperty in base.UserProperties)
		{
			object value = userProperty.Value;
			bool flag2 = ((value is int || value is long) ? true : false);
			string text = (flag2 ? "upn" : "up");
			text = text + "." + userProperty.Key;
			list.Add((text, userProperty.Value));
		}
		foreach (KeyValuePair<string, object> property in ga4Event.Properties)
		{
			object value = property.Value;
			bool flag2 = ((value is int || value is long) ? true : false);
			string text2 = (flag2 ? "epn" : "ep");
			text2 = text2 + "." + property.Key;
			list.Add((text2, property.Value));
		}
		Uri requestUri = new Uri("https://www.google-analytics.com/g/collect?" + string.Join('&', list.Select(((string, object) x) => $"{x.Item1}={x.Item2}")));
		HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
		PrepareHttpHeaders(httpRequestMessage.Headers);
		return SendHttpRequest(httpRequestMessage, "GTag", null, cancellationToken);
	}

	public async Task Track(IEnumerable<Ga4TagEvent> ga4Events, CancellationToken cancellationToken)
	{
		foreach (Ga4TagEvent ga4Event in ga4Events)
		{
			await Track(ga4Event, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public override Task Track(IEnumerable<TrackEvent> trackEvents, CancellationToken cancellationToken)
	{
		IEnumerable<Ga4TagEvent> ga4Events = trackEvents.Select((TrackEvent x) => new Ga4TagEvent
		{
			EventName = x.EventName,
			Properties = x.Parameters
		});
		return Track(ga4Events, cancellationToken);
	}

	private static bool CheckIsMobileByUserAgent(string? userAgent)
	{
		if (string.IsNullOrEmpty(userAgent))
		{
			return false;
		}
		Regex regex = new Regex("(android|bb\\d+|meego).+mobile|avantgo|bada\\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\\.(browser|link)|vodafone|wap|windows ce|xda|xiino", RegexOptions.IgnoreCase);
		if (!string.IsNullOrEmpty(userAgent))
		{
			return regex.IsMatch(userAgent);
		}
		return false;
	}
}
