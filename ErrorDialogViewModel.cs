using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;

namespace Ciphra.VPN.Common.ViewModels.DialogViewModels;

public class ErrorDialogViewModel : ReactiveObject
{
	private string _errorMessage;

	public string ErrorMessage
	{
		get
		{
			return _errorMessage;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<ErrorDialogViewModel, string>(this, ref _errorMessage, value, "ErrorMessage");
		}
	}

	public ICommand CloseDialog { get; }

	public IObservable<Unit> CloseDialogObservable { get; }

	public ErrorDialogViewModel()
	{
		CloseDialogObservable = Observable.Select<Unit, Unit>((IObservable<Unit>)(CloseDialog = (ICommand)ReactiveCommand.Create((Action)delegate
		{
		}, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler)), (Func<Unit, Unit>)((Unit _) => Unit.Default));
	}
}
