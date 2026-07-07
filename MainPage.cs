using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Ciphra.VPN.Common.ViewModels;
using Ciphra.VPN.Common.ViewModels.DataViewModels;
using Ciphra.VPN.WinUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using ReactiveUI;
using Serilog;
using WinRT;
using WinRT.Ciphra_VPN_WinUIVtableClasses;

namespace Ciphra.VPN.WinUI.Pages;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(Ciphra_VPN_WinUI_Dialogs_TrafficLimitDialogPageWinRTTypeDetails))]
public sealed class MainPage : Page, IViewFor<MainPageViewModel>, IViewFor, IActivatableView, IComponentConnector
{
	private readonly OAuthAccountViewModel? _accountViewModel;

	private IDisposable _toggleSubscription;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button AccountIconButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Grid ConnectionRoot;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private HyperlinkButton ServerNameButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Grid SpeedPanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Run UptimeRun;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Run ExternalIpRun;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock DownloadSpeedTextBlock;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock UploadSpeedTextBlock;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Run UploadSpeedRun;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Run DownloadSpeedRun;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Run TrafficSentRun;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Run TrafficReceivedRun;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button ConnectButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock AccountInfoStateTextBlock;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock LocationNameTextBlock;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Grid Glow;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Border Circle;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock ConnectionStateTextBlock;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private SolidColorBrush CircleStroke;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private ScaleTransform GlowScale;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Border GlowDisconnected;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Border GlowConnecting;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Border GlowConnected;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Border GlowUnstable;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private ToolTip AccountIconButtonToolTip;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock AccountIconButtonLabel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private bool _contentLoaded;

	object? IViewFor.ViewModel
	{
		get
		{
			return ViewModel;
		}
		set
		{
			ViewModel = (MainPageViewModel)value;
		}
	}

	public MainPageViewModel? ViewModel { get; set; }

	public MainPage()
	{
		ViewModel = App.GetInstance<MainPageViewModel>() ?? throw new InvalidOperationException("MainPageViewModel is not registered in the container.");
		_accountViewModel = App.GetInstance<OAuthAccountViewModel>();
		InitializeComponent();
		ViewForMixins.WhenActivated((IActivatableView)(object)this, (Action<Action<IDisposable>>)delegate(Action<IDisposable> d)
		{
			if (_accountViewModel != null)
			{
				d(ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(WhenAnyMixin.WhenAnyValue<OAuthAccountViewModel, string>(_accountViewModel, (Expression<Func<OAuthAccountViewModel, string>>)((OAuthAccountViewModel t) => t.DisplayName)), RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string name)
				{
					AccountIconButtonLabel.Text = name;
					((ContentControl)AccountIconButtonToolTip).Content = name;
				}));
			}
			d((IDisposable)PropertyBindingMixins.OneWayBind<MainPageViewModel, MainPage, VpnServerViewModel, string>(this, ViewModel, (Expression<Func<MainPageViewModel, VpnServerViewModel>>)((MainPageViewModel vm) => vm.SelectedVpnServer), (Expression<Func<MainPage, string>>)((MainPage v) => v.LocationNameTextBlock.Text), (Func<VpnServerViewModel, string>)((VpnServerViewModel vm) => vm?.CountryName ?? "No server selected")));
			d((IDisposable)PropertyBindingMixins.OneWayBind<MainPageViewModel, MainPage, VpnServerViewModel, ICommand>(this, ViewModel, (Expression<Func<MainPageViewModel, VpnServerViewModel>>)((MainPageViewModel vm) => vm.SelectedVpnServer), (Expression<Func<MainPage, ICommand>>)((MainPage v) => ((ButtonBase)v.ServerNameButton).Command), (Func<VpnServerViewModel, ICommand>)((VpnServerViewModel vm) => vm?.Select)));
			d(ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(Observable.Select<string, string>(WhenAnyMixin.WhenAnyValue<MainPageViewModel, string>(ViewModel, (Expression<Func<MainPageViewModel, string>>)((MainPageViewModel t) => t.ExternalIp)), (Func<string, string>)((string ip) => ip ?? string.Empty)), RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string ip)
			{
				ExternalIpRun.Text = ip;
			}));
			d((IDisposable)PropertyBindingMixins.OneWayBind<MainPageViewModel, MainPage, string, string>(this, ViewModel, (Expression<Func<MainPageViewModel, string>>)((MainPageViewModel vm) => vm.AccountInfo.AccountInfoStateString), (Expression<Func<MainPage, string>>)((MainPage v) => v.AccountInfoStateTextBlock.Text), (object)null, (IBindingTypeConverter)null));
			d(ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(WhenAnyMixin.WhenAnyValue<MainPageViewModel, string>(ViewModel, (Expression<Func<MainPageViewModel, string>>)((MainPageViewModel t) => t.Uptime)), RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string t)
			{
				UptimeRun.Text = (string.IsNullOrEmpty(t) ? "00:00:00" : t);
			}));
			d(ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(WhenAnyMixin.WhenAnyValue<MainPageViewModel, string>(ViewModel, (Expression<Func<MainPageViewModel, string>>)((MainPageViewModel t) => t.DownloadSpeed)), RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string t)
			{
				DownloadSpeedRun.Text = t;
			}));
			d(ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(WhenAnyMixin.WhenAnyValue<MainPageViewModel, string>(ViewModel, (Expression<Func<MainPageViewModel, string>>)((MainPageViewModel t) => t.UploadSpeed)), RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string t)
			{
				UploadSpeedRun.Text = t;
			}));
			d(ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(WhenAnyMixin.WhenAnyValue<MainPageViewModel, string>(ViewModel, (Expression<Func<MainPageViewModel, string>>)((MainPageViewModel t) => t.TrafficReceived)), RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string t)
			{
				TrafficReceivedRun.Text = t;
			}));
			d(ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(WhenAnyMixin.WhenAnyValue<MainPageViewModel, string>(ViewModel, (Expression<Func<MainPageViewModel, string>>)((MainPageViewModel t) => t.TrafficSent)), RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string t)
			{
				TrafficSentRun.Text = t;
			}));
			d(ObservableExtensions.Subscribe<MainPageViewModel.MainPageState>(Observable.ObserveOn<MainPageViewModel.MainPageState>(WhenAnyMixin.WhenAnyValue<MainPageViewModel, MainPageViewModel.MainPageState>(ViewModel, (Expression<Func<MainPageViewModel, MainPageViewModel.MainPageState>>)((MainPageViewModel t) => t.State)), RxSchedulers.MainThreadScheduler), (Action<MainPageViewModel.MainPageState>)delegate(MainPageViewModel.MainPageState state)
			{
				switch (state)
				{
				case MainPageViewModel.MainPageState.Disconnected:
					GoDisconnected();
					break;
				case MainPageViewModel.MainPageState.Connecting:
					GoConnecting();
					break;
				case MainPageViewModel.MainPageState.Connected:
					GoConnected();
					break;
				case MainPageViewModel.MainPageState.Unstable:
					GoToUnstable();
					break;
				case MainPageViewModel.MainPageState.Error:
					GoError();
					break;
				}
			}));
		});
	}

	private Storyboard SB(string key)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		return (Storyboard)((FrameworkElement)ConnectionRoot).Resources[(object)key];
	}

	private void StopAll()
	{
		SB("ToDisconnected").Stop();
		SB("ToConnecting").Stop();
		SB("ToConnected").Stop();
		SB("ToUnstable").Stop();
		((UIElement)SpeedPanel).Visibility = (Visibility)1;
	}

	private void GoDisconnected()
	{
		StopAll();
		SB("ToDisconnected").Begin();
		ConnectionStateTextBlock.Text = "Disconnected";
		((ContentControl)ConnectButton).Content = Loc.Get("MainConnectButtonConnect");
	}

	private void GoConnecting()
	{
		StopAll();
		SB("ToConnecting").Begin();
		ConnectionStateTextBlock.Text = "Connecting...";
		((ContentControl)ConnectButton).Content = Loc.Get("MainConnectButtonCancel");
	}

	private void GoConnected()
	{
		StopAll();
		SB("ToConnected").Begin();
		((UIElement)SpeedPanel).Visibility = (Visibility)0;
		ConnectionStateTextBlock.Text = "Connected";
		((ContentControl)ConnectButton).Content = Loc.Get("MainConnectButtonDisconnect");
	}

	private void GoToUnstable()
	{
		StopAll();
		SB("ToUnstable").Begin();
		((UIElement)SpeedPanel).Visibility = (Visibility)0;
		ConnectionStateTextBlock.Text = "Unstable";
		((ContentControl)ConnectButton).Content = Loc.Get("MainConnectButtonReconnect");
	}

	private void GoError()
	{
		StopAll();
		SB("ToDisconnected").Begin();
		ConnectionStateTextBlock.Text = "Error";
		((ContentControl)ConnectButton).Content = Loc.Get("MainConnectButtonConnect");
	}

	private void AccountIconButton_OnClick(object sender, RoutedEventArgs e)
	{
		try
		{
			OAuthAccountViewModel? accountViewModel = _accountViewModel;
			if (accountViewModel != null && accountViewModel.OpenAccount.CanExecute(null))
			{
				_accountViewModel.OpenAccount.Execute(null);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Exception in AccountIconButton_OnClick");
		}
	}

	private void ConnectButton_OnClick(object sender, RoutedEventArgs e)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (ViewModel == null)
			{
				return;
			}
			MainPageViewModel.MainPageState state = ViewModel.State;
			if ((uint)state <= 1u)
			{
				_toggleSubscription?.Dispose();
				_toggleSubscription = ObservableExtensions.Subscribe<Unit>(((ReactiveCommandBase<Unit, Unit>)(object)ViewModel.Connect).Execute(Unit.Default));
				return;
			}
			state = ViewModel.State;
			if ((uint)(state - 2) <= 1u)
			{
				_toggleSubscription?.Dispose();
				_toggleSubscription = ObservableExtensions.Subscribe<Unit>(((ReactiveCommandBase<Unit, Unit>)(object)ViewModel.Disconnect).Execute(Unit.Default));
			}
			else if (ViewModel.State == MainPageViewModel.MainPageState.Unstable)
			{
				_toggleSubscription?.Dispose();
				_toggleSubscription = ObservableExtensions.Subscribe<Unit>(((ReactiveCommandBase<Unit, Unit>)(object)ViewModel.Reconnect).Execute(Unit.Default));
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Exception in ConnectButton_OnClick");
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("ms-appx:///Pages/MainPage.xaml");
			Application.LoadComponent((object)this, uri, (ComponentResourceLocation)0);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Expected O, but got Unknown
		switch (connectionId)
		{
		case 2:
			AccountIconButton = CastExtensions.As<Button>(target);
			((ButtonBase)AccountIconButton).Click += new RoutedEventHandler(AccountIconButton_OnClick);
			break;
		case 3:
			ConnectionRoot = CastExtensions.As<Grid>(target);
			break;
		case 4:
			ServerNameButton = CastExtensions.As<HyperlinkButton>(target);
			break;
		case 5:
			SpeedPanel = CastExtensions.As<Grid>(target);
			break;
		case 6:
			UptimeRun = CastExtensions.As<Run>(target);
			break;
		case 7:
			ExternalIpRun = CastExtensions.As<Run>(target);
			break;
		case 8:
			DownloadSpeedTextBlock = CastExtensions.As<TextBlock>(target);
			break;
		case 9:
			UploadSpeedTextBlock = CastExtensions.As<TextBlock>(target);
			break;
		case 10:
			UploadSpeedRun = CastExtensions.As<Run>(target);
			break;
		case 11:
			DownloadSpeedRun = CastExtensions.As<Run>(target);
			break;
		case 12:
			TrafficSentRun = CastExtensions.As<Run>(target);
			break;
		case 13:
			TrafficReceivedRun = CastExtensions.As<Run>(target);
			break;
		case 14:
			ConnectButton = CastExtensions.As<Button>(target);
			((ButtonBase)ConnectButton).Click += new RoutedEventHandler(ConnectButton_OnClick);
			break;
		case 15:
			AccountInfoStateTextBlock = CastExtensions.As<TextBlock>(target);
			break;
		case 16:
			LocationNameTextBlock = CastExtensions.As<TextBlock>(target);
			break;
		case 17:
			Glow = CastExtensions.As<Grid>(target);
			break;
		case 18:
			Circle = CastExtensions.As<Border>(target);
			break;
		case 19:
			ConnectionStateTextBlock = CastExtensions.As<TextBlock>(target);
			break;
		case 20:
			CircleStroke = CastExtensions.As<SolidColorBrush>(target);
			break;
		case 21:
			GlowScale = CastExtensions.As<ScaleTransform>(target);
			break;
		case 22:
			GlowDisconnected = CastExtensions.As<Border>(target);
			break;
		case 23:
			GlowConnecting = CastExtensions.As<Border>(target);
			break;
		case 24:
			GlowConnected = CastExtensions.As<Border>(target);
			break;
		case 25:
			GlowUnstable = CastExtensions.As<Border>(target);
			break;
		case 26:
			AccountIconButtonToolTip = CastExtensions.As<ToolTip>(target);
			break;
		case 27:
			AccountIconButtonLabel = CastExtensions.As<TextBlock>(target);
			break;
		}
		_contentLoaded = true;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public IComponentConnector GetBindingConnector(int connectionId, object target)
	{
		return null;
	}
}
