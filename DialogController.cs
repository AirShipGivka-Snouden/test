using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;

namespace Ciphra.VPN.Common.App;

public class DialogController
{
	private bool _trialDialogShownToday;

	public ICommand ShowDialog { get; }

	public IObservable<ShowDialogIntent> ShowDialogObservable { get; }

	public IObservable<ShowDialogIntent> ShowTrialDialogObservable { get; }

	public IObservable<ShowDialogIntent> ShowTrafficLimitDialogObservable { get; }

	public DialogController(Settings settings)
	{
		DialogController dialogController = this;
		IObservable<ShowDialogIntent> observable = Observable.RefCount<ShowDialogIntent>(Observable.Publish<ShowDialogIntent>((IObservable<ShowDialogIntent>)(ShowDialog = (ICommand)ReactiveCommand.Create<ShowDialogIntent, ShowDialogIntent>((Func<ShowDialogIntent, ShowDialogIntent>)((ShowDialogIntent x) => x), (IObservable<bool>)null, (IScheduler)null))));
		ShowDialogObservable = Observable.ObserveOn<ShowDialogIntent>(observable, RxSchedulers.MainThreadScheduler);
		ShowTrialDialogObservable = Observable.ObserveOn<ShowDialogIntent>(Observable.Where<ShowDialogIntent>(observable, (Func<ShowDialogIntent, bool>)delegate(ShowDialogIntent intent)
		{
			if (intent != ShowDialogIntent.TrialDialog)
			{
				return false;
			}
			if (dialogController._trialDialogShownToday)
			{
				return false;
			}
			DateTime trialDialogDismissedDate = settings.TrialDialogDismissedDate;
			if (trialDialogDismissedDate != DateTime.MinValue && trialDialogDismissedDate.Date >= DateTime.UtcNow.Date)
			{
				return false;
			}
			dialogController._trialDialogShownToday = true;
			settings.TrialDialogDismissedDate = DateTime.UtcNow;
			return true;
		}), RxSchedulers.MainThreadScheduler);
		ShowTrafficLimitDialogObservable = Observable.ObserveOn<ShowDialogIntent>(Observable.Throttle<ShowDialogIntent>(Observable.Where<ShowDialogIntent>(observable, (Func<ShowDialogIntent, bool>)((ShowDialogIntent i) => i == ShowDialogIntent.TrafficLimitDialog)), TimeSpan.FromSeconds(2L)), RxSchedulers.MainThreadScheduler);
	}
}
