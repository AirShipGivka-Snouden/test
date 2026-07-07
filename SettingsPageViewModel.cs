using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Services;
using ReactiveUI;
using VpnHood.Core.Common.Messaging;

namespace Ciphra.VPN.Common.ViewModels;

public class SettingsPageViewModel : ReactiveObject, IExceptionBroadcaster
{
	private readonly Settings _settings;

	private readonly MainPageViewModel _mainPage;

	private bool _autoStart;

	private bool _autoConnect;

	private bool _isAutoStartAvailable;

	private bool _dropQuic;

	private bool _dropUdp;

	private bool _isSmartHealEnabled;

	private string _trafficUsageText = string.Empty;

	private string _resetCountdownText = string.Empty;

	private EnumSource<ChannelProtocol> _selectedChannelProtocol = ChannelProtocols[0];

	public static IReadOnlyList<EnumSource<ChannelProtocol>> ChannelProtocols { get; } = new global::_003C_003Ez__ReadOnlyArray<EnumSource<ChannelProtocol>>(new EnumSource<ChannelProtocol>[3]
	{
		new EnumSource<ChannelProtocol>
		{
			Value = ChannelProtocol.Tcp,
			DisplayName = "TCP"
		},
		new EnumSource<ChannelProtocol>
		{
			Value = ChannelProtocol.Udp,
			DisplayName = "UDP"
		},
		new EnumSource<ChannelProtocol>
		{
			Value = ChannelProtocol.Quic,
			DisplayName = "QUIC"
		}
	});

	public AccountInfoViewModel AccountInfo { get; }

	public ICommand Restore { get; }

	public ICommand GetSettings { get; }

	public ICommand ResetConnectionDefaults { get; }

	public bool AutoStart
	{
		get
		{
			return _autoStart;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SettingsPageViewModel, bool>(this, ref _autoStart, value, "AutoStart");
		}
	}

	public bool IsAutoStartAvailable
	{
		get
		{
			return _isAutoStartAvailable;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SettingsPageViewModel, bool>(this, ref _isAutoStartAvailable, value, "IsAutoStartAvailable");
		}
	}

	public bool AutoConnect
	{
		get
		{
			return _autoConnect;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SettingsPageViewModel, bool>(this, ref _autoConnect, value, "AutoConnect");
		}
	}

	public bool DropQuic
	{
		get
		{
			return _dropQuic;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SettingsPageViewModel, bool>(this, ref _dropQuic, value, "DropQuic");
		}
	}

	public bool DropUdp
	{
		get
		{
			return _dropUdp;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SettingsPageViewModel, bool>(this, ref _dropUdp, value, "DropUdp");
		}
	}

	public bool IsSmartHealEnabled
	{
		get
		{
			return _isSmartHealEnabled;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SettingsPageViewModel, bool>(this, ref _isSmartHealEnabled, value, "IsSmartHealEnabled");
		}
	}

	public string TrafficUsageText
	{
		get
		{
			return _trafficUsageText;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SettingsPageViewModel, string>(this, ref _trafficUsageText, value, "TrafficUsageText");
		}
	}

	public string ResetCountdownText
	{
		get
		{
			return _resetCountdownText;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SettingsPageViewModel, string>(this, ref _resetCountdownText, value, "ResetCountdownText");
		}
	}

	public EnumSource<ChannelProtocol> SelectedChannelProtocol
	{
		get
		{
			return _selectedChannelProtocol;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SettingsPageViewModel, EnumSource<ChannelProtocol>>(this, ref _selectedChannelProtocol, value, "SelectedChannelProtocol");
		}
	}

	public IObservable<(Exception ex, string source)> ExceptionObservable { get; }

	public SettingsPageViewModel(Settings settings, AccountInfoViewModel accountInfo, IStartUpController startUpController, MainPageViewModel mainPage, TrafficAggregator trafficAggregator, SubscriptionManager subscriptionManager)
	{
		if (startUpController == null)
		{
			throw new ArgumentNullException("startUpController");
		}
		if (trafficAggregator == null)
		{
			throw new ArgumentNullException("trafficAggregator");
		}
		_settings = settings ?? throw new ArgumentNullException("settings");
		AccountInfo = accountInfo ?? throw new ArgumentNullException("accountInfo");
		_mainPage = mainPage ?? throw new ArgumentNullException("mainPage");
		Restore = (subscriptionManager ?? throw new ArgumentNullException("subscriptionManager")).Restore;
		ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(trafficAggregator.TrafficUsageText, RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string text)
		{
			TrafficUsageText = text;
		});
		ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(trafficAggregator.ResetCountdownText, RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string text)
		{
			ResetCountdownText = text;
		});
		ReactiveCommand<Unit, Unit> val = ReactiveCommand.Create((Action)LoadSettings, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, StartupState> val2 = ReactiveCommand.CreateFromTask<StartupState>((Func<Task<StartupState>>)startUpController.GetStartupTaskState, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ReactiveCommand<bool, Unit> val3 = ReactiveCommand.CreateFromTask<bool>((Func<bool, Task>)startUpController.ToggleStartUpAsync, (IObservable<bool>)null, RxSchedulers.MainThreadScheduler);
		ObservableExtensions.Subscribe<StartupState>(Observable.ObserveOn<StartupState>((IObservable<StartupState>)val2, RxSchedulers.MainThreadScheduler), (Action<StartupState>)delegate(StartupState autoStart)
		{
			if ((autoStart == StartupState.Unavailable || (uint)(autoStart - 3) <= 1u) ? true : false)
			{
				IsAutoStartAvailable = false;
				AutoStart = false;
			}
			else
			{
				IsAutoStartAvailable = true;
				AutoStart = autoStart == StartupState.Enabled;
			}
		});
		ReactiveCommandMixins.InvokeCommand<Unit, StartupState>((IObservable<Unit>)val, (ReactiveCommandBase<Unit, StartupState>)(object)val2);
		ReactiveCommandMixins.InvokeCommand<bool, Unit>(Observable.Where<bool>(Observable.Skip<bool>(WhenAnyMixin.WhenAnyValue<SettingsPageViewModel, bool>(this, (Expression<Func<SettingsPageViewModel, bool>>)((SettingsPageViewModel t) => t.AutoStart)), 1), (Func<bool, bool>)((bool _) => IsAutoStartAvailable)), (ReactiveCommandBase<bool, Unit>)(object)val3);
		ObservableExtensions.Subscribe<bool>(Observable.Skip<bool>(WhenAnyMixin.WhenAnyValue<SettingsPageViewModel, bool>(this, (Expression<Func<SettingsPageViewModel, bool>>)((SettingsPageViewModel t) => t.AutoConnect)), 1), (Action<bool>)delegate(bool autoConnect)
		{
			if (_settings.AutoConnect != autoConnect)
			{
				_settings.AutoConnect = autoConnect;
			}
		});
		ObservableExtensions.Subscribe<bool>(Observable.Skip<bool>(WhenAnyMixin.WhenAnyValue<SettingsPageViewModel, bool>(this, (Expression<Func<SettingsPageViewModel, bool>>)((SettingsPageViewModel t) => t.DropQuic)), 1), (Action<bool>)delegate(bool v)
		{
			_settings.DropQuic = v;
		});
		ObservableExtensions.Subscribe<bool>(Observable.Skip<bool>(WhenAnyMixin.WhenAnyValue<SettingsPageViewModel, bool>(this, (Expression<Func<SettingsPageViewModel, bool>>)((SettingsPageViewModel t) => t.DropUdp)), 1), (Action<bool>)delegate(bool v)
		{
			_settings.DropUdp = v;
		});
		ObservableExtensions.Subscribe<EnumSource<ChannelProtocol>>(Observable.Skip<EnumSource<ChannelProtocol>>(WhenAnyMixin.WhenAnyValue<SettingsPageViewModel, EnumSource<ChannelProtocol>>(this, (Expression<Func<SettingsPageViewModel, EnumSource<ChannelProtocol>>>)((SettingsPageViewModel t) => t.SelectedChannelProtocol)), 1), (Action<EnumSource<ChannelProtocol>>)delegate(EnumSource<ChannelProtocol> v)
		{
			_settings.ChannelProtocol = v.Value;
		});
		ObservableExtensions.Subscribe<bool>(Observable.Skip<bool>(WhenAnyMixin.WhenAnyValue<SettingsPageViewModel, bool>(this, (Expression<Func<SettingsPageViewModel, bool>>)((SettingsPageViewModel t) => t.IsSmartHealEnabled)), 1), (Action<bool>)delegate(bool v)
		{
			_settings.IsSmartHealEnabled = v;
		});
		ReactiveCommand<Unit, Unit> val4 = ReactiveCommand.Create((Action)delegate
		{
			DropQuic = false;
			DropUdp = false;
			IsSmartHealEnabled = false;
			SelectedChannelProtocol = ChannelProtocols.First((EnumSource<ChannelProtocol> x) => x.Value == ChannelProtocol.Tcp);
			_mainPage.ResetSelectedServer();
		}, (IObservable<bool>)null, (IScheduler)null);
		GetSettings = (ICommand)val;
		ResetConnectionDefaults = (ICommand)val4;
		ExceptionObservable = Observable.RefCount<(Exception, string)>(Observable.Publish<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, Unit>)(object)val).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "getSettings"))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<bool, Unit>)(object)val3).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "setAutoStart")))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, StartupState>)(object)val2).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "getAutoStartState")))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, Unit>)(object)val4).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "resetConnectionDefaults"))))));
	}

	private void LoadSettings()
	{
		AutoConnect = _settings.AutoConnect;
		DropQuic = _settings.DropQuic;
		DropUdp = _settings.DropUdp;
		IsSmartHealEnabled = _settings.IsSmartHealEnabled;
		SelectedChannelProtocol = ChannelProtocols.First((EnumSource<ChannelProtocol> x) => x.Value == _settings.ChannelProtocol);
	}
}
