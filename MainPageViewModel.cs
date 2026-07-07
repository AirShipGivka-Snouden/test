using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services;
using Ciphra.VPN.Common.Services.Interfaces;
using Ciphra.VPN.Common.ViewModels.DataViewModels;
using ReactiveUI;
using Serilog;
using VpnHood.Core.Client.Abstractions;
using VpnHood.Core.Common.Exceptions;
using VpnHood.Core.Common.Messaging;

namespace Ciphra.VPN.Common.ViewModels;

public class MainPageViewModel : ReactiveObject, IExceptionBroadcaster
{
	public enum MainPageState
	{
		Error,
		Disconnected,
		Connecting,
		Connected,
		Unstable
	}

	internal enum ConnectionGateDecision
	{
		Proceed,
		BlockInvalidToken,
		BlockUnknownStatus,
		RequireSubscription
	}

	private const string DisconnectedStatus = "00:00:00";

	private readonly AccountInfoViewModel _accountInfo;

	private readonly IVpnService _vpnService;

	private readonly VpnServerRepository _vpnServerRepository;

	private readonly DialogController _dialogController;

	private readonly INavigationService _navigation;

	private readonly Settings _settings;

	private readonly IAppAnalytics _appAnalytics;

	private readonly Lazy<IExceptionHandler> _exceptionHandler;

	private readonly SemaphoreSlim _reconnectSemaphore = new SemaphoreSlim(1, 1);

	private Task _currentConnectionTask;

	private CancellationTokenSource _connectionCts;

	private Task _currentDisconnectTask;

	private VpnServerViewModel _selectedVpnServer;

	private MainPageState _state = MainPageState.Disconnected;

	private IDisposable _connectionStateTrackerSub;

	private string _externalIp = string.Empty;

	private string _uptime = "00:00:00";

	private string _downloadSpeed = string.Empty;

	private string _uploadSpeed = string.Empty;

	private string _trafficSent = string.Empty;

	private string _trafficReceived = string.Empty;

	private readonly TrafficAggregator _trafficAggregator;

	private bool _userSelectedServer;

	private bool _isSmartHealInProgress;

	private readonly Action _navigateToLocations;

	private long _stateOverrideUntilTicks;

	public IObservable<(Exception ex, string source)> ExceptionObservable { get; }

	public AccountInfoViewModel AccountInfo => _accountInfo;

	public ICommand RestoreLastUsedServer { get; }

	public ICommand RehydrateAfterResume { get; }

	public ReactiveCommand<Unit, Unit> Connect { get; }

	public ReactiveCommand<Unit, Unit> Disconnect { get; }

	public ReactiveCommand<Unit, Unit> Reconnect { get; }

	public string ExternalIp
	{
		get
		{
			return _externalIp;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MainPageViewModel, string>(this, ref _externalIp, value, "ExternalIp");
		}
	}

	public string Uptime
	{
		get
		{
			return _uptime;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MainPageViewModel, string>(this, ref _uptime, value, "Uptime");
		}
	}

	public string DownloadSpeed
	{
		get
		{
			return _downloadSpeed;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MainPageViewModel, string>(this, ref _downloadSpeed, value, "DownloadSpeed");
		}
	}

	public string UploadSpeed
	{
		get
		{
			return _uploadSpeed;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MainPageViewModel, string>(this, ref _uploadSpeed, value, "UploadSpeed");
		}
	}

	public string TrafficSent
	{
		get
		{
			return _trafficSent;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MainPageViewModel, string>(this, ref _trafficSent, value, "TrafficSent");
		}
	}

	public string TrafficReceived
	{
		get
		{
			return _trafficReceived;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MainPageViewModel, string>(this, ref _trafficReceived, value, "TrafficReceived");
		}
	}

	public MainPageState State
	{
		get
		{
			return _state;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MainPageViewModel, MainPageState>(this, ref _state, value, "State");
		}
	}

	public VpnServerViewModel SelectedVpnServer
	{
		get
		{
			return _selectedVpnServer;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MainPageViewModel, VpnServerViewModel>(this, ref _selectedVpnServer, value, "SelectedVpnServer");
		}
	}

	public SmartHealResultViewModel SmartHealResult { get; }

	public bool IsSmartHealInProgress
	{
		get
		{
			return _isSmartHealInProgress;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MainPageViewModel, bool>(this, ref _isSmartHealInProgress, value, "IsSmartHealInProgress");
		}
	}

	private IObservable<ConnectionStateDto?> ConnectionStateTracker => Observable.ObserveOn<ConnectionStateDto>(Observable.Where<ConnectionStateDto>(Observable.Select<long, ConnectionStateDto>(Observable.Interval(TimeSpan.FromSeconds(1L)), (Func<long, ConnectionStateDto>)((long _) => _vpnService.CheckConnectionState())), (Func<ConnectionStateDto, bool>)((ConnectionStateDto t) => t != null)), RxSchedulers.MainThreadScheduler);

	public MainPageViewModel(AccountInfoViewModel accountInfo, IVpnService vpnService, VpnServerRepository vpnServerRepository, ExternalIpService externalIpService, RateNotifier rateNotifier, PromoNotifier promoNotifier, DialogController dialogController, INavigationService navigation, Settings settings, VpnServerViewModel.Factory vpnServerFactory, IAppAnalytics appAnalytics, Lazy<IExceptionHandler> exceptionHandler, SmartHealResultViewModel smartHealResult, TrafficAggregator trafficAggregator)
	{
		MainPageViewModel mainPageViewModel = this;
		if (externalIpService == null)
		{
			throw new ArgumentNullException("externalIpService");
		}
		if (rateNotifier == null)
		{
			throw new ArgumentNullException("rateNotifier");
		}
		if (promoNotifier == null)
		{
			throw new ArgumentNullException("promoNotifier");
		}
		if (vpnServerFactory == null)
		{
			throw new ArgumentNullException("vpnServerFactory");
		}
		_trafficAggregator = trafficAggregator ?? throw new ArgumentNullException("trafficAggregator");
		_accountInfo = accountInfo ?? throw new ArgumentNullException("accountInfo");
		_vpnService = vpnService ?? throw new ArgumentNullException("vpnService");
		_vpnServerRepository = vpnServerRepository ?? throw new ArgumentNullException("vpnServerRepository");
		_dialogController = dialogController ?? throw new ArgumentNullException("dialogController");
		_navigation = navigation ?? throw new ArgumentNullException("navigation");
		_settings = settings ?? throw new ArgumentNullException("settings");
		_appAnalytics = appAnalytics ?? throw new ArgumentNullException("appAnalytics");
		_exceptionHandler = exceptionHandler ?? throw new ArgumentNullException("exceptionHandler");
		SmartHealResult = smartHealResult ?? throw new ArgumentNullException("smartHealResult");
		ReactiveCommand<Unit, Unit> val = ReactiveCommand.Create((Action)delegate
		{
		}, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> val2 = ReactiveCommand.Create((Action)delegate
		{
		}, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> val3 = ReactiveCommand.Create((Action)delegate
		{
		}, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, MainPageState> val4 = ReactiveCommand.CreateFromTask<MainPageState>((Func<CancellationToken, Task<MainPageState>>)ConnectAsync, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, MainPageState> val5 = ReactiveCommand.CreateFromTask<MainPageState>((Func<CancellationToken, Task<MainPageState>>)DisconnectAsync, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> reconnect = ReactiveCommand.CreateFromTask((Func<CancellationToken, Task>)ReconnectAsync, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, VpnServerDto> val6 = ReactiveCommand.CreateFromTask<VpnServerDto>((Func<CancellationToken, Task<VpnServerDto>>)RehydrateAfterResumeAsync, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, string> val7 = ReactiveCommand.CreateFromTask<string>((Func<CancellationToken, Task<string>>)externalIpService.GetExternalIp, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, VpnServerDto> val8 = ReactiveCommand.CreateFromTask<VpnServerDto>((Func<CancellationToken, Task<VpnServerDto>>)RestoreLastUsedServerAsync, (IObservable<bool>)null, (IScheduler)null);
		Action navigateToLocations = delegate
		{
			navigation.NavigateTo(NavigationIntent.LocationsPage);
		};
		_navigateToLocations = navigateToLocations;
		ObservableExtensions.Subscribe<NavigationArgs>(Observable.ObserveOn<NavigationArgs>(navigation.NavigatedObservable, RxSchedulers.MainThreadScheduler), (Action<NavigationArgs>)delegate(NavigationArgs t)
		{
			if (t.Intent == NavigationIntent.MainPage && t.Parameter is VpnServerDto vpnServerDto)
			{
				mainPageViewModel._userSelectedServer = true;
				mainPageViewModel._settings.UserDisconnected = false;
				mainPageViewModel.SelectedVpnServer = vpnServerFactory(vpnServerDto, navigateToLocations);
			}
		});
		ReactiveCommandMixins.InvokeCommand<ShowDialogIntent>(Observable.Select<Unit, ShowDialogIntent>(Observable.Where<Unit>(Observable.Merge<Unit>(Observable.Select<MainPageState, Unit>((IObservable<MainPageState>)val4, (Func<MainPageState, Unit>)((MainPageState _) => Unit.Default)), Observable.Select<Unit, Unit>((IObservable<Unit>)reconnect, (Func<Unit, Unit>)((Unit _) => Unit.Default))), (Func<Unit, bool>)((Unit _) => rateNotifier.TriggerNotification())), (Func<Unit, ShowDialogIntent>)((Unit _) => ShowDialogIntent.RateUsDialog)), dialogController.ShowDialog);
		ReactiveCommandMixins.InvokeCommand<ShowDialogIntent>(Observable.Select<MainPageState, ShowDialogIntent>(Observable.Where<MainPageState>(Observable.Where<MainPageState>(Observable.DistinctUntilChanged<MainPageState>(WhenAnyMixin.WhenAnyValue<MainPageViewModel, MainPageState>(this, (Expression<Func<MainPageViewModel, MainPageState>>)((MainPageViewModel t) => t.State))), (Func<MainPageState, bool>)((MainPageState state) => state == MainPageState.Error)), (Func<MainPageState, bool>)((MainPageState _) => promoNotifier.TriggerNotification())), (Func<MainPageState, ShowDialogIntent>)((MainPageState _) => ShowDialogIntent.PromotionDialog)), dialogController.ShowDialog);
		ObservableExtensions.Subscribe<VpnServerDto>(Observable.ObserveOn<VpnServerDto>(Observable.Merge<VpnServerDto>((IObservable<VpnServerDto>)val8, (IObservable<VpnServerDto>)val6), RxSchedulers.MainThreadScheduler), (Action<VpnServerDto>)delegate(VpnServerDto serverDto)
		{
			MainPageState state = mainPageViewModel.State;
			bool flag = (uint)(state - 2) <= 1u;
			if (!flag && serverDto != null)
			{
				if (serverDto.RequiresUpgrade && mainPageViewModel._settings.AutoConnect)
				{
					Log.Warning<string>("Cached server {ServerId} requires upgrade with autoconnect enabled; clearing cache.", serverDto.Id);
					mainPageViewModel._settings.LastUsedServerId = null;
					mainPageViewModel.SelectedVpnServer = new VpnServerViewModel(navigateToLocations);
				}
				else
				{
					VpnServerViewModel selectedVpnServer = vpnServerFactory(serverDto, navigateToLocations);
					mainPageViewModel.SelectedVpnServer = selectedVpnServer;
				}
			}
		});
		ObservableExtensions.Subscribe<bool>(Observable.ObserveOn<bool>(Observable.Select<VpnServerViewModel, bool>(Observable.ObserveOn<VpnServerViewModel>(Observable.Throttle<VpnServerViewModel>(WhenAnyMixin.WhenAnyValue<MainPageViewModel, VpnServerViewModel>(this, (Expression<Func<MainPageViewModel, VpnServerViewModel>>)((MainPageViewModel t) => t.SelectedVpnServer)), TimeSpan.FromSeconds(2L)), RxSchedulers.MainThreadScheduler), (Func<VpnServerViewModel, bool>)delegate(VpnServerViewModel t)
		{
			if (t.VpnServer == null)
			{
				return false;
			}
			if (t.VpnServer.RequiresUpgrade || string.IsNullOrEmpty(t.VpnServer.AccessKey))
			{
				return false;
			}
			if (accountInfo.SubStatus == AccountInfoViewModel.SubscriptionStatus.Unknown)
			{
				return false;
			}
			if (accountInfo.SubStatus == AccountInfoViewModel.SubscriptionStatus.Inactive && accountInfo.TrialExpirationDate < DateTime.Now)
			{
				dialogController.ShowDialog.Execute(ShowDialogIntent.TrialDialog);
				return false;
			}
			if (accountInfo.SubStatus == AccountInfoViewModel.SubscriptionStatus.Inactive && accountInfo.TrialExpirationDate >= DateTime.Now)
			{
				dialogController.ShowDialog.Execute(ShowDialogIntent.TrialDialog);
			}
			if (!settings.AutoConnect && !mainPageViewModel._userSelectedServer)
			{
				return false;
			}
			return !mainPageViewModel._settings.UserDisconnected;
		}), RxSchedulers.TaskpoolScheduler), (Action<bool>)delegate(bool t)
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			if (t)
			{
				ObservableExtensions.Subscribe<Unit>(((ReactiveCommandBase<Unit, Unit>)(object)reconnect).Execute(Unit.Default));
			}
		});
		ObservableExtensions.Subscribe<bool>(Observable.ObserveOn<bool>(Observable.Select<(AccountInfoViewModel.SubscriptionStatus, DateTime?), bool>(Observable.ObserveOn<(AccountInfoViewModel.SubscriptionStatus, DateTime?)>(WhenAnyMixin.WhenAnyValue<AccountInfoViewModel, AccountInfoViewModel.SubscriptionStatus, DateTime?>(accountInfo, (Expression<Func<AccountInfoViewModel, AccountInfoViewModel.SubscriptionStatus>>)((AccountInfoViewModel t) => t.SubStatus), (Expression<Func<AccountInfoViewModel, DateTime?>>)((AccountInfoViewModel t) => t.TrialExpirationDate)), RxSchedulers.MainThreadScheduler), (Func<(AccountInfoViewModel.SubscriptionStatus, DateTime?), bool>)delegate((AccountInfoViewModel.SubscriptionStatus, DateTime?) tuple)
		{
			if (tuple.Item1 == AccountInfoViewModel.SubscriptionStatus.Unknown)
			{
				return false;
			}
			if (tuple.Item1 == AccountInfoViewModel.SubscriptionStatus.Active)
			{
				if (mainPageViewModel.SelectedVpnServer?.VpnServer != null && mainPageViewModel._settings.AutoConnect && mainPageViewModel.State != MainPageState.Connected && mainPageViewModel.State != MainPageState.Connecting && !mainPageViewModel._settings.UserDisconnected)
				{
					return true;
				}
				return false;
			}
			if (!tuple.Item2.HasValue || tuple.Item2 < DateTime.Now)
			{
				dialogController.ShowDialog.Execute(ShowDialogIntent.TrialDialog);
				return false;
			}
			dialogController.ShowDialog.Execute(ShowDialogIntent.TrialDialog);
			return (mainPageViewModel.SelectedVpnServer?.VpnServer != null && mainPageViewModel._settings.AutoConnect && mainPageViewModel.State != MainPageState.Connected && mainPageViewModel.State != MainPageState.Connecting && !mainPageViewModel._settings.UserDisconnected) ? true : false;
		}), RxSchedulers.TaskpoolScheduler), (Action<bool>)delegate(bool t)
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			if (t)
			{
				ObservableExtensions.Subscribe<Unit>(((ReactiveCommandBase<Unit, Unit>)(object)reconnect).Execute(Unit.Default));
			}
		});
		SelectedVpnServer = new VpnServerViewModel(navigateToLocations);
		ReactiveCommandMixins.InvokeCommand<Unit, MainPageState>(Observable.ObserveOn<Unit>(Observable.Select<bool, Unit>(Observable.Where<bool>(Observable.Select<Unit, bool>(Observable.ObserveOn<Unit>((IObservable<Unit>)val, RxSchedulers.MainThreadScheduler), (Func<Unit, bool>)((Unit _) => mainPageViewModel.ValidateConnectionPreconditions())), (Func<bool, bool>)((bool valid) => valid)), (Func<bool, Unit>)((bool _) => Unit.Default)), RxSchedulers.TaskpoolScheduler), (ReactiveCommandBase<Unit, MainPageState>)(object)val4);
		ReactiveCommandMixins.InvokeCommand<Unit, MainPageState>(Observable.ObserveOn<Unit>(Observable.Do<Unit>((IObservable<Unit>)val2, (Action<Unit>)delegate
		{
			mainPageViewModel._settings.UserDisconnected = true;
		}), RxSchedulers.TaskpoolScheduler), (ReactiveCommandBase<Unit, MainPageState>)(object)val5);
		ReactiveCommandMixins.InvokeCommand<Unit, Unit>(Observable.ObserveOn<Unit>((IObservable<Unit>)val3, RxSchedulers.TaskpoolScheduler), (ReactiveCommandBase<Unit, Unit>)(object)reconnect);
		Connect = val;
		Disconnect = val2;
		Reconnect = val3;
		RehydrateAfterResume = (ICommand)val6;
		RestoreLastUsedServer = (ICommand)val8;
		ExceptionObservable = Observable.RefCount<(Exception, string)>(Observable.Publish<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, MainPageState>)(object)val4).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "connect"))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, MainPageState>)(object)val5).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "disconnect")))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, Unit>)(object)reconnect).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "reconnect")))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, string>)(object)val7).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "getExternalIp")))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, VpnServerDto>)(object)val8).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "restoreLastUsedServer"))))));
		ReactiveCommandMixins.InvokeCommand<Unit, string>(Observable.ObserveOn<Unit>(Observable.Delay<Unit>(Observable.Merge<Unit>(Observable.Merge<Unit>(Observable.Select<MainPageState, Unit>((IObservable<MainPageState>)val4, (Func<MainPageState, Unit>)((MainPageState _) => Unit.Default)), Observable.Select<MainPageState, Unit>((IObservable<MainPageState>)val5, (Func<MainPageState, Unit>)((MainPageState _) => Unit.Default))), (IObservable<Unit>)reconnect), TimeSpan.FromSeconds(5L)), RxSchedulers.TaskpoolScheduler), (ReactiveCommandBase<Unit, string>)(object)val7);
		ReactiveCommandMixins.InvokeCommand<Unit, string>(Observable.Select<MainPageState, Unit>(Observable.DistinctUntilChanged<MainPageState>(Observable.Select<long, MainPageState>(Observable.Timer(TimeSpan.FromSeconds(1L), TimeSpan.FromSeconds(10L)), (Func<long, MainPageState>)((long _) => mainPageViewModel.State))), (Func<MainPageState, Unit>)((MainPageState _) => Unit.Default)), (ReactiveCommandBase<Unit, string>)(object)val7);
		ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>((IObservable<string>)val7, RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string t)
		{
			mainPageViewModel.ExternalIp = t ?? string.Empty;
		});
		ReactiveCommand<MainPageState, Unit> trackConnectionState = ReactiveCommand.CreateFromTask<MainPageState>((Func<MainPageState, CancellationToken, Task>)((MainPageState state, CancellationToken ct) => mainPageViewModel.TrackConnectionState(mainPageViewModel.SelectedVpnServer?.VpnServer, state, ct)), (IObservable<bool>)null, (IScheduler)null);
		ObservableExtensions.Subscribe<Unit>(Observable.ObserveOn<Unit>(Observable.Delay<Unit>(Observable.Merge<Unit>(Observable.Merge<Unit>(Observable.Select<MainPageState, Unit>((IObservable<MainPageState>)val5, (Func<MainPageState, Unit>)((MainPageState _) => Unit.Default)), Observable.Select<MainPageState, Unit>((IObservable<MainPageState>)val4, (Func<MainPageState, Unit>)((MainPageState _) => Unit.Default))), Observable.Select<Unit, Unit>((IObservable<Unit>)reconnect, (Func<Unit, Unit>)((Unit _) => Unit.Default))), TimeSpan.FromSeconds(2L)), RxSchedulers.TaskpoolScheduler), (Action<Unit>)delegate
		{
			try
			{
				ObservableExtensions.Subscribe<Unit>(((ReactiveCommandBase<MainPageState, Unit>)(object)trackConnectionState).Execute(mainPageViewModel.State));
			}
			catch (Exception ex)
			{
				Log.Error<string>(ex, "Failed to track connection state: {Message}", ex.Message);
			}
		});
		ObservableExtensions.Subscribe<MainPageState>(Observable.ObserveOn<MainPageState>(Observable.Merge<MainPageState>((IObservable<MainPageState>)val5, (IObservable<MainPageState>)val4), RxSchedulers.MainThreadScheduler), (Action<MainPageState>)delegate(MainPageState state)
		{
			Interlocked.Exchange(ref mainPageViewModel._stateOverrideUntilTicks, DateTime.UtcNow.Add(TimeSpan.FromSeconds(3L)).Ticks);
			mainPageViewModel.State = state;
		});
		ObservableExtensions.Subscribe<MainPageState>(Observable.ObserveOn<MainPageState>(WhenAnyMixin.WhenAnyValue<MainPageViewModel, MainPageState>(this, (Expression<Func<MainPageViewModel, MainPageState>>)((MainPageViewModel t) => t.State)), RxSchedulers.MainThreadScheduler), (Action<MainPageState>)delegate(MainPageState t)
		{
			if ((uint)t <= 1u)
			{
				mainPageViewModel.Uptime = "00:00:00";
				mainPageViewModel.DownloadSpeed = string.Empty;
				mainPageViewModel.UploadSpeed = string.Empty;
				mainPageViewModel.TrafficSent = string.Empty;
				mainPageViewModel.TrafficReceived = string.Empty;
			}
		});
		ObservableExtensions.Subscribe<SmartHealCandidate>(Observable.ObserveOn<SmartHealCandidate>(_vpnService.SmartHealSucceeded, RxSchedulers.MainThreadScheduler), (Action<SmartHealCandidate>)delegate(SmartHealCandidate candidate)
		{
			mainPageViewModel.SmartHealResult.Show(candidate);
		});
		ObservableExtensions.Subscribe<bool>(Observable.ObserveOn<bool>(_vpnService.IsSmartHealInProgress, RxSchedulers.MainThreadScheduler), (Action<bool>)delegate(bool v)
		{
			mainPageViewModel.IsSmartHealInProgress = v;
		});
		ObservableExtensions.Subscribe<Unit>(Observable.ObserveOn<Unit>(trafficAggregator.TrafficLimitExceeded, RxSchedulers.MainThreadScheduler), (Action<Unit>)delegate
		{
			ObservableExtensions.Subscribe<Unit>(((ReactiveCommandBase<Unit, Unit>)(object)mainPageViewModel.Disconnect).Execute());
			mainPageViewModel._dialogController.ShowDialog.Execute(ShowDialogIntent.TrafficLimitDialog);
		});
	}

	public void ResetSelectedServer()
	{
		_userSelectedServer = false;
		_settings.LastUsedServerId = null;
		SelectedVpnServer = new VpnServerViewModel(_navigateToLocations);
	}

	public async Task<VpnServerDto?> RehydrateAfterResumeAsync(CancellationToken ct)
	{
		VpnServerDto server = await RestoreLastUsedServerAsync(ct);
		ConnectionStateDto connectionState = _vpnService.CheckConnectionState(createIfMissing: true);
		MainPageState state = State;
		if ((uint)(state - 2) <= 2u)
		{
			bool flag = connectionState == null;
			bool flag2 = flag;
			if (!flag2)
			{
				bool flag3;
				switch (connectionState.ClientState)
				{
				case ClientState.None:
				case ClientState.Disconnecting:
				case ClientState.Disposed:
					flag3 = true;
					break;
				default:
					flag3 = false;
					break;
				}
				flag2 = flag3;
			}
			if (flag2)
			{
				Interlocked.Exchange(ref _connectionStateTrackerSub, null)?.Dispose();
				if (_settings.UserDisconnected)
				{
					Log.Information("Skipping auto-reconnect on resume: user-initiated disconnect is in progress.");
					return server;
				}
				Log.Warning("VPN service was disposed while app was in background. Auto-reconnecting...");
				ObservableExtensions.Subscribe<Unit>(((ReactiveCommandBase<Unit, Unit>)(object)Reconnect).Execute(Unit.Default));
				return server;
			}
			if (connectionState.ClientState != ClientState.Disposed && connectionState.ClientState != ClientState.None && connectionState.ClientState != ClientState.Disconnecting)
			{
				RestartConnectionStateTracker();
			}
		}
		else if (connectionState != null && connectionState.ClientState != ClientState.Disposed && connectionState.ClientState != ClientState.None && connectionState.ClientState != ClientState.Disconnecting)
		{
			RestartConnectionStateTracker();
		}
		return server;
	}

	private void RestartConnectionStateTracker()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		Interlocked.Exchange(ref _connectionStateTrackerSub, null)?.Dispose();
		_connectionStateTrackerSub = (IDisposable)new CompositeDisposable(new IDisposable[1] { ObservableExtensions.Subscribe<ConnectionStateDto>(Observable.ObserveOn<ConnectionStateDto>(ConnectionStateTracker, RxSchedulers.MainThreadScheduler), (Action<ConnectionStateDto>)delegate(ConnectionStateDto state)
		{
			if (state != null)
			{
				ApplyConnectionState(state);
			}
		}) });
	}

	private static string FormatSpeed(long bytesPerSec)
	{
		if (1 == 0)
		{
		}
		string result = ((bytesPerSec >= 1000000) ? $"{(double)bytesPerSec / 1000000.0:0.#} MB/s" : ((bytesPerSec < 1000) ? $"{bytesPerSec} B/s" : $"{(double)bytesPerSec / 1000.0:0.#} KB/s"));
		if (1 == 0)
		{
		}
		return result;
	}

	private static string FormatTraffic(long bytes)
	{
		if (1 == 0)
		{
		}
		string result = ((bytes >= 1000000) ? ((bytes < 1000000000) ? $"{(double)bytes / 1000000.0:0.#} MB" : $"{(double)bytes / 1000000000.0:0.##} GB") : ((bytes < 1000) ? $"{bytes} B" : $"{(double)bytes / 1000.0:0.#} KB"));
		if (1 == 0)
		{
		}
		return result;
	}

	private void ApplyConnectionState(ConnectionStateDto state)
	{
		if (state.ClientState != ClientState.Disposed && state.CreatedTime.HasValue)
		{
			TimeSpan timeSpan = DateTime.UtcNow - state.CreatedTime.Value;
			Uptime = $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
		}
		DownloadSpeed = FormatSpeed(state.SpeedReceived);
		UploadSpeed = FormatSpeed(state.SpeedSent);
		TrafficReceived = FormatTraffic(state.TrafficReceived);
		TrafficSent = FormatTraffic(state.TrafficSent);
		ClientState? clientState = state.ClientState;
		if (1 == 0)
		{
		}
		MainPageState mainPageState;
		switch (clientState)
		{
		case ClientState.Unstable:
			mainPageState = MainPageState.Unstable;
			break;
		case ClientState.Connected:
			mainPageState = MainPageState.Connected;
			break;
		case ClientState.Initializing:
		case ClientState.Connecting:
			mainPageState = MainPageState.Connecting;
			break;
		default:
			mainPageState = MainPageState.Disconnected;
			break;
		}
		if (1 == 0)
		{
		}
		MainPageState mainPageState2 = mainPageState;
		bool flag = mainPageState2 == MainPageState.Disconnected;
		bool flag2 = flag;
		if (flag2)
		{
			mainPageState = State;
			bool flag3 = (uint)(mainPageState - 3) <= 1u;
			flag2 = flag3;
		}
		if (flag2 && IsTrafficOverflow(state.Error))
		{
			_dialogController.ShowDialog.Execute(ShowDialogIntent.TrafficLimitDialog);
		}
		long num = Interlocked.Read(in _stateOverrideUntilTicks);
		if (num > 0 && DateTime.UtcNow.Ticks < num && mainPageState2 != State)
		{
			Log.Debug<MainPageState, MainPageState>("Suppressed polling state {PolledState} while override active (current: {CurrentState})", mainPageState2, State);
		}
		else
		{
			State = mainPageState2;
		}
	}

	private async Task ReconnectAsync(CancellationToken ct)
	{
		try
		{
			await _reconnectSemaphore.WaitAsync(ct);
			ConnectionStateDto connectionState = _vpnService.CheckConnectionState();
			if (connectionState != null && connectionState.ClientState != ClientState.Disposed && connectionState.ClientState != ClientState.Disconnecting && connectionState.ClientState != ClientState.None)
			{
				await DisconnectAsync(ct);
				await Task.Delay(TimeSpan.FromSeconds(1L), ct);
			}
			await ConnectAsync(ct);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Failed to reconnect");
		}
		finally
		{
			_reconnectSemaphore.Release();
		}
	}

	internal static ConnectionGateDecision EvaluateConnectionGate(bool isUserTokenValid, AccountInfoViewModel.SubscriptionStatus subStatus, DateTime? trialExpirationDate, DateTime now)
	{
		if (!isUserTokenValid)
		{
			return ConnectionGateDecision.BlockInvalidToken;
		}
		bool flag;
		switch (subStatus)
		{
		case AccountInfoViewModel.SubscriptionStatus.Unknown:
			return ConnectionGateDecision.BlockUnknownStatus;
		case AccountInfoViewModel.SubscriptionStatus.Inactive:
		case AccountInfoViewModel.SubscriptionStatus.Expired:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag && (!trialExpirationDate.HasValue || trialExpirationDate < now))
		{
			return ConnectionGateDecision.RequireSubscription;
		}
		return ConnectionGateDecision.Proceed;
	}

	private bool ValidateConnectionPreconditions()
	{
		switch (EvaluateConnectionGate(_settings.IsUserTokenValid, _accountInfo.SubStatus, _accountInfo.TrialExpirationDate, DateTime.Now))
		{
		case ConnectionGateDecision.BlockInvalidToken:
			Log.Warning("Cannot connect, user token is not valid. Go to Settings to manage your token.");
			return false;
		case ConnectionGateDecision.BlockUnknownStatus:
			Log.Warning("Cannot connect, account subscription status is unknown.");
			return false;
		case ConnectionGateDecision.RequireSubscription:
			Log.Warning("Cannot connect, subscription inactive/expired and trial unavailable or expired. Prompting to subscribe.");
			_dialogController.ShowDialog.Execute(ShowDialogIntent.SubscribeDialog);
			return false;
		default:
		{
			if (_trafficAggregator.IsTrafficLimitExceeded)
			{
				Log.Warning("Cannot connect: client-side traffic limit exceeded, waiting for cycle reset.");
				_dialogController.ShowDialog.Execute(ShowDialogIntent.TrafficLimitDialog);
				return false;
			}
			VpnServerViewModel selectedVpnServer = SelectedVpnServer;
			if (selectedVpnServer != null && selectedVpnServer.VpnServer?.RequiresUpgrade == true)
			{
				Log.Warning<string>("Cached server {ServerId} requires upgrade; clearing cache and navigating to locations.", SelectedVpnServer?.VpnServer?.Id ?? "unknown");
				_settings.LastUsedServerId = null;
				SelectedVpnServer = new VpnServerViewModel(_navigateToLocations);
				_navigation.NavigateTo(NavigationIntent.LocationsPage);
				return false;
			}
			if (SelectedVpnServer?.VpnServer == null)
			{
				_navigation.NavigateTo(NavigationIntent.LocationsPage);
				return false;
			}
			return true;
		}
		}
	}

	private async Task<MainPageState> ConnectAsync(CancellationToken ct)
	{
		_settings.UserDisconnected = false;
		VpnServerDto server = SelectedVpnServer?.VpnServer;
		try
		{
			RestartConnectionStateTracker();
			TrackVpnServerConnection(server);
			await ConnectInternal(server, ct);
			return MainPageState.Connected;
		}
		catch (Exception ex) when (((ex is TaskCanceledException || ex is OperationCanceledException) ? 1 : 0) != 0)
		{
			Log.Warning("Connection attempt was canceled.");
			_connectionStateTrackerSub?.Dispose();
			return MainPageState.Disconnected;
		}
		catch (ArgumentException ex2) when (ex2.ParamName == "AccessKey")
		{
			_connectionStateTrackerSub?.Dispose();
			Log.Warning((Exception)ex2, "Connection aborted: server access key unavailable.");
			_dialogController.ShowDialog.Execute(ShowDialogIntent.TrialDialog);
			return MainPageState.Error;
		}
		catch (Exception ex3)
		{
			_connectionStateTrackerSub?.Dispose();
			TrackConnectionFailed(server, ex3);
			if (IsTrafficOverflow(ex3))
			{
				_dialogController.ShowDialog.Execute(ShowDialogIntent.TrafficLimitDialog);
				return MainPageState.Error;
			}
			_exceptionHandler.Value.HandleException(ex3, "ConnectAsync", fatal: false);
			return MainPageState.Error;
		}
	}

	private async Task ConnectInternal(VpnServerDto? server, CancellationToken ct)
	{
		await CancelConnectionAndWaitForDisconnect();
		if (server == null)
		{
			throw new InvalidOperationException("No VPN server selected for connection.");
		}
		if (server.RequiresUpgrade || string.IsNullOrEmpty(server.AccessKey))
		{
			Log.Warning<string>("Cannot connect to server {ServerId}: requires upgrade or has no access key.", server.Id);
			_dialogController.ShowDialog.Execute(ShowDialogIntent.TrialDialog);
			return;
		}
		_connectionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		_currentConnectionTask = _vpnService.ConnectToServerAsync(server, _connectionCts.Token);
		await _currentConnectionTask;
		_settings.LastUsedServerId = server.Id;
	}

	private async Task<MainPageState> DisconnectAsync(CancellationToken ct)
	{
		try
		{
			await CancelConnectionAndWaitForDisconnect();
			try
			{
				_connectionStateTrackerSub?.Dispose();
				_currentDisconnectTask = _vpnService.DisconnectAsync();
				await _currentDisconnectTask;
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				_exceptionHandler.Value.HandleException(ex2, "DisconnectAsync", fatal: false);
			}
		}
		catch (Exception ex)
		{
			Exception ex3 = ex;
			Log.Error(ex3, "Unexpected error during disconnect");
		}
		return MainPageState.Disconnected;
	}

	private async Task CancelConnectionAndWaitForDisconnect()
	{
		Task currentConnectionTask = _currentConnectionTask;
		if (currentConnectionTask != null && !currentConnectionTask.IsCompleted)
		{
			try
			{
				await _connectionCts.CancelAsync();
				await _currentConnectionTask;
				_connectionCts.Dispose();
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log.Debug(ex2, "Concurrent connection cancelled");
			}
		}
		currentConnectionTask = _currentDisconnectTask;
		if (currentConnectionTask != null && !currentConnectionTask.IsCompleted)
		{
			try
			{
				await _currentDisconnectTask;
			}
			catch (Exception ex3)
			{
				Log.Debug(ex3, "Concurrent disconnection cancelled");
			}
		}
	}

	private async Task<VpnServerDto?> RestoreLastUsedServerAsync(CancellationToken ct)
	{
		Log.Debug("Restoring last used server");
		if (string.IsNullOrEmpty(_settings.UserToken) || _settings.UserToken.Contains("Not set"))
		{
			Log.Warning("User token is not set, cannot restore last used server");
			return null;
		}
		try
		{
			List<VpnServerDto> servers = await _vpnServerRepository.GetServersAsync(ct);
			if (servers == null || servers.Count == 0)
			{
				Log.Warning("No servers available to restore last used server");
				return null;
			}
			if (!string.IsNullOrEmpty(_settings.LastUsedServerId))
			{
				VpnServerDto server = servers.FirstOrDefault((VpnServerDto s) => s.Id == _settings.LastUsedServerId);
				if (server != null)
				{
					Log.Information<string, string>("Restored last used server: {ServerId} - {CountryName}", server.Id, server.CountryIso);
					return server;
				}
				Log.Warning<string>("Last used server {ServerId} not found in the list of available servers", _settings.LastUsedServerId);
			}
			VpnServerDto bestServer = servers.FirstOrDefault((VpnServerDto s) => s.IsBestServer);
			if (bestServer != null)
			{
				Log.Information<string>("Selected default best server: {ServerId}", bestServer.Id);
				return bestServer;
			}
			return null;
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Failed to restore last used server: {Message}", ex.Message);
		}
		return null;
	}

	private void TrackVpnServerConnection(VpnServerDto? server)
	{
		try
		{
			if (server == null)
			{
				Log.Debug("Cannot track VPN server connection, server is null");
				return;
			}
			_appAnalytics.SendEvent("Connection Attempt", ("Server Id", server.Id ?? "Unknown"), ("Country Iso", server.CountryIso ?? "Unknown"), ("City", server.City ?? "Unknown"), ("Requested Channel Protocol", _settings.ChannelProtocol.ToString()), ("Drop Udp", _settings.DropUdp.ToString()), ("Drop Quic", _settings.DropQuic.ToString()));
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Failed to track VPN server connection: {Message}", ex.Message);
		}
	}

	private Task TrackConnectionState(VpnServerDto? server, MainPageState state, CancellationToken ct)
	{
		try
		{
			if (server == null)
			{
				Log.Debug("Cannot track connection status, server is null");
				return Task.CompletedTask;
			}
			List<(string, string)> list = new List<(string, string)>
			{
				("Server Id", server.Id ?? "Unknown"),
				("Country Iso", server.CountryIso ?? "Unknown"),
				("City", server.City ?? "Unknown"),
				("State", state.ToString()),
				("Requested Channel Protocol", _settings.ChannelProtocol.ToString()),
				("Drop Udp", _settings.DropUdp.ToString()),
				("Drop Quic", _settings.DropQuic.ToString())
			};
			if (state == MainPageState.Connected)
			{
				ConnectionStateDto connectionStateDto = _vpnService.CheckConnectionState();
				if (connectionStateDto != null && connectionStateDto.ActiveChannelProtocol.HasValue)
				{
					list.Add(("Active Channel Protocol", connectionStateDto.ActiveChannelProtocol.ToString()));
				}
				if (connectionStateDto != null && connectionStateDto.IsUdpChannelSupported.HasValue)
				{
					list.Add(("Udp Channel Supported", connectionStateDto.IsUdpChannelSupported.ToString()));
				}
			}
			_appAnalytics.SendEvent("Connection State", list.ToArray());
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Failed to track connection status: {Message}", ex.Message);
		}
		return Task.CompletedTask;
	}

	private void TrackConnectionFailed(VpnServerDto? server, Exception ex)
	{
		try
		{
			if (server != null)
			{
				_appAnalytics.SendEvent("Connection Failed", ("Server Id", server.Id ?? "Unknown"), ("Country Iso", server.CountryIso ?? "Unknown"), ("City", server.City ?? "Unknown"), ("Requested Channel Protocol", _settings.ChannelProtocol.ToString()), ("Drop Udp", _settings.DropUdp.ToString()), ("Drop Quic", _settings.DropQuic.ToString()), ("Error Type", ex.GetType().Name), ("Error Message", ex.Message ?? ""));
			}
		}
		catch (Exception ex2)
		{
			Log.Error(ex2, "Failed to track connection failure.");
		}
	}

	private static bool IsTrafficOverflow(Exception? ex)
	{
		while (ex != null)
		{
			if (ex is SessionException ex2 && ex2.SessionResponse.ErrorCode == SessionErrorCode.AccessTrafficOverflow)
			{
				return true;
			}
			ex = ex.InnerException;
		}
		return false;
	}
}
