using System;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services;
using Ciphra.VPN.Common.Services.Interfaces;
using Ciphra.VPN.Common.Services.OnChain;
using ReactiveUI;
using Serilog;

namespace Ciphra.VPN.Common.ViewModels;

public class AccountInfoViewModel : ReactiveObject, IExceptionBroadcaster
{
	public enum AccountInfoState
	{
		Initializing,
		CheckingLicense,
		InvalidToken,
		TokenRejected,
		NetworkError,
		TrialActive,
		TrialExpired,
		Error,
		Completed
	}

	public enum SubscriptionStatus
	{
		Inactive = 0,
		Proxy = 1,
		Active = 2,
		Expired = 99,
		Unknown = 100
	}

	private sealed record LicenseCheckResult(TokenValidationResult tokenValidationResult, DateTime? trialExpirationDate, string? updatedUserToken = null, bool markTokenValid = false);

	private readonly AuthProvider _authProvider;

	private readonly ITrialService _trialService;

	private readonly Settings _settings;

	private readonly DialogController _dialogController;

	private string? _userToken = string.Empty;

	private string? _onChainStatusOverride;

	private AccountInfoState _state = AccountInfoState.Initializing;

	private SubscriptionStatus _subStatus = SubscriptionStatus.Unknown;

	private bool _isValidating;

	private string _subscriptionStatusString = string.Empty;

	private string _accountInfoStateString = string.Empty;

	private bool _isUserTokenValid;

	private DateTime? _subscriptionExpirationDate;

	private DateTime? _trialExpirationDate;

	public ICommand ValidateToken { get; }

	public ICommand ChangeUserToken { get; }

	public ICommand ManageSubscription { get; }

	public ICommand VisitSubscriptionManagementPage { get; }

	public ICommand VisitSite { get; }

	public IObservable<bool> TokenValidated { get; }

	public string? UserToken
	{
		get
		{
			return _userToken;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<AccountInfoViewModel, string>(this, ref _userToken, value, "UserToken");
		}
	}

	public bool IsUserTokenValid
	{
		get
		{
			return _isUserTokenValid;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<AccountInfoViewModel, bool>(this, ref _isUserTokenValid, value, "IsUserTokenValid");
		}
	}

	public string SubscriptionStatusString
	{
		get
		{
			return _subscriptionStatusString;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<AccountInfoViewModel, string>(this, ref _subscriptionStatusString, value, "SubscriptionStatusString");
		}
	}

	public string AccountInfoStateString
	{
		get
		{
			return _accountInfoStateString;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<AccountInfoViewModel, string>(this, ref _accountInfoStateString, value, "AccountInfoStateString");
		}
	}

	public SubscriptionStatus SubStatus
	{
		get
		{
			return _subStatus;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<AccountInfoViewModel, SubscriptionStatus>(this, ref _subStatus, value, "SubStatus");
		}
	}

	public AccountInfoState State
	{
		get
		{
			return _state;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<AccountInfoViewModel, AccountInfoState>(this, ref _state, value, "State");
		}
	}

	public DateTime? SubscriptionExpirationDate
	{
		get
		{
			return _subscriptionExpirationDate;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<AccountInfoViewModel, DateTime?>(this, ref _subscriptionExpirationDate, value, "SubscriptionExpirationDate");
		}
	}

	public DateTime? TrialExpirationDate
	{
		get
		{
			return _trialExpirationDate;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<AccountInfoViewModel, DateTime?>(this, ref _trialExpirationDate, value, "TrialExpirationDate");
		}
	}

	public IObservable<(Exception ex, string source)> ExceptionObservable { get; }

	public AccountInfoViewModel(AuthProvider authProvider, ITrialService trialService, Settings settings, DialogController dialogController)
	{
		AccountInfoViewModel accountInfoViewModel = this;
		_authProvider = authProvider ?? throw new ArgumentNullException("authProvider");
		_trialService = trialService ?? throw new ArgumentNullException("trialService");
		_settings = settings ?? throw new ArgumentNullException("settings");
		_dialogController = dialogController ?? throw new ArgumentNullException("dialogController");
		ReactiveCommand<Unit, LicenseCheckResult> val = ReactiveCommand.CreateFromTask<LicenseCheckResult>((Func<CancellationToken, Task<LicenseCheckResult>>)ValidateTokenAndCheckTrialAsync, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ObservableExtensions.Subscribe<bool>(Observable.ObserveOn<bool>(Observable.Where<bool>(((ReactiveCommandBase<Unit, LicenseCheckResult>)(object)val).IsExecuting, (Func<bool, bool>)((bool isExecuting) => isExecuting)), RxSchedulers.MainThreadScheduler), (Action<bool>)delegate
		{
			accountInfoViewModel.State = AccountInfoState.CheckingLicense;
		});
		ObservableExtensions.Subscribe<LicenseCheckResult>(Observable.ObserveOn<LicenseCheckResult>((IObservable<LicenseCheckResult>)val, RxSchedulers.MainThreadScheduler), (Action<LicenseCheckResult>)ApplyLicenseCheckResult);
		ObservableExtensions.Subscribe<SubscriptionStatus>(Observable.ObserveOn<SubscriptionStatus>(WhenAnyMixin.WhenAnyValue<AccountInfoViewModel, SubscriptionStatus>(this, (Expression<Func<AccountInfoViewModel, SubscriptionStatus>>)((AccountInfoViewModel t) => t.SubStatus)), RxSchedulers.MainThreadScheduler), (Action<SubscriptionStatus>)delegate(SubscriptionStatus status)
		{
			accountInfoViewModel.SubscriptionStatusString = status.ToFriendlyString();
		});
		ObservableExtensions.Subscribe<AccountInfoState>(Observable.ObserveOn<AccountInfoState>(WhenAnyMixin.WhenAnyValue<AccountInfoViewModel, AccountInfoState>(this, (Expression<Func<AccountInfoViewModel, AccountInfoState>>)((AccountInfoViewModel t) => t.State)), RxSchedulers.MainThreadScheduler), (Action<AccountInfoState>)delegate(AccountInfoState state)
		{
			if (string.IsNullOrEmpty(accountInfoViewModel._onChainStatusOverride))
			{
				accountInfoViewModel.AccountInfoStateString = state.ToFriendlyString();
			}
		});
		OnChainCredentialService.StatusChanged += delegate(string? msg)
		{
			accountInfoViewModel._onChainStatusOverride = msg;
			Scheduler.Schedule(RxSchedulers.MainThreadScheduler, (Action)delegate
			{
				accountInfoViewModel.AccountInfoStateString = msg ?? accountInfoViewModel.State.ToFriendlyString();
			});
		};
		ReactiveCommand<Unit, ShowDialogIntent> val2 = ReactiveCommand.Create<ShowDialogIntent>((Func<ShowDialogIntent>)(() => ShowDialogIntent.AuthDialog), (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, ShowDialogIntent> val3 = ReactiveCommand.Create<ShowDialogIntent>((Func<ShowDialogIntent>)(() => ShowDialogIntent.SubscribeDialog), (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, ShowDialogIntent> val4 = ReactiveCommand.Create<ShowDialogIntent>((Func<ShowDialogIntent>)(() => ShowDialogIntent.VisitCiphraIntent), (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, ShowDialogIntent> val5 = ReactiveCommand.Create<ShowDialogIntent>((Func<ShowDialogIntent>)(() => ShowDialogIntent.VisitSubscriptionsPageIntent), (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommandMixins.InvokeCommand<ShowDialogIntent>(Observable.Merge<ShowDialogIntent>(Observable.Merge<ShowDialogIntent>(Observable.Merge<ShowDialogIntent>((IObservable<ShowDialogIntent>)val2, (IObservable<ShowDialogIntent>)val3), (IObservable<ShowDialogIntent>)val4), (IObservable<ShowDialogIntent>)val5), dialogController.ShowDialog);
		TokenValidated = Observable.Select<LicenseCheckResult, bool>(Observable.ObserveOn<LicenseCheckResult>(Observable.Where<LicenseCheckResult>((IObservable<LicenseCheckResult>)val, (Func<LicenseCheckResult, bool>)((LicenseCheckResult t) => t.tokenValidationResult != null)), RxSchedulers.MainThreadScheduler), (Func<LicenseCheckResult, bool>)((LicenseCheckResult t) => t.tokenValidationResult.IsValid));
		ChangeUserToken = (ICommand)val2;
		ManageSubscription = (ICommand)val3;
		ValidateToken = (ICommand)val;
		VisitSite = (ICommand)val4;
		VisitSubscriptionManagementPage = (ICommand)val5;
		UserToken = settings.UserToken ?? "Not set";
		IsUserTokenValid = settings.IsUserTokenValid;
		ObservableExtensions.Subscribe<string>(WhenAnyMixin.WhenAnyValue<AccountInfoViewModel, string>(this, (Expression<Func<AccountInfoViewModel, string>>)((AccountInfoViewModel t) => t.UserToken)), (Action<string>)delegate(string token)
		{
			if (token != settings.UserToken && token != "Not set")
			{
				settings.UserToken = token;
			}
		});
		ObservableExtensions.Subscribe<Unit>(Observable.ObserveOn<Unit>(Observable.Select<string, Unit>(Observable.Where<string>(Observable.Delay<string>(Observable.Where<string>(WhenAnyMixin.WhenAnyValue<AccountInfoViewModel, string>(this, (Expression<Func<AccountInfoViewModel, string>>)((AccountInfoViewModel t) => t.UserToken)), (Func<string, bool>)((string token) => !string.IsNullOrWhiteSpace(token))), TimeSpan.FromMilliseconds(2000L)), (Func<string, bool>)((string _) => !accountInfoViewModel._isValidating)), (Func<string, Unit>)((string _) => Unit.Default)), RxSchedulers.MainThreadScheduler), (Action<Unit>)delegate
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				accountInfoViewModel.ValidateToken.Execute(Unit.Default);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error validating token on token change");
			}
		});
		ObservableExtensions.Subscribe<bool>(WhenAnyMixin.WhenAnyValue<AccountInfoViewModel, bool>(this, (Expression<Func<AccountInfoViewModel, bool>>)((AccountInfoViewModel t) => t.IsUserTokenValid)), (Action<bool>)delegate(bool isValid)
		{
			if (isValid != settings.IsUserTokenValid)
			{
				settings.IsUserTokenValid = isValid;
			}
		});
		IObservable<(Exception, string)> observable = Observable.Select<LicenseCheckResult, (Exception, string)>(Observable.Where<LicenseCheckResult>((IObservable<LicenseCheckResult>)val, (Func<LicenseCheckResult, bool>)delegate(LicenseCheckResult t)
		{
			int result;
			if ((object)t != null)
			{
				TokenValidationResult tokenValidationResult = t.tokenValidationResult;
				if ((object)tokenValidationResult != null && !tokenValidationResult.IsValid)
				{
					result = ((tokenValidationResult.Exception != null) ? 1 : 0);
					goto IL_0021;
				}
			}
			result = 0;
			goto IL_0021;
			IL_0021:
			return (byte)result != 0;
		}), (Func<LicenseCheckResult, (Exception, string)>)((LicenseCheckResult t) => (t.tokenValidationResult.Exception, "checkLicense")));
		ExceptionObservable = Observable.Merge<(Exception, string)>(Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, LicenseCheckResult>)(object)val).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "checkLicense"))), observable);
	}

	private async Task<LicenseCheckResult> ValidateTokenAndCheckTrialAsync(CancellationToken ct)
	{
		_isValidating = true;
		try
		{
			string updatedUserToken = null;
			bool markTokenValid = false;
			if (!OnChainCredentialService.ForceOnChainMode && (string.IsNullOrEmpty(_settings.UserToken) || _settings.UserToken == "Not set"))
			{
				string restoredToken = await _authProvider.TryRestoreToken(ct);
				if (!string.IsNullOrEmpty(restoredToken))
				{
					_settings.UserToken = restoredToken;
					updatedUserToken = restoredToken;
				}
				else
				{
					try
					{
						Log.Information("No token found, auto-registering new user token");
						string newToken = await _authProvider.CreateNewToken(ct);
						_settings.UserToken = newToken;
						updatedUserToken = newToken;
						markTokenValid = true;
						_settings.TrialDialogDismissedDate = DateTime.UtcNow;
					}
					catch (Exception ex)
					{
						Exception ex2 = ex;
						Log.Error(ex2, "Failed to auto-register token");
						return new LicenseCheckResult(new TokenValidationResult(IsValid: false, null, ex2), null);
					}
				}
			}
			Task<TokenValidationResult> tokenValidationTask = _authProvider.ValidateToken(_settings.UserToken, ct);
			Task<DateTime?> trialExpirationTask = _trialService.CheckTrialExpiryDateAsync(ct);
			InlineArray2<Task> buffer = default(InlineArray2<Task>);
			buffer[0] = tokenValidationTask;
			buffer[1] = trialExpirationTask;
			await Task.WhenAll(buffer);
			return new LicenseCheckResult(await tokenValidationTask, await trialExpirationTask, updatedUserToken, markTokenValid);
		}
		finally
		{
			_isValidating = false;
		}
	}

	private void ApplyLicenseCheckResult(LicenseCheckResult result)
	{
		if (!string.IsNullOrWhiteSpace(result.updatedUserToken))
		{
			UserToken = result.updatedUserToken;
		}
		if (result.markTokenValid)
		{
			IsUserTokenValid = true;
		}
		HandleLicenseResponse(result.tokenValidationResult, result.trialExpirationDate);
	}

	private void HandleLicenseResponse(TokenValidationResult tokenValidationResult, DateTime? trialExpirationDate)
	{
		try
		{
			if ((object)tokenValidationResult != null && !tokenValidationResult.IsValid && tokenValidationResult.Exception != null)
			{
				Log.Warning(tokenValidationResult.Exception, "Token validation failed due to network error, preserving previous state");
				State = (_settings.IsUserTokenValid ? AccountInfoState.Completed : AccountInfoState.NetworkError);
				return;
			}
			if (string.IsNullOrEmpty(_settings.UserToken) || _settings.UserToken == "Not set")
			{
				State = AccountInfoState.InvalidToken;
				Log.Warning("No user token available after auto-registration attempt");
				return;
			}
			if (!tokenValidationResult.IsValid)
			{
				State = AccountInfoState.TokenRejected;
				IsUserTokenValid = false;
				Log.Warning("Token was rejected by the server");
				_dialogController.ShowDialog.Execute(ShowDialogIntent.AuthDialog);
				return;
			}
			IsUserTokenValid = true;
			if (tokenValidationResult.Response == null)
			{
				Log.Warning("Token is valid but response payload is missing");
				State = AccountInfoState.Completed;
				return;
			}
			UserDto user = tokenValidationResult.Response.User;
			if (user != null && user.SubscriptionStatus <= 2)
			{
				SubscriptionStatus subscriptionStatus = (SubscriptionStatus)tokenValidationResult.Response.User.SubscriptionStatus;
				SubStatus = subscriptionStatus;
			}
			user = tokenValidationResult.Response.User;
			if (user != null && user.SubscriptionOverdue)
			{
				SubStatus = SubscriptionStatus.Expired;
			}
			user = tokenValidationResult.Response.User;
			if (user != null && user.SubscriptionExpiryDay.HasValue)
			{
				SubscriptionExpirationDate = tokenValidationResult.Response.User.SubscriptionExpiryDay.Value;
			}
			else
			{
				SubscriptionExpirationDate = null;
			}
			SubscriptionStatus subStatus = SubStatus;
			if ((uint)(subStatus - 1) <= 1u)
			{
				State = AccountInfoState.Completed;
			}
			else if (trialExpirationDate.HasValue)
			{
				TrialExpirationDate = trialExpirationDate;
				if (trialExpirationDate > DateTime.Now)
				{
					State = AccountInfoState.TrialActive;
				}
				else
				{
					State = AccountInfoState.TrialExpired;
				}
			}
			else
			{
				State = AccountInfoState.TrialExpired;
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error handling license response");
			State = AccountInfoState.Error;
		}
	}
}
