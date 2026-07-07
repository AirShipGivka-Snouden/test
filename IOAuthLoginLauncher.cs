using System.Threading;
using System.Threading.Tasks;

namespace Ciphra.VPN.Common.Services.Interfaces;

public interface IOAuthLoginLauncher
{
	Task LaunchLoginAsync(string authorizeUrl, CancellationToken cancellationToken = default(CancellationToken));
}
