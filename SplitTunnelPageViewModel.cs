using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services;
using Ciphra.VPN.Common.Services.Interfaces;
using Ciphra.VPN.Common.Utils;
using ReactiveUI;
using VpnHood.Core.Client.Abstractions;

namespace Ciphra.VPN.Common.ViewModels;

public class SplitTunnelPageViewModel : ReactiveObject, IExceptionBroadcaster
{
	private static readonly Regex AndroidPackageRegex = new Regex("^[A-Za-z][A-Za-z0-9_]*(\\.[A-Za-z][A-Za-z0-9_]*)+$", RegexOptions.CultureInvariant);

	private readonly Settings _settings;

	private readonly IVpnService? _vpnService;

	private readonly SplitTunnelCapabilities _capabilities;

	private bool _suppressApply;

	private bool _useSplitLocalNetwork;

	private EnumSource<RoutingMode> _selectedRoutingMode = RoutingModes[0];

	private string _ipRangesText = string.Empty;

	private string _domainsText = string.Empty;

	private string _appsText = string.Empty;

	private string _blockIpRangesText = string.Empty;

	private string _blockDomainsText = string.Empty;

	private string _statusText = "Off";

	private string _applyMessageText = string.Empty;

	private static readonly Regex CommentStripperRegex = new Regex("#.*|;.*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public static IReadOnlyList<EnumSource<RoutingMode>> RoutingModes { get; } = new global::_003C_003Ez__ReadOnlyArray<EnumSource<RoutingMode>>(new EnumSource<RoutingMode>[3]
	{
		new EnumSource<RoutingMode>
		{
			Value = RoutingMode.TunnelAll,
			DisplayName = "Tunnel everything"
		},
		new EnumSource<RoutingMode>
		{
			Value = RoutingMode.TunnelOnly,
			DisplayName = "Tunnel only listed"
		},
		new EnumSource<RoutingMode>
		{
			Value = RoutingMode.BypassThese,
			DisplayName = "Bypass listed"
		}
	});

	public ICommand GetSettings { get; }

	public ICommand ApplyChanges { get; }

	public ICommand ResetToDefaults { get; }

	public IObservable<(Exception ex, string source)> ExceptionObservable { get; }

	public bool UseSplitLocalNetwork
	{
		get
		{
			return _useSplitLocalNetwork;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SplitTunnelPageViewModel, bool>(this, ref _useSplitLocalNetwork, value, "UseSplitLocalNetwork");
		}
	}

	public EnumSource<RoutingMode> SelectedRoutingMode
	{
		get
		{
			return _selectedRoutingMode;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SplitTunnelPageViewModel, EnumSource<RoutingMode>>(this, ref _selectedRoutingMode, value, "SelectedRoutingMode");
		}
	}

	public string IpRangesText
	{
		get
		{
			return _ipRangesText;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SplitTunnelPageViewModel, string>(this, ref _ipRangesText, value, "IpRangesText");
		}
	}

	public string DomainsText
	{
		get
		{
			return _domainsText;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SplitTunnelPageViewModel, string>(this, ref _domainsText, value, "DomainsText");
		}
	}

	public string AppsText
	{
		get
		{
			return _appsText;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SplitTunnelPageViewModel, string>(this, ref _appsText, value, "AppsText");
		}
	}

	public string BlockIpRangesText
	{
		get
		{
			return _blockIpRangesText;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SplitTunnelPageViewModel, string>(this, ref _blockIpRangesText, value, "BlockIpRangesText");
		}
	}

	public string BlockDomainsText
	{
		get
		{
			return _blockDomainsText;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SplitTunnelPageViewModel, string>(this, ref _blockDomainsText, value, "BlockDomainsText");
		}
	}

	public string StatusText
	{
		get
		{
			return _statusText;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SplitTunnelPageViewModel, string>(this, ref _statusText, value, "StatusText");
		}
	}

	public string ApplyMessageText
	{
		get
		{
			return _applyMessageText;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SplitTunnelPageViewModel, string>(this, ref _applyMessageText, value, "ApplyMessageText");
		}
	}

	public SplitTunnelPageViewModel(Settings settings, IVpnService? vpnService = null, IVpnServiceManagerFactory? vpnServiceManagerFactory = null)
	{
		_settings = settings ?? throw new ArgumentNullException("settings");
		_vpnService = vpnService;
		_capabilities = vpnServiceManagerFactory?.SplitTunnelCapabilities ?? SplitTunnelCapabilities.Default;
		ReactiveCommand<Unit, Unit> val = ReactiveCommand.Create((Action)LoadSettings, (IObservable<bool>)null, (IScheduler)null);
		ObservableExtensions.Subscribe<Unit>(Observable.Skip<Unit>(WhenAnyMixin.WhenAnyValue<SplitTunnelPageViewModel, Unit, bool, EnumSource<RoutingMode>, string, string, string, string, string>(this, (Expression<Func<SplitTunnelPageViewModel, bool>>)((SplitTunnelPageViewModel t) => t.UseSplitLocalNetwork), (Expression<Func<SplitTunnelPageViewModel, EnumSource<RoutingMode>>>)((SplitTunnelPageViewModel t) => t.SelectedRoutingMode), (Expression<Func<SplitTunnelPageViewModel, string>>)((SplitTunnelPageViewModel t) => t.IpRangesText), (Expression<Func<SplitTunnelPageViewModel, string>>)((SplitTunnelPageViewModel t) => t.DomainsText), (Expression<Func<SplitTunnelPageViewModel, string>>)((SplitTunnelPageViewModel t) => t.AppsText), (Expression<Func<SplitTunnelPageViewModel, string>>)((SplitTunnelPageViewModel t) => t.BlockIpRangesText), (Expression<Func<SplitTunnelPageViewModel, string>>)((SplitTunnelPageViewModel t) => t.BlockDomainsText), (Func<bool, EnumSource<RoutingMode>, string, string, string, string, string, Unit>)((bool _, EnumSource<RoutingMode> _, string _, string _, string _, string _, string _) => Unit.Default)), 1), (Action<Unit>)delegate
		{
			if (!_suppressApply)
			{
				if (TryValidate(out string _))
				{
					ApplyToSettings();
				}
				ApplyMessageText = string.Empty;
				UpdateStatusText();
			}
		});
		ReactiveCommand<Unit, Unit> val2 = ReactiveCommand.Create((Action)delegate
		{
			ApplyPendingChanges(showSavedMessage: true);
		}, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> val3 = ReactiveCommand.Create((Action)ResetAll, (IObservable<bool>)null, (IScheduler)null);
		GetSettings = (ICommand)val;
		ApplyChanges = (ICommand)val2;
		ResetToDefaults = (ICommand)val3;
		ExceptionObservable = Observable.RefCount<(Exception, string)>(Observable.Publish<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Merge<(Exception, string)>(Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, Unit>)(object)val).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "getSettings"))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, Unit>)(object)val2).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "applyChanges")))), Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, Unit>)(object)val3).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "resetToDefaults"))))));
	}

	private void LoadSettings()
	{
		_suppressApply = true;
		try
		{
			SplitTunnelSettings st = _settings.SplitTunnel;
			UseSplitLocalNetwork = st.UseSplitLocalNetwork;
			SelectedRoutingMode = RoutingModes.First((EnumSource<RoutingMode> m) => m.Value == st.SplitTunnelRoutingMode);
			BlockIpRangesText = st.SplitIpAppBlocks;
			BlockDomainsText = st.SplitDomainBlocks;
			switch (st.SplitTunnelRoutingMode)
			{
			case RoutingMode.TunnelOnly:
				IpRangesText = st.SplitIpDeviceIncludes;
				DomainsText = st.SplitDomainIncludes;
				AppsText = string.Join(Environment.NewLine, st.SplitApps);
				break;
			case RoutingMode.BypassThese:
				IpRangesText = st.SplitIpDeviceExcludes;
				DomainsText = st.SplitDomainExcludes;
				AppsText = string.Join(Environment.NewLine, st.SplitApps);
				break;
			default:
				IpRangesText = string.Empty;
				DomainsText = string.Empty;
				AppsText = string.Empty;
				break;
			}
		}
		finally
		{
			_suppressApply = false;
		}
		ApplyMessageText = string.Empty;
		UpdateStatusText();
	}

	private void ResetAll()
	{
		_suppressApply = true;
		try
		{
			UseSplitLocalNetwork = false;
			SelectedRoutingMode = RoutingModes.First((EnumSource<RoutingMode> m) => m.Value == RoutingMode.TunnelAll);
			IpRangesText = string.Empty;
			DomainsText = string.Empty;
			AppsText = string.Empty;
			BlockIpRangesText = string.Empty;
			BlockDomainsText = string.Empty;
		}
		finally
		{
			_suppressApply = false;
		}
		ApplyToSettings();
		ApplyMessageText = string.Empty;
		UpdateStatusText();
	}

	public void ApplyPendingChanges(bool showSavedMessage = false)
	{
		if (!TryValidate(out string error))
		{
			ApplyMessageText = error ?? "Fix invalid split-tunnel rules.";
			UpdateStatusText();
		}
		else
		{
			ApplyToSettings();
			UpdateStatusText();
			ApplyMessageText = ((!showSavedMessage) ? string.Empty : (IsVpnActive() ? "Saved. Reconnect VPN to use these changes." : "Saved."));
		}
	}

	private void UpdateStatusText()
	{
		if (!TryValidate(out string _))
		{
			StatusText = "Invalid";
			return;
		}
		bool flag = HasContent(IpRangesText) || HasContent(DomainsText) || HasContent(AppsText);
		bool flag2 = HasContent(BlockIpRangesText) || HasContent(BlockDomainsText);
		RoutingMode value = SelectedRoutingMode.Value;
		if (value == RoutingMode.TunnelOnly && !flag && !flag2)
		{
			StatusText = "Needs rules";
			return;
		}
		bool flag3 = UseSplitLocalNetwork || flag2 || (value == RoutingMode.TunnelOnly && flag) || (value == RoutingMode.BypassThese && flag);
		StatusText = (flag3 ? "On" : "Off");
	}

	private bool TryValidate(out string? error)
	{
		bool flag = SelectedRoutingMode.Value != RoutingMode.TunnelAll;
		error = (flag ? ValidateIpRules("IP ranges", IpRangesText) : null) ?? ValidateIpRules("Blocked IP ranges", BlockIpRangesText) ?? ValidateDomainCapability(flag) ?? (flag ? ValidateDomainRules("Domains", DomainsText) : null) ?? ValidateDomainRules("Blocked domains", BlockDomainsText) ?? (flag ? ValidateAppRules(AppsText) : null);
		return error == null;
	}

	private string? ValidateDomainCapability(bool validateCriteria)
	{
		if (_capabilities.IsTcpProxySupported)
		{
			return null;
		}
		return ((validateCriteria && HasText(DomainsText)) || HasText(BlockDomainsText)) ? "Domain split tunneling is not supported on this platform." : null;
	}

	private static string? ValidateIpRules(string label, string text)
	{
		if (!HasText(text))
		{
			return null;
		}
		try
		{
			IpRangeTextFileParser.Parse(text);
			return null;
		}
		catch (FormatException ex)
		{
			return label + ": " + ex.Message;
		}
	}

	private static string? ValidateDomainRules(string label, string text)
	{
		if (!DomainTextFileParser.TryValidate(text, out string error))
		{
			return label + ": " + error;
		}
		return null;
	}

	private string? ValidateAppRules(string text)
	{
		if (!_capabilities.IsIncludeAppsSupported && !_capabilities.IsExcludeAppsSupported)
		{
			return null;
		}
		string[] array = SplitOnLines(text);
		foreach (string text2 in array)
		{
			if (!AndroidPackageRegex.IsMatch(text2))
			{
				return "Apps: invalid Android package '" + text2 + "'.";
			}
		}
		return null;
	}

	private bool IsVpnActive()
	{
		try
		{
			ClientState? clientState = _vpnService?.CheckConnectionState()?.ClientState;
			return clientState.HasValue && clientState.Value.CanDisconnect();
		}
		catch (ObjectDisposedException)
		{
			return false;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	private void ApplyToSettings()
	{
		_settings.SplitTunnel.RunBatched(ApplyToSettingsCore);
	}

	private void ApplyToSettingsCore(SplitTunnelSettings st)
	{
		RoutingMode value = SelectedRoutingMode.Value;
		bool flag = HasContent(BlockIpRangesText);
		bool flag2 = HasContent(BlockDomainsText);
		string[] array = SplitOnLines(AppsText);
		st.UseSplitLocalNetwork = UseSplitLocalNetwork;
		st.SplitTunnelRoutingMode = value;
		st.SplitIpAppBlocks = BlockIpRangesText;
		st.SplitDomainBlocks = BlockDomainsText;
		switch (value)
		{
		case RoutingMode.TunnelOnly:
		{
			bool flag5 = HasContent(IpRangesText);
			bool flag6 = HasContent(DomainsText);
			st.UseSplitIpViaDevice = flag5;
			st.UseSplitIpViaApp = flag5 || flag;
			st.UseSplitDomain = flag6 || flag2;
			st.SplitIpDeviceIncludes = IpRangesText;
			st.SplitIpDeviceExcludes = string.Empty;
			st.SplitIpAppIncludes = IpRangesText;
			st.SplitIpAppExcludes = string.Empty;
			st.SplitDomainIncludes = DomainsText;
			st.SplitDomainExcludes = string.Empty;
			st.SplitAppMode = ((array.Length != 0) ? SplitAppMode.Include : SplitAppMode.All);
			st.SplitApps = array;
			break;
		}
		case RoutingMode.BypassThese:
		{
			bool flag3 = HasContent(IpRangesText);
			bool flag4 = HasContent(DomainsText);
			st.UseSplitIpViaDevice = flag3;
			st.UseSplitIpViaApp = flag3 || flag;
			st.UseSplitDomain = flag4 || flag2;
			st.SplitIpDeviceIncludes = string.Empty;
			st.SplitIpDeviceExcludes = IpRangesText;
			st.SplitIpAppIncludes = string.Empty;
			st.SplitIpAppExcludes = IpRangesText;
			st.SplitDomainIncludes = string.Empty;
			st.SplitDomainExcludes = DomainsText;
			st.SplitAppMode = ((array.Length != 0) ? SplitAppMode.Exclude : SplitAppMode.All);
			st.SplitApps = array;
			break;
		}
		default:
			st.UseSplitIpViaDevice = false;
			st.UseSplitIpViaApp = flag;
			st.UseSplitDomain = flag2;
			st.SplitIpDeviceIncludes = string.Empty;
			st.SplitIpDeviceExcludes = string.Empty;
			st.SplitIpAppIncludes = string.Empty;
			st.SplitIpAppExcludes = string.Empty;
			st.SplitDomainIncludes = string.Empty;
			st.SplitDomainExcludes = string.Empty;
			st.SplitAppMode = SplitAppMode.All;
			st.SplitApps = Array.Empty<string>();
			break;
		}
	}

	private static string[] SplitOnLines(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return Array.Empty<string>();
		}
		return (from x in text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
			select x.Trim() into x
			where x.Length > 0
			select x).ToArray();
	}

	private static bool HasText(string? text)
	{
		return !string.IsNullOrWhiteSpace(text);
	}

	private static bool HasContent(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = CommentStripperRegex.Replace(text, string.Empty);
		return text2.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Any((string line) => !string.IsNullOrWhiteSpace(line));
	}
}
