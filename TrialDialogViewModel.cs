using System;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Services.Interfaces;
using ReactiveUI;

namespace Ciphra.VPN.Common.ViewModels.DialogViewModels;

public class TrialDialogViewModel : ReactiveObject, IExceptionBroadcaster
{
	private const string PaymentPageUrl = "https://www.ciphravpn.com/payment";

	private string _trialDaysLeftText;

	private bool _isTrialExpired;

	public ICommand LaunchPaymentsPage { get; }

	public ICommand Later { get; }

	public IObservable<Unit> CloseDialogObservable { get; }

	public IObservable<(Exception ex, string source)> ExceptionObservable { get; }

	public bool IsTrialExpired
	{
		get
		{
			return _isTrialExpired;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<TrialDialogViewModel, bool>(this, ref _isTrialExpired, value, "IsTrialExpired");
		}
	}

	public string TrialDaysLeftText
	{
		get
		{
			return _trialDaysLeftText;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<TrialDialogViewModel, string>(this, ref _trialDaysLeftText, value, "TrialDaysLeftText");
		}
	}

	public AccountInfoViewModel AccountInfo { get; private set; }

	public bool IsExternalPurchaseAllowed { get; }

	public TrialDialogViewModel(AccountInfoViewModel accountInfo, IStripeUrlLauncher urlLauncher, Settings settings)
	{
		TrialDialogViewModel trialDialogViewModel = this;
		if (accountInfo == null)
		{
			throw new ArgumentNullException("accountInfo");
		}
		if (urlLauncher == null)
		{
			throw new ArgumentNullException("urlLauncher");
		}
		if (settings == null)
		{
			throw new ArgumentNullException("settings");
		}
		AccountInfo = accountInfo;
		IsExternalPurchaseAllowed = urlLauncher.IsEnabled;
		_trialDaysLeftText = string.Empty;
		_isTrialExpired = false;
		ReactiveCommand<Unit, bool> val = ReactiveCommand.CreateFromTask<bool>((Func<CancellationToken, Task<bool>>)((CancellationToken ct) => urlLauncher.LaunchStripeUrlAsync("https://www.ciphravpn.com/payment", ct)), (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ReactiveCommand<Unit, Unit> val2 = ReactiveCommand.Create((Action)delegate
		{
			settings.TrialDialogDismissedDate = DateTime.UtcNow;
		}, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ObservableExtensions.Subscribe<DateTime?>(Observable.ObserveOn<DateTime?>(Observable.Where<DateTime?>(WhenAnyMixin.WhenAnyValue<AccountInfoViewModel, DateTime?>(accountInfo, (Expression<Func<AccountInfoViewModel, DateTime?>>)((AccountInfoViewModel t) => t.TrialExpirationDate)), (Func<DateTime?, bool>)((DateTime? t) => t.HasValue)), RxSchedulers.MainThreadScheduler), (Action<DateTime?>)delegate(DateTime? expiryDate)
		{
			if (expiryDate.HasValue)
			{
				int days = (expiryDate.Value.Date - DateTime.UtcNow.Date).Days;
				if (days > 0)
				{
					trialDialogViewModel.TrialDaysLeftText = days.ToString();
					trialDialogViewModel.IsTrialExpired = false;
				}
				else
				{
					trialDialogViewModel.TrialDaysLeftText = "0";
					trialDialogViewModel.IsTrialExpired = true;
				}
			}
		});
		ExceptionObservable = Observable.RefCount<(Exception, string)>(Observable.Publish<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, bool>)(object)val).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "launchPaymentsPage"))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, Unit>)(object)val2).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "later"))))));
		CloseDialogObservable = Observable.Merge<Unit>(Observable.Select<bool, Unit>((IObservable<bool>)val, (Func<bool, Unit>)((bool _) => Unit.Default)), (IObservable<Unit>)val2);
		LaunchPaymentsPage = (ICommand)val;
		Later = (ICommand)val2;
	}
}
