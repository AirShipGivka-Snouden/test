using System;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services;
using Ciphra.VPN.Common.Services.Interfaces;
using ReactiveUI;
using Serilog;

namespace Ciphra.VPN.Common.ViewModels;

public class OAuthAccountViewModel : ReactiveObject
{
	public enum AccountState
	{
		SignedOut,
		SignedIn
	}

	public enum SubscriptionSource
	{
		None,
		Stripe,
		Redeem
	}

	private readonly OAuthSignInCoordinator _signInCoordinator;

	private readonly OAuthSessionService _sessionService;

	private readonly OAuthVpnTokenSync _vpnTokenSync;

	private readonly INavigationService? _navigation;

	private AccountState _state = AccountState.SignedOut;

	private string? _accountId;

	private string? _email;

	private bool _isEmailVerified;

	private bool _isPremium;

	private bool _isSubscriptionOverdue;

	private DateTime? _subscriptionExpiresAt;

	private SubscriptionSource _source = SubscriptionSource.None;

	private long? _dataCapBytes;

	private string? _vpnToken;

	private string? _referralCode;

	private string? _referralShareUrl;

	private string? _lastErrorMessage;

	private string _displayName = "Sign in";

	public ICommand SignIn { get; }

	public ICommand SignOut { get; }

	public ICommand CancelSignIn { get; }

	public ICommand RefreshAccount { get; }

	public ICommand ManageSubscription { get; }

	public ICommand OpenAccount { get; }

	public AccountState State
	{
		get
		{
			return _state;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<OAuthAccountViewModel, AccountState>(this, ref _state, value, "State");
		}
	}

	public string? AccountId
	{
		get
		{
			return _accountId;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<OAuthAccountViewModel, string>(this, ref _accountId, value, "AccountId");
		}
	}

	public string? Email
	{
		get
		{
			return _email;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<OAuthAccountViewModel, string>(this, ref _email, value, "Email");
		}
	}

	public bool IsEmailVerified
	{
		get
		{
			return _isEmailVerified;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<OAuthAccountViewModel, bool>(this, ref _isEmailVerified, value, "IsEmailVerified");
		}
	}

	public bool IsPremium
	{
		get
		{
			return _isPremium;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<OAuthAccountViewModel, bool>(this, ref _isPremium, value, "IsPremium");
		}
	}

	public bool IsSubscriptionOverdue
	{
		get
		{
			return _isSubscriptionOverdue;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<OAuthAccountViewModel, bool>(this, ref _isSubscriptionOverdue, value, "IsSubscriptionOverdue");
		}
	}

	public DateTime? SubscriptionExpiresAt
	{
		get
		{
			return _subscriptionExpiresAt;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<OAuthAccountViewModel, DateTime?>(this, ref _subscriptionExpiresAt, value, "SubscriptionExpiresAt");
		}
	}

	public SubscriptionSource Source
	{
		get
		{
			return _source;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<OAuthAccountViewModel, SubscriptionSource>(this, ref _source, value, "Source");
		}
	}

	public long? DataCapBytes
	{
		get
		{
			return _dataCapBytes;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<OAuthAccountViewModel, long?>(this, ref _dataCapBytes, value, "DataCapBytes");
		}
	}

	public string? VpnToken
	{
		get
		{
			return _vpnToken;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<OAuthAccountViewModel, string>(this, ref _vpnToken, value, "VpnToken");
		}
	}

	public string? ReferralCode
	{
		get
		{
			return _referralCode;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<OAuthAccountViewModel, string>(this, ref _referralCode, value, "ReferralCode");
			IReactiveObjectExtensions.RaisePropertyChanged<OAuthAccountViewModel>(this, "HasReferral");
		}
	}

	public string? ReferralShareUrl
	{
		get
		{
			return _referralShareUrl;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<OAuthAccountViewModel, string>(this, ref _referralShareUrl, value, "ReferralShareUrl");
			IReactiveObjectExtensions.RaisePropertyChanged<OAuthAccountViewModel>(this, "HasReferral");
		}
	}

	public bool HasReferral => !string.IsNullOrWhiteSpace(_referralShareUrl) || !string.IsNullOrWhiteSpace(_referralCode);

	public string? LastErrorMessage
	{
		get
		{
			return _lastErrorMessage;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<OAuthAccountViewModel, string>(this, ref _lastErrorMessage, value, "LastErrorMessage");
		}
	}

	public string DisplayName
	{
		get
		{
			return _displayName;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<OAuthAccountViewModel, string>(this, ref _displayName, value, "DisplayName");
		}
	}

	public bool IsSignedIn => State == AccountState.SignedIn;

	public OAuthAccountViewModel()
		: this(null, null, null, null, null, null)
	{
	}

	public OAuthAccountViewModel(IOAuthLoginLauncher? loginLauncher, OAuthApiClient? api, IOAuthSecureStorage? storage, Settings? settings, AccountInfoViewModel? accountInfo, INavigationService? navigation, IAppAnalytics? analytics = null)
	{
		_signInCoordinator = new OAuthSignInCoordinator(loginLauncher);
		_sessionService = new OAuthSessionService(api, storage);
		_vpnTokenSync = new OAuthVpnTokenSync(settings, accountInfo);
		_navigation = navigation;
		_signInCoordinator.Analytics = analytics;
		_sessionService.Analytics = analytics;
		SignIn = (ICommand)ReactiveCommand.CreateFromTask((Func<CancellationToken, Task>)SignInAsync, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		SignOut = (ICommand)ReactiveCommand.CreateFromTask((Func<CancellationToken, Task>)SignOutAsync, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		CancelSignIn = (ICommand)ReactiveCommand.Create((Action)CancelSignInImpl, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		RefreshAccount = (ICommand)ReactiveCommand.CreateFromTask((Func<CancellationToken, Task>)RefreshAccountAsync, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ManageSubscription = (ICommand)ReactiveCommand.Create((Action)ManageSubscriptionStub, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		OpenAccount = (ICommand)ReactiveCommand.Create((Action)OpenAccountStub, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(WhenAnyMixin.WhenAnyValue<OAuthAccountViewModel, string, string, AccountState>(this, (Expression<Func<OAuthAccountViewModel, string>>)((OAuthAccountViewModel t) => t.Email), (Expression<Func<OAuthAccountViewModel, AccountState>>)((OAuthAccountViewModel t) => t.State), (Func<string, AccountState, string>)((string email, AccountState state) => FormatDisplayName(email, state))), RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string name)
		{
			DisplayName = name;
		});
		TryRestoreSessionAsync();
	}

	private static string FormatDisplayName(string? email, AccountState state)
	{
		if (state != AccountState.SignedIn)
		{
			return "Sign in";
		}
		if (string.IsNullOrWhiteSpace(email))
		{
			return "Account";
		}
		int num = email.IndexOf('@');
		return (num > 0) ? email.Substring(0, num) : email;
	}

	private async Task SignInAsync(CancellationToken ct)
	{
		if (State == AccountState.SignedIn)
		{
			return;
		}
		LastErrorMessage = null;
		OAuthSignInStartResult result = await _signInCoordinator.StartAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		switch (result.Status)
		{
		case OAuthSignInStartStatus.Started:
			break;
		case OAuthSignInStartStatus.Cancelled:
			Scheduler.Schedule(RxSchedulers.MainThreadScheduler, (Action)delegate
			{
				State = AccountState.SignedOut;
			});
			break;
		case OAuthSignInStartStatus.Unavailable:
			Scheduler.Schedule(RxSchedulers.MainThreadScheduler, (Action)delegate
			{
				LastErrorMessage = "Sign-in is not available on this platform";
				State = AccountState.SignedOut;
			});
			break;
		case OAuthSignInStartStatus.LaunchFailed:
			Scheduler.Schedule(RxSchedulers.MainThreadScheduler, (Action)delegate
			{
				LastErrorMessage = result.Exception?.Message;
				State = AccountState.SignedOut;
			});
			break;
		}
	}

	public void HandleCallback(string callbackUri)
	{
		OAuthCallbackValidationResult result = _signInCoordinator.HandleCallback(callbackUri);
		if (result.Status == OAuthCallbackStatus.Ignored)
		{
			return;
		}
		if (result.Status == OAuthCallbackStatus.Accepted)
		{
			ExchangeAndApplyAsync(result.Code, result.CodeVerifier);
			return;
		}
		Scheduler.Schedule(RxSchedulers.MainThreadScheduler, (Action)delegate
		{
			LastErrorMessage = result.ErrorMessage;
			State = AccountState.SignedOut;
		});
	}

	private async Task ExchangeAndApplyAsync(string code, string verifier)
	{
		ApplySessionResult(await _sessionService.ExchangeAndLoadAccountAsync(code, verifier).ConfigureAwait(continueOnCapturedContext: false));
	}

	private void ApplyMeOnUiThread(OAuthMeResponse me)
	{
		Scheduler.Schedule(RxSchedulers.MainThreadScheduler, (Action)delegate
		{
			AccountId = me.Id;
			Email = me.Email;
			IsEmailVerified = me.EmailVerified;
			VpnToken = me.VpnToken;
			ReferralCode = me.Referral?.Code;
			ReferralShareUrl = me.Referral?.ShareUrl;
			_vpnTokenSync.ApplyUserBoundToken(me.VpnToken);
			if (me.Subscription != null)
			{
				IsPremium = me.Subscription.IsPremium;
				IsSubscriptionOverdue = me.Subscription.Overdue;
				SubscriptionExpiresAt = me.Subscription.ExpiresAt;
				DataCapBytes = me.Subscription.DataCapBytes;
				string source = me.Subscription.Source;
				if (1 == 0)
				{
				}
				SubscriptionSource source2 = ((source == "stripe") ? SubscriptionSource.Stripe : ((source == "redeem") ? SubscriptionSource.Redeem : SubscriptionSource.None));
				if (1 == 0)
				{
				}
				Source = source2;
			}
			else
			{
				IsPremium = false;
				IsSubscriptionOverdue = false;
				SubscriptionExpiresAt = null;
				DataCapBytes = null;
				Source = SubscriptionSource.None;
			}
			LastErrorMessage = null;
			State = AccountState.SignedIn;
		});
	}

	private async Task TryRestoreSessionAsync()
	{
		ApplySessionResult(await _sessionService.RestoreSessionAsync().ConfigureAwait(continueOnCapturedContext: false));
	}

	private void ApplySessionResult(OAuthSessionOperationResult result)
	{
		switch (result.Status)
		{
		case OAuthSessionOperationStatus.Success:
			if (result.Account != null)
			{
				ApplyMeOnUiThread(result.Account);
			}
			break;
		case OAuthSessionOperationStatus.SessionExpired:
			Scheduler.Schedule(RxSchedulers.MainThreadScheduler, (Action)delegate
			{
				_vpnTokenSync.RestoreAnonymousToken();
				ClearAccount();
				LastErrorMessage = result.ErrorMessage;
				State = AccountState.SignedOut;
			});
			break;
		case OAuthSessionOperationStatus.Failed:
			Scheduler.Schedule(RxSchedulers.MainThreadScheduler, (Action)delegate
			{
				LastErrorMessage = result.ErrorMessage;
				State = AccountState.SignedOut;
			});
			break;
		case OAuthSessionOperationStatus.Unavailable:
			if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
			{
				Scheduler.Schedule(RxSchedulers.MainThreadScheduler, (Action)delegate
				{
					LastErrorMessage = result.ErrorMessage;
					State = AccountState.SignedOut;
				});
			}
			break;
		case OAuthSessionOperationStatus.NoStoredSession:
		case OAuthSessionOperationStatus.RefreshFailed:
			break;
		}
	}

	private async Task SignOutAsync(CancellationToken ct)
	{
		Log.Information("OAuthAccountViewModel.SignOut invoked");
		_signInCoordinator.Cancel();
		await _sessionService.SignOutAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		Scheduler.Schedule(RxSchedulers.MainThreadScheduler, (Action)delegate
		{
			_vpnTokenSync.RestoreAnonymousToken();
			ClearAccount();
			State = AccountState.SignedOut;
		});
	}

	private void CancelSignInImpl()
	{
		Log.Information("OAuthAccountViewModel.CancelSignIn invoked");
		_signInCoordinator.Cancel();
		State = AccountState.SignedOut;
	}

	private async Task RefreshAccountAsync(CancellationToken _)
	{
		Log.Information("OAuthAccountViewModel.RefreshAccount invoked");
		if (State == AccountState.SignedIn)
		{
			ApplySessionResult(await _sessionService.RefreshAccountAsync().ConfigureAwait(continueOnCapturedContext: false));
		}
	}

	private void ManageSubscriptionStub()
	{
		Log.Information("OAuthAccountViewModel.ManageSubscription invoked (stub)");
	}

	private void OpenAccountStub()
	{
		Log.Information<AccountState>("OAuthAccountViewModel.OpenAccount invoked (state={State})", State);
		switch (State)
		{
		case AccountState.SignedOut:
			SignIn.Execute(null);
			break;
		case AccountState.SignedIn:
			_navigation?.NavigateTo(NavigationIntent.SettingsPage);
			break;
		}
	}

	private void ClearAccount()
	{
		AccountId = null;
		Email = null;
		IsEmailVerified = false;
		IsPremium = false;
		IsSubscriptionOverdue = false;
		SubscriptionExpiresAt = null;
		Source = SubscriptionSource.None;
		DataCapBytes = null;
		VpnToken = null;
		ReferralCode = null;
		ReferralShareUrl = null;
		LastErrorMessage = null;
	}
}
