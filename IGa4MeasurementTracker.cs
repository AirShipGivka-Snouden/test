using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ga4.Trackers.Ga4Measurements;

public interface IGa4MeasurementTracker : ITracker
{
	Task Track(IEnumerable<Ga4MeasurementEvent> ga4Events, CancellationToken cancellationToken);
}
