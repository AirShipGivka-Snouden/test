using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ga4.Trackers;

public interface ITracker
{
	bool IsEnabled { get; set; }

	Task Track(IEnumerable<TrackEvent> trackEvents, CancellationToken cancellationToken = default(CancellationToken));

	Task Track(TrackEvent trackEvent, CancellationToken cancellationToken = default(CancellationToken));
}
