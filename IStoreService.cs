using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.Models;

namespace Ciphra.VPN.Common.Services.Interfaces;

public interface IStoreService
{
	Task<SubscriptionStatusResponse> GetSubscriptionStatusAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task<string?> GetItemPrice(SubscriptionType subType, CancellationToken cancellationToken = default(CancellationToken));

	Task<string> PurchaseItemAsync(SubscriptionType subType, CancellationToken cancellationToken = default(CancellationToken));

	Task<SubscriptionStatusResponse> RestorePurchasesAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return GetSubscriptionStatusAsync(cancellationToken);
	}
}
