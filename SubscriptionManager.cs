using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services.Interfaces;
using ReactiveUI;
using Serilog;

namespace Ciphra.VPN.Common.ViewModels;

public class SubscriptionManager : ReactiveObject, IExceptionBroadcaster
{
	private const string DefaultPrice1 = "$4.99";

	private const string DefaultPrice3 = "$11.99";

	private const string DefaultPrice12 = "$39.99";

	private readonly IStoreService _storeService;

	private bool _isSubscriptionActive;

	private DateTimeOffset _subscriptionExpirationDate;

	private string _price1 = "$4.99";

	private string _price3 = "$11.99";

	private string _price12 = "$39.99";

	private IDisposable _subscriptionStatusSubscription;

	private ICommand CheckSubStatusSilent { get; }

	public ICommand CheckSubscriptionActive { get; }

	public ICommand GetPrices { get; }

	public ICommand Buy1 { get; }

	public ICommand Buy3 { get; }

	public ICommand Buy12 { get; }

	public ICommand Restore { get; }

	public string Price1
	{
		get
		{
			return _price1;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SubscriptionManager, string>(this, ref _price1, value, "Price1");
		}
	}

	public string Price3
	{
		get
		{
			return _price3;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SubscriptionManager, string>(this, ref _price3, value, "Price3");
		}
	}

	public string Price12
	{
		get
		{
			return _price12;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SubscriptionManager, string>(this, ref _price12, value, "Price12");
		}
	}

	public bool IsSubscriptionActive
	{
		get
		{
			return _isSubscriptionActive;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SubscriptionManager, bool>(this, ref _isSubscriptionActive, value, "IsSubscriptionActive");
		}
	}

	public DateTimeOffset SubscriptionExpirationDate
	{
		get
		{
			return _subscriptionExpirationDate;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SubscriptionManager, DateTimeOffset>(this, ref _subscriptionExpirationDate, value, "SubscriptionExpirationDate");
		}
	}

	public IObservable<Unit> CloseDialogObservable { get; }

	public IObservable<SubscriptionStatusResponse> SubscriptionStatusObservable { get; }

	public IObservable<(Exception ex, string source)> ExceptionObservable { get; }

	public SubscriptionManager(IStoreService storeService, IAppAnalytics appAnalytics)
	{
		SubscriptionManager subscriptionManager = this;
		if (appAnalytics == null)
		{
			throw new ArgumentNullException("appAnalytics");
		}
		_storeService = storeService ?? throw new ArgumentNullException("storeService");
		ReactiveCommand<Unit, SubscriptionStatusResponse> val = ReactiveCommand.CreateFromTask<SubscriptionStatusResponse>((Func<CancellationToken, Task<SubscriptionStatusResponse>>)storeService.GetSubscriptionStatusAsync, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ReactiveCommand<Unit, SubscriptionStatusResponse> val2 = ReactiveCommand.CreateFromTask<SubscriptionStatusResponse>((Func<CancellationToken, Task<SubscriptionStatusResponse>>)storeService.GetSubscriptionStatusAsync, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ReactiveCommand<Unit, (string, string, string)> val3 = ReactiveCommand.CreateFromTask<(string, string, string)>((Func<CancellationToken, Task<(string, string, string)>>)GetPricesAsync, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ReactiveCommand<Unit, string> val4 = ReactiveCommand.CreateFromTask<string>((Func<CancellationToken, Task<string>>)((CancellationToken ct) => storeService.PurchaseItemAsync(SubscriptionType.M1, ct)), (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ReactiveCommand<Unit, string> val5 = ReactiveCommand.CreateFromTask<string>((Func<CancellationToken, Task<string>>)((CancellationToken ct) => storeService.PurchaseItemAsync(SubscriptionType.M3, ct)), (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ReactiveCommand<Unit, string> val6 = ReactiveCommand.CreateFromTask<string>((Func<CancellationToken, Task<string>>)((CancellationToken ct) => storeService.PurchaseItemAsync(SubscriptionType.M12, ct)), (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ReactiveCommand<Unit, SubscriptionStatusResponse> val7 = ReactiveCommand.CreateFromTask<SubscriptionStatusResponse>((Func<CancellationToken, Task<SubscriptionStatusResponse>>)storeService.RestorePurchasesAsync, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ObservableExtensions.Subscribe<(SubscriptionType, string)>(Observable.Merge<(SubscriptionType, string)>(Observable.Merge<(SubscriptionType, string)>(Observable.Select<string, (SubscriptionType, string)>((IObservable<string>)val4, (Func<string, (SubscriptionType, string)>)((string transactionId) => (M1: SubscriptionType.M1, transactionId: transactionId))), Observable.Select<string, (SubscriptionType, string)>((IObservable<string>)val5, (Func<string, (SubscriptionType, string)>)((string transactionId) => (M3: SubscriptionType.M3, transactionId: transactionId)))), Observable.Select<string, (SubscriptionType, string)>((IObservable<string>)val6, (Func<string, (SubscriptionType, string)>)((string transactionId) => (M12: SubscriptionType.M12, transactionId: transactionId)))), (Action<(SubscriptionType, string)>)delegate((SubscriptionType, string transactionId) t)
		{
			appAnalytics.TrackSubscriptionPurchaseIntent(t.Item1, t.transactionId);
		}, (Action<Exception>)delegate(Exception ex)
		{
			Log.Error(ex, "Failed to track subscription purchase");
		});
		ObservableExtensions.Subscribe<SubscriptionStatusResponse>(Observable.ObserveOn<SubscriptionStatusResponse>(Observable.Merge<SubscriptionStatusResponse>(Observable.Merge<SubscriptionStatusResponse>((IObservable<SubscriptionStatusResponse>)val, (IObservable<SubscriptionStatusResponse>)val2), (IObservable<SubscriptionStatusResponse>)val7), RxSchedulers.MainThreadScheduler), (Action<SubscriptionStatusResponse>)delegate(SubscriptionStatusResponse status)
		{
			subscriptionManager.IsSubscriptionActive = status.IsActive;
			subscriptionManager.SubscriptionExpirationDate = status.ExpiryDate;
		}, (Action<Exception>)delegate
		{
			subscriptionManager.IsSubscriptionActive = false;
			subscriptionManager.SubscriptionExpirationDate = DateTimeOffset.MinValue;
		});
		IObservable<string> observable = Observable.Merge<string>(Observable.Merge<string>((IObservable<string>)val4, (IObservable<string>)val5), (IObservable<string>)val6);
		ObservableExtensions.Subscribe<Unit>(Observable.Select<string, Unit>(observable, (Func<string, Unit>)((string _) => Unit.Default)), (Action<Unit>)delegate
		{
			subscriptionManager.TrackSubscriptionStatusAfterPurchaseClick();
		});
		ObservableExtensions.Subscribe<(string, string, string)>(Observable.ObserveOn<(string, string, string)>((IObservable<(string, string, string)>)val3, RxSchedulers.MainThreadScheduler), (Action<(string, string, string)>)delegate((string p1, string p3, string p12) prices)
		{
			subscriptionManager.Price1 = prices.p1 ?? "$4.99";
			subscriptionManager.Price3 = prices.p3 ?? "$11.99";
			subscriptionManager.Price12 = prices.p12 ?? "$39.99";
		}, (Action<Exception>)delegate(Exception ex)
		{
			Log.Error(ex, "Failed to get subscription prices, using defaults.");
			subscriptionManager.Price1 = "$4.99";
			subscriptionManager.Price3 = "$11.99";
			subscriptionManager.Price12 = "$39.99";
		});
		CloseDialogObservable = Observable.Select<string, Unit>(Observable.Where<string>(observable, (Func<string, bool>)((string transactionId) => !string.IsNullOrEmpty(transactionId))), (Func<string, Unit>)((string _) => Unit.Default));
		CheckSubscriptionActive = (ICommand)val;
		SubscriptionStatusObservable = Observable.Merge<SubscriptionStatusResponse>(Observable.Merge<SubscriptionStatusResponse>((IObservable<SubscriptionStatusResponse>)val, (IObservable<SubscriptionStatusResponse>)val2), (IObservable<SubscriptionStatusResponse>)val7);
		CheckSubStatusSilent = (ICommand)val2;
		GetPrices = (ICommand)val3;
		Buy1 = (ICommand)val4;
		Buy3 = (ICommand)val5;
		Buy12 = (ICommand)val6;
		Restore = (ICommand)val7;
		ObservableExtensions.Subscribe<Exception>(((ReactiveCommandBase<Unit, SubscriptionStatusResponse>)(object)val2).ThrownExceptions, (Action<Exception>)delegate(Exception ex)
		{
			Log.Error(ex, "Failed to check subscription");
		});
		ExceptionObservable = Observable.RefCount<(Exception, string)>(Observable.Publish<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, SubscriptionStatusResponse>)(object)val).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "checkSubscriptionActive"))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, (string, string, string)>)(object)val3).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "getPrices")))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, string>)(object)val4).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "buy1")))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, string>)(object)val5).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "buy3")))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, string>)(object)val6).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "buy12")))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, SubscriptionStatusResponse>)(object)val7).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "restore"))))));
	}

	private async Task<(string? p1, string? p3, string? p12)> GetPricesAsync(CancellationToken ct)
	{
		Log.Information("Fetching subscription prices...");
		return (p1: await _storeService.GetItemPrice(SubscriptionType.M1, ct), p3: await _storeService.GetItemPrice(SubscriptionType.M3, ct), p12: await _storeService.GetItemPrice(SubscriptionType.M12, ct));
	}

	private void TrackSubscriptionStatusAfterPurchaseClick()
	{
		Log.Debug("Re-subscribing to subscription status check after purchase click.");
		_subscriptionStatusSubscription?.Dispose();
		_subscriptionStatusSubscription = ReactiveCommandMixins.InvokeCommand<Unit>(Observable.Where<Unit>(Observable.Take<Unit>(Observable.Select<long, Unit>(Observable.Interval(TimeSpan.FromSeconds(20L)), (Func<long, Unit>)((long _) => Unit.Default)), 40), (Func<Unit, bool>)((Unit _) => !IsSubscriptionActive)), CheckSubStatusSilent);
	}
}
