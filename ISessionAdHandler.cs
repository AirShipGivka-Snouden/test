using System;
using System.Threading;
using System.Threading.Tasks;

namespace VpnHood.Core.Client;

public interface ISessionAdHandler : IDisposable
{
	bool IsWaitingForAd { get; }

	event EventHandler? IsWaitingForChanged;

	Task<Exception?> TryWaitForAd(CancellationToken cancellationToken);

	Task WaitForAd(CancellationToken cancellationToken);

	void SetAdOk();

	Task SendRewardedAdData(string rewardedAdData, CancellationToken cancellationToken);

	void SetAdFailed(Exception ex);
}
