using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Ciphra.VPN.Common.ViewModels.DialogViewModels;
using ReactiveUI;
using Serilog;

namespace Ciphra.VPN.Common.App;

public class AggregateExceptionHandler : IExceptionHandler
{
	private readonly DialogController _dialogController;

	private readonly ErrorDialogViewModel _errorDialogPageViewModel;

	private readonly IAppAnalytics _analytics;

	public AggregateExceptionHandler(DialogController dialogController, ErrorDialogViewModel errorDialogPageViewModel, IExceptionBroadcaster[] exceptionBroadcasters, IAppAnalytics analytics)
	{
		_dialogController = dialogController ?? throw new ArgumentNullException("dialogController");
		_errorDialogPageViewModel = errorDialogPageViewModel ?? throw new ArgumentNullException("errorDialogPageViewModel");
		_analytics = analytics ?? throw new ArgumentNullException("analytics");
		IObservable<(Exception, string)> observable = Observable.SelectMany<IExceptionBroadcaster, (Exception, string)>(Observable.ToObservable<IExceptionBroadcaster>((IEnumerable<IExceptionBroadcaster>)exceptionBroadcasters), (Func<IExceptionBroadcaster, IObservable<(Exception, string)>>)((IExceptionBroadcaster b) => b.ExceptionObservable));
		ObservableExtensions.Subscribe<(Exception, string)>(Observable.ObserveOn<(Exception, string)>(observable, RxSchedulers.MainThreadScheduler), (Action<(Exception, string)>)delegate((Exception ex, string source) t)
		{
			Log.Error<string>(t.ex, "Exception from {Source}", t.source);
			errorDialogPageViewModel.ErrorMessage = t.ex.Message;
		});
		ReactiveCommandMixins.InvokeCommand<ShowDialogIntent>(Observable.Select<(Exception, string), ShowDialogIntent>(Observable.ObserveOn<(Exception, string)>(observable, RxSchedulers.MainThreadScheduler), (Func<(Exception, string), ShowDialogIntent>)(((Exception ex, string source) t) => ShowDialogIntent.ErrorDialog)), dialogController.ShowDialog);
	}

	public void HandleException(Exception ex, string source, bool fatal)
	{
		if (fatal)
		{
			Log.Fatal<string>(ex, "Fatal exception in {Source}", source);
		}
		else
		{
			Log.Error<string>(ex, "Exception in {Source}", source);
		}
		Scheduler.Schedule(RxSchedulers.MainThreadScheduler, (Action)delegate
		{
			_errorDialogPageViewModel.ErrorMessage = ex.Message;
			_dialogController.ShowDialog.Execute(ShowDialogIntent.ErrorDialog);
		});
		try
		{
			_analytics.SendException(ex, source, fatal);
		}
		catch (Exception ex2)
		{
			Log.Debug<string>("Failed to send exception to analytics: {Message}", ex2.Message);
		}
	}
}
