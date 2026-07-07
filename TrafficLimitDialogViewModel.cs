using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using ReactiveUI;

namespace Ciphra.VPN.Common.ViewModels.DialogViewModels;

public class TrafficLimitDialogViewModel : ReactiveObject
{
	public ICommand GoPremium { get; }

	public ICommand CloseDialog { get; }

	public IObservable<Unit> CloseDialogObservable { get; }

	public TrafficLimitDialogViewModel(DialogController dialogController)
	{
		if (dialogController == null)
		{
			throw new ArgumentNullException("dialogController");
		}
		ReactiveCommand<Unit, Unit> val = ReactiveCommand.Create<Unit, Unit>((Func<Unit, Unit>)((Unit x) => x), (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommandMixins.InvokeCommand<ShowDialogIntent>(Observable.Select<Unit, ShowDialogIntent>(Observable.ObserveOn<Unit>((IObservable<Unit>)val, RxSchedulers.MainThreadScheduler), (Func<Unit, ShowDialogIntent>)((Unit _) => ShowDialogIntent.SubscribeDialog)), dialogController.ShowDialog);
		ReactiveCommand<Unit, Unit> val2 = ReactiveCommand.Create((Action)delegate
		{
		}, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		GoPremium = (ICommand)val;
		CloseDialog = (ICommand)val2;
		CloseDialogObservable = Observable.Merge<Unit>((IObservable<Unit>)val, (IObservable<Unit>)val2);
	}
}
