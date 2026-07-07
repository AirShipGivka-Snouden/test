using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ga4.Trackers;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;

namespace VpnHood.Core.Toolkit.Utils;

public static class TrackerExtensions
{
	extension(ITracker tracker)
	{
		public Task<bool> TryTrack(TrackEvent trackEvent)
		{
			return tracker.TryTrackWithCancellation(trackEvent, CancellationToken.None);
		}

		public async Task<bool> TryTrackWithCancellation(TrackEvent trackEvent, CancellationToken cancellationToken)
		{
			try
			{
				await tracker.Track(trackEvent, cancellationToken).Vhc();
				return true;
			}
			catch (Exception exception)
			{
				VhLogger.Instance.LogDebug(exception, "Failed to track event.");
				return false;
			}
		}

		public async Task<bool> TryTrack(IEnumerable<TrackEvent> trackEvents)
		{
			try
			{
				await tracker.Track(trackEvents).Vhc();
				return true;
			}
			catch (Exception exception)
			{
				VhLogger.Instance.LogDebug(exception, "Failed to track events.");
				return false;
			}
		}

		public Task<bool> TryTrackError(Exception exception, string message, string action)
		{
			return tracker.TryTrackError(exception, message, action, isWarning: false);
		}

		public Task<bool> TryTrackWarningAsync(Exception exception, string message, string action)
		{
			return tracker.TryTrackError(exception, message, action, isWarning: true);
		}

		private Task<bool> TryTrackError(Exception exception, string message, string action, bool isWarning)
		{
			TrackEvent item = new TrackEvent
			{
				EventName = "vh_exception",
				Parameters = new Dictionary<string, object>
				{
					{ "method", action },
					{
						"message",
						message + ", " + exception.Message
					},
					{
						"error_type",
						exception.GetType().Name
					},
					{
						"error_level",
						isWarning ? "warning" : "error"
					}
				}
			};
			return tracker.TryTrack(new _003C_003Ez__ReadOnlySingleElementList<TrackEvent>(item));
		}
	}
}
