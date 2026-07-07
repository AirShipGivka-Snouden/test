using System.Threading;
using System.Threading.Tasks;

namespace Ciphra.VPN.Common.Services.Interfaces;

public interface IOAuthSecureStorage
{
	Task<OAuthStoredSession?> LoadAsync(CancellationToken ct = default(CancellationToken));

	Task SaveAsync(OAuthStoredSession session, CancellationToken ct = default(CancellationToken));

	Task ClearAsync(CancellationToken ct = default(CancellationToken));
}
