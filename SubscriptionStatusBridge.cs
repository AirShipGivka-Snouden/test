using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services.Interfaces;
using Ciphra.VPN.Common.ViewModels;
using ReactiveUI;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public class SubscriptionStatusBridge : IExceptionBroadcaster
{
	private readonly ApiService _apiService;

	private readonly AccountInfoViewModel _accountInfoViewModel;

	private readonly IDeviceIdService _deviceIdService;

	public IObservable<(Exception ex, string source)> ExceptionObservable { get; }

	public SubscriptionStatusBridge(ApiService apiService, SubscriptionManager subscriptionManager, AccountInfoViewModel accountInfoViewModel, IDeviceIdService deviceIdService)
	{
		if (subscriptionManager == null)
		{
			throw new ArgumentNullException("subscriptionManager");
		}
		_apiService = apiService ?? throw new ArgumentNullException("apiService");
		_accountInfoViewModel = accountInfoViewModel ?? throw new ArgumentNullException("accountInfoViewModel");
		_deviceIdService = deviceIdService ?? throw new ArgumentNullException("deviceIdService");
		ReactiveCommand<SubscriptionStatusResponse, Unit> val = ReactiveCommand.CreateFromTask<SubscriptionStatusResponse>((Func<SubscriptionStatusResponse, CancellationToken, Task>)ReportSubscriptionStatus, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommandMixins.InvokeCommand<SubscriptionStatusResponse, Unit>(subscriptionManager.SubscriptionStatusObservable, (ReactiveCommandBase<SubscriptionStatusResponse, Unit>)(object)val);
		ReactiveCommandMixins.InvokeCommand<Unit>((IObservable<Unit>)val, accountInfoViewModel.ValidateToken);
		ExceptionObservable = Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<SubscriptionStatusResponse, Unit>)(object)val).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "reportSubStatus")));
	}

	private async Task ReportSubscriptionStatus(SubscriptionStatusResponse subscriptionStatus, CancellationToken ct)
	{
		try
		{
			string token = _accountInfoViewModel.UserToken;
			if (string.IsNullOrEmpty(token) || token.Contains("Not set", StringComparison.OrdinalIgnoreCase))
			{
				Log.Warning("SubscriptionStatusBridge: User token is not set; skipping subscription status report.");
				return;
			}
			string deviceId = await _deviceIdService.GetDeviceId();
			string platform = _deviceIdService.GetPlatform();
			await _apiService.UpdateSubscriptionAsync(platform, token, subscriptionStatus.SubscriptionLevel, subscriptionStatus.ExpiryDate, deviceId, subscriptionStatus.PurchaseToken, ct);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Failed to report subscription status");
		}
	}
}
