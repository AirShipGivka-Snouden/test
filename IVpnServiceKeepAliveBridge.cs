using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.Models;

namespace Ciphra.VPN.Common.Services;

internal interface IVpnServiceKeepAliveBridge
{
	Task<HealthSnapshot> SampleHealthAsync(CancellationToken ct);

	Task<VpnServerDto?> RefreshAccessKeyAsync(VpnServerDto server, CancellationToken ct);

	Task ReconnectAsync(VpnServerDto server, ConnectionSettings? overrides, CancellationToken ct);

	Task<SmartHealCandidate?> TrySmartHealAsync(VpnServerDto server, ConnectionSettings original, CancellationToken ct);
}
