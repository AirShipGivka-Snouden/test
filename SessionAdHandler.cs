using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.Messaging;
using VpnHood.Core.Tunneling.Utils;

namespace VpnHood.Core.Client;

internal class SessionAdHandler(ClientSession session) : ISessionAdHandler, IDisposable
{
	private TaskCompletionSource _waitForAdCts = VhUtils.CreateCompletedTcs();

	private readonly Lock _waitForAdLock = new Lock();

	public bool IsWaitingForAd => !_waitForAdCts.Task.IsCompleted;

	public event EventHandler? IsWaitingForChanged;

	public async Task<Exception?> TryWaitForAd(CancellationToken cancellationToken)
	{
		try
		{
			await WaitForAd(cancellationToken);
			return null;
		}
		catch (Exception ex)
		{
			VhLogger.Instance.LogError(ex, "Failed to wait for ad.");
			return ex;
		}
	}

	public Task WaitForAd(CancellationToken cancellationToken)
	{
		using (_waitForAdLock.EnterScope())
		{
			return _waitForAdCts.Task.IsCompleted ? WaitForAdInternal(cancellationToken) : _waitForAdCts.Task.WaitAsync(cancellationToken);
		}
	}

	private async Task WaitForAdInternal(CancellationToken cancellationToken)
	{
		try
		{
			_waitForAdCts = new TaskCompletionSource();
			session.PassthroughState.PassthroughForAd = true;
			this.IsWaitingForChanged?.Invoke(this, EventArgs.Empty);
			await _waitForAdCts.Task.WaitAsync(cancellationToken);
		}
		finally
		{
			session.PassthroughState.PassthroughForAd = false;
			session.DropCurrentConnections();
			this.IsWaitingForChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public void SetAdOk()
	{
		_waitForAdCts.TrySetResult();
	}

	public void SetAdFailed(Exception ex)
	{
		_waitForAdCts.TrySetException(ex);
	}

	public async Task SendRewardedAdData(string rewardedAdData, CancellationToken cancellationToken)
	{
		try
		{
			using (await session.SendRequest<SessionResponse>(new RewardedAdRequest
			{
				RequestId = UniqueIdFactory.Create(),
				SessionId = session.Config.SessionId,
				SessionKey = session.Config.SessionKey,
				AdData = rewardedAdData
			}, cancellationToken).Vhc())
			{
				SetAdOk();
			}
		}
		catch (Exception adFailed)
		{
			SetAdFailed(adFailed);
			throw;
		}
	}

	public void Dispose()
	{
		_waitForAdCts.TrySetCanceled();
	}
}
