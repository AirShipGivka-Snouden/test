using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Services;
using ReactiveUI;
using VpnHood.Core.Common.Messaging;

namespace Ciphra.VPN.Common.ViewModels;

public class MauiSettingsPageViewModel : ReactiveObject, IExceptionBroadcaster
{
	private readonly Settings _settings;

	private readonly MainPageViewModel _mainPage;

	private bool _autoConnect;

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

	public ICommand GetSettings { get; }

	public ICommand ResetConnectionDefaults { get; }

	public AccountInfoViewModel AccountInfo { get; }

	public bool AutoConnect
	{
		get
		{
			return _autoConnect;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MauiSettingsPageViewModel, bool>(this, ref _autoConnect, value, "AutoConnect");
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
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MauiSettingsPageViewModel, bool>(this, ref _dropQuic, value, "DropQuic");
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
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MauiSettingsPageViewModel, bool>(this, ref _dropUdp, value, "DropUdp");
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
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MauiSettingsPageViewModel, bool>(this, ref _isSmartHealEnabled, value, "IsSmartHealEnabled");
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
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MauiSettingsPageViewModel, string>(this, ref _trafficUsageText, value, "TrafficUsageText");
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
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MauiSettingsPageViewModel, string>(this, ref _resetCountdownText, value, "ResetCountdownText");
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
			IReactiveObjectExtensions.RaiseAndSetIfChanged<MauiSettingsPageViewModel, EnumSource<ChannelProtocol>>(this, ref _selectedChannelProtocol, value, "SelectedChannelProtocol");
		}
	}

	public IObservable<(Exception ex, string source)> ExceptionObservable { get; }

	public MauiSettingsPageViewModel(Settings settings, AccountInfoViewModel accountInfo, MainPageViewModel mainPage, TrafficAggregator trafficAggregator)
	{
		_settings = settings ?? throw new ArgumentNullException("settings");
		AccountInfo = accountInfo ?? throw new ArgumentNullException("accountInfo");
		_mainPage = mainPage ?? throw new ArgumentNullException("mainPage");
		if (trafficAggregator == null)
		{
			throw new ArgumentNullException("trafficAggregator");
		}
		ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(trafficAggregator.TrafficUsageText, RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string text)
		{
			TrafficUsageText = text;
		});
		ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(trafficAggregator.ResetCountdownText, RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string text)
		{
			ResetCountdownText = text;
		});
		ReactiveCommand<Unit, Unit> val = ReactiveCommand.Create((Action)LoadSettings, (IObservable<bool>)null, (IScheduler)null);
		ObservableExtensions.Subscribe<bool>(Observable.Skip<bool>(WhenAnyMixin.WhenAnyValue<MauiSettingsPageViewModel, bool>(this, (Expression<Func<MauiSettingsPageViewModel, bool>>)((MauiSettingsPageViewModel t) => t.AutoConnect)), 1), (Action<bool>)delegate(bool autoConnect)
		{
			if (_settings.AutoConnect != autoConnect)
			{
				_settings.AutoConnect = autoConnect;
			}
		});
		ObservableExtensions.Subscribe<bool>(Observable.Skip<bool>(WhenAnyMixin.WhenAnyValue<MauiSettingsPageViewModel, bool>(this, (Expression<Func<MauiSettingsPageViewModel, bool>>)((MauiSettingsPageViewModel t) => t.DropQuic)), 1), (Action<bool>)delegate(bool v)
		{
			_settings.DropQuic = v;
		});
		ObservableExtensions.Subscribe<bool>(Observable.Skip<bool>(WhenAnyMixin.WhenAnyValue<MauiSettingsPageViewModel, bool>(this, (Expression<Func<MauiSettingsPageViewModel, bool>>)((MauiSettingsPageViewModel t) => t.DropUdp)), 1), (Action<bool>)delegate(bool v)
		{
			_settings.DropUdp = v;
		});
		ObservableExtensions.Subscribe<EnumSource<ChannelProtocol>>(Observable.Skip<EnumSource<ChannelProtocol>>(WhenAnyMixin.WhenAnyValue<MauiSettingsPageViewModel, EnumSource<ChannelProtocol>>(this, (Expression<Func<MauiSettingsPageViewModel, EnumSource<ChannelProtocol>>>)((MauiSettingsPageViewModel t) => t.SelectedChannelProtocol)), 1), (Action<EnumSource<ChannelProtocol>>)delegate(EnumSource<ChannelProtocol> v)
		{
			_settings.ChannelProtocol = v.Value;
		});
		ObservableExtensions.Subscribe<bool>(Observable.Skip<bool>(WhenAnyMixin.WhenAnyValue<MauiSettingsPageViewModel, bool>(this, (Expression<Func<MauiSettingsPageViewModel, bool>>)((MauiSettingsPageViewModel t) => t.IsSmartHealEnabled)), 1), (Action<bool>)delegate(bool v)
		{
			_settings.IsSmartHealEnabled = v;
		});
		ReactiveCommand<Unit, Unit> val2 = ReactiveCommand.Create((Action)delegate
		{
			DropQuic = false;
			DropUdp = false;
			IsSmartHealEnabled = false;
			SelectedChannelProtocol = ChannelProtocols.First((EnumSource<ChannelProtocol> x) => x.Value == ChannelProtocol.Tcp);
			_mainPage.ResetSelectedServer();
		}, (IObservable<bool>)null, (IScheduler)null);
		ExceptionObservable = Observable.Merge<(Exception, string)>(Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, Unit>)(object)val).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "getSettings"))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, Unit>)(object)val2).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "resetConnectionDefaults"))));
		GetSettings = (ICommand)val;
		ResetConnectionDefaults = (ICommand)val2;
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
