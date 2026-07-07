using System;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Services;
using ReactiveUI;

namespace Ciphra.VPN.Common.ViewModels.DialogViewModels;

public class AuthDialogViewModel : ReactiveObject, IExceptionBroadcaster
{
	public enum AuthDialogState
	{
		Initializing,
		Validating,
		TokenValid,
		TokenInvalid,
		TokenCreated,
		ErrorOccurred
	}

	private string _userToken = string.Empty;

	private bool _isTokenValid = false;

	private AuthDialogState _state = AuthDialogState.Initializing;

	public ICommand CreateToken { get; }

	public ICommand ValidateToken { get; }

	public ICommand Activate { get; }

	public IObservable<Unit> ActivatedObservable { get; }

	public string UserToken
	{
		get
		{
			return _userToken;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<AuthDialogViewModel, string>(this, ref _userToken, value, "UserToken");
		}
	}

	public bool IsTokenValid
	{
		get
		{
			return _isTokenValid;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<AuthDialogViewModel, bool>(this, ref _isTokenValid, value, "IsTokenValid");
		}
	}

	public AuthDialogState State
	{
		get
		{
			return _state;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<AuthDialogViewModel, AuthDialogState>(this, ref _state, value, "State");
		}
	}

	public IObservable<(Exception ex, string source)> ExceptionObservable { get; }

	public AuthDialogViewModel(Settings settings, AuthProvider authProvider, AccountInfoViewModel accountInfoViewModel)
	{
		AuthDialogViewModel authDialogViewModel = this;
		if (settings == null)
		{
			throw new ArgumentNullException("settings");
		}
		if (authProvider == null)
		{
			throw new ArgumentNullException("authProvider");
		}
		if (accountInfoViewModel == null)
		{
			throw new ArgumentNullException("accountInfoViewModel");
		}
		ReactiveCommand<string, TokenValidationResult> val = ReactiveCommand.CreateFromTask<string, TokenValidationResult>((Func<string, CancellationToken, Task<TokenValidationResult>>)((string token, CancellationToken ct) => authProvider.ValidateToken(token, ct)), (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ReactiveCommand<Unit, string> val2 = ReactiveCommand.CreateFromTask<string>((Func<CancellationToken, Task<string>>)authProvider.CreateNewToken, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ReactiveCommand<Unit, Unit> val3 = ReactiveCommand.Create((Action)delegate
		{
			accountInfoViewModel.UserToken = authDialogViewModel.UserToken;
			accountInfoViewModel.IsUserTokenValid = authDialogViewModel.IsTokenValid;
		}, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ObservableExtensions.Subscribe<bool>(Observable.ObserveOn<bool>(((ReactiveCommandBase<string, TokenValidationResult>)(object)val).IsExecuting, RxSchedulers.MainThreadScheduler), (Action<bool>)delegate(bool isExecuting)
		{
			if (isExecuting)
			{
				authDialogViewModel.State = AuthDialogState.Validating;
			}
		});
		ObservableExtensions.Subscribe<TokenValidationResult>(Observable.ObserveOn<TokenValidationResult>((IObservable<TokenValidationResult>)val, RxSchedulers.MainThreadScheduler), (Action<TokenValidationResult>)delegate(TokenValidationResult t)
		{
			authDialogViewModel.State = (t.IsValid ? AuthDialogState.TokenValid : AuthDialogState.TokenInvalid);
			authDialogViewModel.IsTokenValid = t.IsValid;
		}, (Action<Exception>)delegate
		{
			authDialogViewModel.IsTokenValid = false;
		});
		ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>((IObservable<string>)val2, RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string token)
		{
			authDialogViewModel.State = AuthDialogState.TokenCreated;
			authDialogViewModel.UserToken = token;
			authDialogViewModel.IsTokenValid = true;
		}, (Action<Exception>)delegate
		{
			authDialogViewModel.IsTokenValid = false;
		});
		ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(WhenAnyMixin.WhenAnyValue<AccountInfoViewModel, string>(accountInfoViewModel, (Expression<Func<AccountInfoViewModel, string>>)((AccountInfoViewModel a) => a.UserToken)), RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string token)
		{
			if (!string.IsNullOrWhiteSpace(token) && token != authDialogViewModel.UserToken)
			{
				authDialogViewModel.UserToken = token;
			}
		});
		ReactiveCommandMixins.InvokeCommand<string, TokenValidationResult>(Observable.ObserveOn<string>(Observable.Throttle<string>(Observable.Where<string>(WhenAnyMixin.WhenAnyValue<AuthDialogViewModel, string>(this, (Expression<Func<AuthDialogViewModel, string>>)((AuthDialogViewModel t) => t.UserToken)), (Func<string, bool>)((string token) => !string.IsNullOrWhiteSpace(token) && token.Length > 8)), TimeSpan.FromSeconds(1L)), RxSchedulers.MainThreadScheduler), (ReactiveCommandBase<string, TokenValidationResult>)(object)val);
		ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(WhenAnyMixin.WhenAnyValue<AuthDialogViewModel, string>(this, (Expression<Func<AuthDialogViewModel, string>>)((AuthDialogViewModel t) => t.UserToken)), RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string t)
		{
			if (string.IsNullOrEmpty(t))
			{
				authDialogViewModel.IsTokenValid = false;
				authDialogViewModel.State = AuthDialogState.Initializing;
			}
		});
		UserToken = settings.UserToken ?? string.Empty;
		CreateToken = (ICommand)val2;
		ValidateToken = (ICommand)val;
		Activate = (ICommand)val3;
		ActivatedObservable = (IObservable<Unit>)val3;
		ExceptionObservable = Observable.RefCount<(Exception, string)>(Observable.Publish<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<string, TokenValidationResult>)(object)val).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "validateToken"))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, string>)(object)val2).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "createNewToken")))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, Unit>)(object)val3).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "activate"))))));
	}
}
