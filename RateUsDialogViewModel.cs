using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using ReactiveUI;
using Serilog;

namespace Ciphra.VPN.Common.ViewModels.DialogViewModels;

public class RateUsDialogViewModel : IExceptionBroadcaster
{
	private readonly RateNotifier _rateNotifier;

	private readonly IFeedbackLauncher _feedbackLauncher;

	private readonly IAppAnalytics _appAnalytics;

	public ICommand Like { get; }

	public ICommand Dislike { get; }

	public ICommand Later { get; }

	public IObservable<Unit> CloseDialogObservable { get; }

	public IObservable<(Exception ex, string source)> ExceptionObservable { get; }

	public RateUsDialogViewModel(Settings settings, RateNotifier rateNotifier, IFeedbackLauncher feedbackLauncher, IAppAnalytics appAnalytics)
	{
		_rateNotifier = rateNotifier ?? throw new ArgumentNullException("rateNotifier");
		_feedbackLauncher = feedbackLauncher ?? throw new ArgumentNullException("feedbackLauncher");
		_appAnalytics = appAnalytics ?? throw new ArgumentNullException("appAnalytics");
		ReactiveCommand<Unit, string> val = ReactiveCommand.CreateFromTask<string>((Func<Task<string>>)LikeAsync, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ReactiveCommand<Unit, string> val2 = ReactiveCommand.CreateFromTask<string>((Func<Task<string>>)DislikeAsync, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ReactiveCommand<Unit, string> val3 = ReactiveCommand.Create<string>((Func<string>)LaterFunc, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ObservableExtensions.Subscribe<string>(Observable.Merge<string>((IObservable<string>)val, (IObservable<string>)val2), (Action<string>)delegate
		{
			settings.IsRated = true;
		});
		ObservableExtensions.Subscribe<string>(Observable.Merge<string>(Observable.Merge<string>((IObservable<string>)val, (IObservable<string>)val2), (IObservable<string>)val3), (Action<string>)TrackRateUsEvent);
		CloseDialogObservable = Observable.Select<string, Unit>(Observable.Merge<string>(Observable.Merge<string>((IObservable<string>)val, (IObservable<string>)val2), (IObservable<string>)val3), (Func<string, Unit>)((string _) => Unit.Default));
		ExceptionObservable = Observable.RefCount<(Exception, string)>(Observable.Publish<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, string>)(object)val).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "like"))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, string>)(object)val2).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "dislike")))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, string>)(object)val3).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "later"))))));
		Like = (ICommand)val;
		Dislike = (ICommand)val2;
		Later = (ICommand)val3;
	}

	private async Task<string> LikeAsync()
	{
		await _feedbackLauncher.LaunchStoreReviewFormAsync();
		return "LikeIt";
	}

	private async Task<string> DislikeAsync()
	{
		await _feedbackLauncher.LaunchFeedbackFormAsync();
		return "Dislike";
	}

	private string LaterFunc()
	{
		_rateNotifier.ResetToRetryNotification();
		return "Later";
	}

	private void TrackRateUsEvent(string rateAction)
	{
		try
		{
			_appAnalytics.SendEvent("App Rate", ("Action", rateAction));
			Log.Information<string>("App rate action: {Rating}", rateAction);
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Failed to track RateUs event: {RateAction}", rateAction);
		}
	}
}
