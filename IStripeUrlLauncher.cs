using System.Threading;
using System.Threading.Tasks;

namespace Ciphra.VPN.Common.Services.Interfaces;

public interface IStripeUrlLauncher
{
	bool IsEnabled => true;

	Task<bool> LaunchStripeUrlAsync(string url, CancellationToken cancellationToken = default(CancellationToken));
}
