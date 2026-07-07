using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using ReactiveUI;
using Serilog;

namespace Ciphra.VPN.Common.ViewModels;

public class AppShellViewModel : ReactiveObject
{
	public ICommand Init { get; }

	public IObservable<Unit> Initialized { get; }

	public SubscriptionManager SubscriptionManager { get; }

	public AppShellViewModel(MainPageViewModel mainPageViewModel, AccountInfoViewModel accountInfoViewModel, SubscriptionManager subscriptionManager, LocationsPageViewModel locationsPageViewModel, Settings settings, IAppAnalytics appAnalytics)
	{
		if (mainPageViewModel == null)
		{
			throw new ArgumentNullException("mainPageViewModel");
		}
		if (accountInfoViewModel == null)
		{
			throw new ArgumentNullException("accountInfoViewModel");
		}
		if (subscriptionManager == null)
		{
			throw new ArgumentNullException("subscriptionManager");
		}
		if (locationsPageViewModel == null)
		{
			throw new ArgumentNullException("locationsPageViewModel");
		}
		if (settings == null)
		{
			throw new ArgumentNullException("settings");
		}
		if (appAnalytics == null)
		{
			throw new ArgumentNullException("appAnalytics");
		}
		SubscriptionManager = subscriptionManager;
		ReactiveCommand<Unit, Unit> val = ReactiveCommand.Create<Unit, Unit>((Func<Unit, Unit>)((Unit _) => _), (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> val2 = ReactiveCommand.CreateFromTask((Func<Task>)appAnalytics.IdentifyAsync, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommandMixins.InvokeCommand<Unit, Unit>((IObservable<Unit>)val, (ReactiveCommandBase<Unit, Unit>)(object)val2);
		IObservable<Unit> observable = Observable.Delay<Unit>((IObservable<Unit>)val, TimeSpan.FromSeconds(2L), RxSchedulers.MainThreadScheduler);
		ReactiveCommandMixins.InvokeCommand<Unit>(observable, accountInfoViewModel.ValidateToken);
		ReactiveCommandMixins.InvokeCommand<Unit>(observable, subscriptionManager.CheckSubscriptionActive);
		ReactiveCommandMixins.InvokeCommand<Unit>(observable, mainPageViewModel.RestoreLastUsedServer);
		ReactiveCommandMixins.InvokeCommand<Unit>(observable, subscriptionManager.GetPrices);
		ReactiveCommandMixins.InvokeCommand<Unit>(Observable.Where<Unit>(observable, (Func<Unit, bool>)((Unit _) => settings.IsUserTokenValid)), locationsPageViewModel.GetServers);
		Init = (ICommand)val;
		Initialized = observable;
		ObservableExtensions.Subscribe<Exception>(((ReactiveCommandBase<Unit, Unit>)(object)val2).ThrownExceptions, (Action<Exception>)delegate(Exception ex)
		{
			Log.Error(ex, "Failed to identify user");
		});
	}
}
