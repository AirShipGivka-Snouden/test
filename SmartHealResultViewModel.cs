using System;
using System.Reactive.Concurrency;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using ReactiveUI;

namespace Ciphra.VPN.Common.ViewModels;

public class SmartHealResultViewModel : ReactiveObject
{
	private readonly Settings _settings;

	private bool _isVisible;

	private string _changesSummary = string.Empty;

	private SmartHealCandidate? _lastCandidate;

	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SmartHealResultViewModel, bool>(this, ref _isVisible, value, "IsVisible");
		}
	}

	public string ChangesSummary
	{
		get
		{
			return _changesSummary;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<SmartHealResultViewModel, string>(this, ref _changesSummary, value, "ChangesSummary");
		}
	}

	public ICommand SaveSettings { get; }

	public ICommand Dismiss { get; }

	public SmartHealResultViewModel(Settings settings)
	{
		_settings = settings ?? throw new ArgumentNullException("settings");
		SaveSettings = (ICommand)ReactiveCommand.Create((Action)DoSaveSettings, (IObservable<bool>)null, (IScheduler)null);
		Dismiss = (ICommand)ReactiveCommand.Create((Action)DoDismiss, (IObservable<bool>)null, (IScheduler)null);
	}

	public void Show(SmartHealCandidate candidate)
	{
		_lastCandidate = candidate;
		ChangesSummary = candidate.ChangesSummary;
		IsVisible = true;
	}

	private void DoSaveSettings()
	{
		SmartHealCandidate lastCandidate = _lastCandidate;
		if ((object)lastCandidate != null)
		{
			_settings.DropQuic = lastCandidate.Settings.DropQuic;
			_settings.DropUdp = lastCandidate.Settings.DropUdp;
			_settings.ChannelProtocol = lastCandidate.Settings.ChannelProtocol;
		}
		IsVisible = false;
	}

	private void DoDismiss()
	{
		IsVisible = false;
	}
}
