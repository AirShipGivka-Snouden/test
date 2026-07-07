using System.Threading;
using System.Threading.Tasks;

namespace Ga4.Trackers.Ga4Tags;

public interface IGa4TagTracker : ITracker
{
	Task Track(Ga4TagEvent ga4Event, CancellationToken cancellationToken);
}
