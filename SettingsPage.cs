using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.ViewModels;
using Ciphra.VPN.WinUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using ReactiveUI;
using Serilog;
using VpnHood.Core.Common.Messaging;
using WinRT;
using WinRT.Ciphra_VPN_WinUIVtableClasses;
using Windows.ApplicationModel.DataTransfer;

namespace Ciphra.VPN.WinUI.Pages;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(Ciphra_VPN_WinUI_Dialogs_TrafficLimitDialogPageWinRTTypeDetails))]
public sealed class SettingsPage : Page, IViewFor<SettingsPageViewModel>, IViewFor, IActivatableView, IComponentConnector
{
	private readonly OAuthAccountViewModel? _accountViewModel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Border OAuthAccountCard;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Border ReferralCard;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button SplitTunnelButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private ToggleSwitch AutoStartToggleSwitch;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private ToggleSwitch AutoConnectToggleSwitch;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button ResetConnectionDefaultsButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private ToggleSwitch DropQuicToggleSwitch;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private ToggleSwitch DropUdpToggleSwitch;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private ComboBox ChannelProtocolComboBox;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private ToggleSwitch SmartHealToggleSwitch;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button ChangeUserTokenButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button BuySubscriptionButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button ManageSubscriptionsButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button VisitWebsiteButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock ExpirationDateTextBlock;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock RenewalMessageTextBlock;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock TrafficUsageTextBlock;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock ResetCountdownTextBlock;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock SubscriptionStatusTextBlock;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBox UserIdTextBox;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button CopyToClipboardButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBox ReferralShareUrlTextBox;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button ReferralCopyLinkButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button OAuthSignInButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button OAuthSignOutButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock OAuthAccountDisplayNameTextBlock;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock OAuthAccountStateTextBlock;

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
			ViewModel = (SettingsPageViewModel)value;
		}
	}

	public SettingsPageViewModel? ViewModel { get; set; }

	public SettingsPage()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Expected O, but got Unknown
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Expected O, but got Unknown
		ViewModel = App.GetInstance<SettingsPageViewModel>() ?? throw new InvalidOperationException("SettingsPageViewModel is not registered in the container.");
		_accountViewModel = App.GetInstance<OAuthAccountViewModel>();
		InitializeComponent();
		((ItemsControl)ChannelProtocolComboBox).ItemsSource = SettingsPageViewModel.ChannelProtocols;
		ViewForMixins.WhenActivated((IActivatableView)(object)this, (Action<Action<IDisposable>>)delegate(Action<IDisposable> d)
		{
			if (_accountViewModel != null)
			{
				d(ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(WhenAnyMixin.WhenAnyValue<OAuthAccountViewModel, string>(_accountViewModel, (Expression<Func<OAuthAccountViewModel, string>>)((OAuthAccountViewModel t) => t.DisplayName)), RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string name)
				{
					OAuthAccountDisplayNameTextBlock.Text = name;
				}));
				d(ObservableExtensions.Subscribe<(OAuthAccountViewModel.AccountState, string)>(Observable.ObserveOn<(OAuthAccountViewModel.AccountState, string)>(WhenAnyMixin.WhenAnyValue<OAuthAccountViewModel, OAuthAccountViewModel.AccountState, string>(_accountViewModel, (Expression<Func<OAuthAccountViewModel, OAuthAccountViewModel.AccountState>>)((OAuthAccountViewModel t) => t.State), (Expression<Func<OAuthAccountViewModel, string>>)((OAuthAccountViewModel t) => t.Email)), RxSchedulers.MainThreadScheduler), (Action<(OAuthAccountViewModel.AccountState, string)>)delegate((OAuthAccountViewModel.AccountState, string) tuple)
				{
					var (accountState, text) = tuple;
					OAuthAccountStateTextBlock.Text = ((accountState != OAuthAccountViewModel.AccountState.SignedIn) ? Loc.Get("AccountNotSignedIn") : (string.IsNullOrWhiteSpace(text) ? Loc.Get("AccountSignedIn") : text));
					bool flag = accountState == OAuthAccountViewModel.AccountState.SignedIn;
					((UIElement)OAuthSignInButton).Visibility = (Visibility)(flag ? 1 : 0);
					((UIElement)OAuthSignOutButton).Visibility = (Visibility)(!flag);
				}));
				d(ObservableExtensions.Subscribe<(string, bool)>(Observable.ObserveOn<(string, bool)>(WhenAnyMixin.WhenAnyValue<OAuthAccountViewModel, string, bool>(_accountViewModel, (Expression<Func<OAuthAccountViewModel, string>>)((OAuthAccountViewModel t) => t.ReferralShareUrl), (Expression<Func<OAuthAccountViewModel, bool>>)((OAuthAccountViewModel t) => t.HasReferral)), RxSchedulers.MainThreadScheduler), (Action<(string, bool)>)delegate((string, bool) tuple)
				{
					var (text, flag) = tuple;
					((UIElement)ReferralCard).Visibility = (Visibility)(!flag);
					ReferralShareUrlTextBox.Text = text ?? string.Empty;
				}));
			}
			d((IDisposable)PropertyBindingMixins.OneWayBind<SettingsPageViewModel, SettingsPage, string, string>(this, ViewModel, (Expression<Func<SettingsPageViewModel, string>>)((SettingsPageViewModel vm) => vm.AccountInfo.UserToken), (Expression<Func<SettingsPage, string>>)((SettingsPage c) => c.UserIdTextBox.Text), (object)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.Bind<SettingsPageViewModel, SettingsPage, bool, bool>(this, ViewModel, (Expression<Func<SettingsPageViewModel, bool>>)((SettingsPageViewModel vm) => vm.AutoStart), (Expression<Func<SettingsPage, bool>>)((SettingsPage c) => c.AutoStartToggleSwitch.IsOn), (object)null, (IBindingTypeConverter)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.OneWayBind<SettingsPageViewModel, SettingsPage, bool, bool>(this, ViewModel, (Expression<Func<SettingsPageViewModel, bool>>)((SettingsPageViewModel vm) => vm.IsAutoStartAvailable), (Expression<Func<SettingsPage, bool>>)((SettingsPage c) => ((Control)c.AutoStartToggleSwitch).IsEnabled), (object)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.Bind<SettingsPageViewModel, SettingsPage, bool, bool>(this, ViewModel, (Expression<Func<SettingsPageViewModel, bool>>)((SettingsPageViewModel vm) => vm.AutoConnect), (Expression<Func<SettingsPage, bool>>)((SettingsPage c) => c.AutoConnectToggleSwitch.IsOn), (object)null, (IBindingTypeConverter)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.Bind<SettingsPageViewModel, SettingsPage, bool, bool>(this, ViewModel, (Expression<Func<SettingsPageViewModel, bool>>)((SettingsPageViewModel vm) => vm.DropQuic), (Expression<Func<SettingsPage, bool>>)((SettingsPage c) => c.DropQuicToggleSwitch.IsOn), (object)null, (IBindingTypeConverter)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.Bind<SettingsPageViewModel, SettingsPage, bool, bool>(this, ViewModel, (Expression<Func<SettingsPageViewModel, bool>>)((SettingsPageViewModel vm) => vm.DropUdp), (Expression<Func<SettingsPage, bool>>)((SettingsPage c) => c.DropUdpToggleSwitch.IsOn), (object)null, (IBindingTypeConverter)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.Bind<SettingsPageViewModel, SettingsPage, bool, bool>(this, ViewModel, (Expression<Func<SettingsPageViewModel, bool>>)((SettingsPageViewModel vm) => vm.IsSmartHealEnabled), (Expression<Func<SettingsPage, bool>>)((SettingsPage c) => c.SmartHealToggleSwitch.IsOn), (object)null, (IBindingTypeConverter)null, (IBindingTypeConverter)null));
			d(ObservableExtensions.Subscribe<EnumSource<ChannelProtocol>>(Observable.ObserveOn<EnumSource<ChannelProtocol>>(WhenAnyMixin.WhenAnyValue<SettingsPage, EnumSource<ChannelProtocol>>(this, (Expression<Func<SettingsPage, EnumSource<ChannelProtocol>>>)((SettingsPage x) => x.ViewModel.SelectedChannelProtocol)), RxSchedulers.MainThreadScheduler), (Action<EnumSource<ChannelProtocol>>)delegate(EnumSource<ChannelProtocol> s)
			{
				((Selector)ChannelProtocolComboBox).SelectedItem = s;
			}));
			d((IDisposable)PropertyBindingMixins.OneWayBind<SettingsPageViewModel, SettingsPage, string, string>(this, ViewModel, (Expression<Func<SettingsPageViewModel, string>>)((SettingsPageViewModel vm) => vm.AccountInfo.SubscriptionStatusString), (Expression<Func<SettingsPage, string>>)((SettingsPage c) => c.SubscriptionStatusTextBlock.Text), (object)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.OneWayBind<SettingsPageViewModel, SettingsPage, AccountInfoViewModel.SubscriptionStatus, Visibility>(this, ViewModel, (Expression<Func<SettingsPageViewModel, AccountInfoViewModel.SubscriptionStatus>>)((SettingsPageViewModel vm) => vm.AccountInfo.SubStatus), (Expression<Func<SettingsPage, Visibility>>)((SettingsPage c) => ((UIElement)c.BuySubscriptionButton).Visibility), (Func<AccountInfoViewModel.SubscriptionStatus, Visibility>)((AccountInfoViewModel.SubscriptionStatus t) => (Visibility)(t == AccountInfoViewModel.SubscriptionStatus.Active))));
			d((IDisposable)PropertyBindingMixins.OneWayBind<SettingsPageViewModel, SettingsPage, AccountInfoViewModel.SubscriptionStatus, Visibility>(this, ViewModel, (Expression<Func<SettingsPageViewModel, AccountInfoViewModel.SubscriptionStatus>>)((SettingsPageViewModel vm) => vm.AccountInfo.SubStatus), (Expression<Func<SettingsPage, Visibility>>)((SettingsPage c) => ((UIElement)c.ManageSubscriptionsButton).Visibility), (Func<AccountInfoViewModel.SubscriptionStatus, Visibility>)((AccountInfoViewModel.SubscriptionStatus t) => (Visibility)(t != AccountInfoViewModel.SubscriptionStatus.Active))));
			d(ObservableExtensions.Subscribe<AccountInfoViewModel.SubscriptionStatus>(Observable.ObserveOn<AccountInfoViewModel.SubscriptionStatus>(WhenAnyMixin.WhenAnyValue<SettingsPage, AccountInfoViewModel.SubscriptionStatus>(this, (Expression<Func<SettingsPage, AccountInfoViewModel.SubscriptionStatus>>)((SettingsPage x) => x.ViewModel.AccountInfo.SubStatus)), RxSchedulers.MainThreadScheduler), (Action<AccountInfoViewModel.SubscriptionStatus>)UpdateSubscriptionStatusDisplay));
			d(ObservableExtensions.Subscribe<DateTime?>(Observable.ObserveOn<DateTime?>(WhenAnyMixin.WhenAnyValue<SettingsPage, DateTime?>(this, (Expression<Func<SettingsPage, DateTime?>>)((SettingsPage x) => x.ViewModel.AccountInfo.SubscriptionExpirationDate)), RxSchedulers.MainThreadScheduler), (Action<DateTime?>)UpdateExpirationDateDisplay));
			d(ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(WhenAnyMixin.WhenAnyValue<SettingsPage, string>(this, (Expression<Func<SettingsPage, string>>)((SettingsPage x) => x.ViewModel.TrafficUsageText)), RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string text)
			{
				TrafficUsageTextBlock.Text = text;
				((UIElement)TrafficUsageTextBlock).Visibility = (Visibility)(string.IsNullOrEmpty(text) ? 1 : 0);
			}));
			d(ObservableExtensions.Subscribe<string>(Observable.ObserveOn<string>(WhenAnyMixin.WhenAnyValue<SettingsPage, string>(this, (Expression<Func<SettingsPage, string>>)((SettingsPage x) => x.ViewModel.ResetCountdownText)), RxSchedulers.MainThreadScheduler), (Action<string>)delegate(string text)
			{
				ResetCountdownTextBlock.Text = text;
				((UIElement)ResetCountdownTextBlock).Visibility = (Visibility)(string.IsNullOrEmpty(text) ? 1 : 0);
			}));
		});
		((Selector)ChannelProtocolComboBox).SelectionChanged += (SelectionChangedEventHandler)delegate
		{
			if (((Selector)ChannelProtocolComboBox).SelectedItem is EnumSource<ChannelProtocol> selectedChannelProtocol && ViewModel != null)
			{
				ViewModel.SelectedChannelProtocol = selectedChannelProtocol;
			}
		};
		((ButtonBase)ResetConnectionDefaultsButton).Command = ViewModel?.ResetConnectionDefaults;
		((ButtonBase)ChangeUserTokenButton).Command = ViewModel?.AccountInfo.ChangeUserToken;
		((ButtonBase)BuySubscriptionButton).Command = ViewModel?.AccountInfo.ManageSubscription;
		((ButtonBase)VisitWebsiteButton).Command = ViewModel?.AccountInfo.VisitSite;
		((ButtonBase)ManageSubscriptionsButton).Command = ViewModel?.AccountInfo.VisitSubscriptionManagementPage;
		((ButtonBase)OAuthSignInButton).Command = _accountViewModel?.SignIn;
		((ButtonBase)OAuthSignOutButton).Command = _accountViewModel?.SignOut;
		((ButtonBase)ReferralCopyLinkButton).Click += (RoutedEventHandler)delegate
		{
			CopyToClipboard(_accountViewModel?.ReferralShareUrl);
		};
		((FrameworkElement)this).Loaded += new RoutedEventHandler(SettingsPage_Loaded);
	}

	private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			ViewModel?.GetSettings.Execute(Unit.Default);
			ViewModel?.AccountInfo.ValidateToken.Execute(Unit.Default);
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Error loading settings page: {Message}", ex.Message);
		}
	}

	private void UpdateSubscriptionStatusDisplay(AccountInfoViewModel.SubscriptionStatus status)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		TextBlock subscriptionStatusTextBlock = SubscriptionStatusTextBlock;
		if (1 == 0)
		{
		}
		Style style = (Style)(status switch
		{
			AccountInfoViewModel.SubscriptionStatus.Active => (object)(Style)((FrameworkElement)this).Resources[(object)"StatusActiveStyle"], 
			AccountInfoViewModel.SubscriptionStatus.Expired => (object)(Style)((FrameworkElement)this).Resources[(object)"StatusExpiredStyle"], 
			_ => (object)(Style)((FrameworkElement)this).Resources[(object)"StatusInactiveStyle"], 
		});
		if (1 == 0)
		{
		}
		((FrameworkElement)subscriptionStatusTextBlock).Style = style;
		if (RenewalMessageTextBlock != (TextBlock)null)
		{
			((UIElement)RenewalMessageTextBlock).Visibility = (Visibility)(status != AccountInfoViewModel.SubscriptionStatus.Expired);
		}
	}

	private void UpdateExpirationDateDisplay(DateTime? expirationDate)
	{
		object obj = ((FrameworkElement)this).FindName("ExpirationDateTextBlock");
		TextBlock val = (TextBlock)((obj is TextBlock) ? obj : null);
		if (!(val == (TextBlock)null))
		{
			if (expirationDate.HasValue)
			{
				CultureInfo currentCulture = CultureInfo.CurrentCulture;
				val.Text = string.Format(currentCulture, Loc.Get("SubscriptionExpiresFormat"), expirationDate.Value.ToString("d", currentCulture));
				((UIElement)val).Visibility = (Visibility)0;
			}
			else
			{
				((UIElement)val).Visibility = (Visibility)1;
			}
		}
	}

	private void CopyToClipboardButton_OnClick(object sender, RoutedEventArgs e)
	{
		try
		{
			if (ViewModel?.AccountInfo.UserToken != null)
			{
				CopyToClipboard(ViewModel.AccountInfo.UserToken);
			}
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Error copying User ID to clipboard: {Message}", ex.Message);
		}
	}

	private static void CopyToClipboard(string? text)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		try
		{
			DataPackage val = new DataPackage();
			val.SetText(text);
			Clipboard.SetContent(val);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error copying to clipboard");
		}
	}

	private void SplitTunnelButton_OnClick(object sender, RoutedEventArgs e)
	{
		try
		{
			App.GetInstance<INavigationService>()?.NavigateTo(NavigationIntent.SplitTunnelPage);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to navigate to SplitTunnelPage");
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("ms-appx:///Pages/SettingsPage.xaml");
			Application.LoadComponent((object)this, uri, (ComponentResourceLocation)0);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected O, but got Unknown
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Expected O, but got Unknown
		switch (connectionId)
		{
		case 2:
			OAuthAccountCard = CastExtensions.As<Border>(target);
			break;
		case 3:
			ReferralCard = CastExtensions.As<Border>(target);
			break;
		case 4:
			SplitTunnelButton = CastExtensions.As<Button>(target);
			((ButtonBase)SplitTunnelButton).Click += new RoutedEventHandler(SplitTunnelButton_OnClick);
			break;
		case 5:
			AutoStartToggleSwitch = CastExtensions.As<ToggleSwitch>(target);
			break;
		case 6:
			AutoConnectToggleSwitch = CastExtensions.As<ToggleSwitch>(target);
			break;
		case 7:
			ResetConnectionDefaultsButton = CastExtensions.As<Button>(target);
			break;
		case 8:
			DropQuicToggleSwitch = CastExtensions.As<ToggleSwitch>(target);
			break;
		case 9:
			DropUdpToggleSwitch = CastExtensions.As<ToggleSwitch>(target);
			break;
		case 10:
			ChannelProtocolComboBox = CastExtensions.As<ComboBox>(target);
			break;
		case 11:
			SmartHealToggleSwitch = CastExtensions.As<ToggleSwitch>(target);
			break;
		case 12:
			ChangeUserTokenButton = CastExtensions.As<Button>(target);
			break;
		case 13:
			BuySubscriptionButton = CastExtensions.As<Button>(target);
			break;
		case 14:
			ManageSubscriptionsButton = CastExtensions.As<Button>(target);
			break;
		case 15:
			VisitWebsiteButton = CastExtensions.As<Button>(target);
			break;
		case 16:
			ExpirationDateTextBlock = CastExtensions.As<TextBlock>(target);
			break;
		case 17:
			RenewalMessageTextBlock = CastExtensions.As<TextBlock>(target);
			break;
		case 18:
			TrafficUsageTextBlock = CastExtensions.As<TextBlock>(target);
			break;
		case 19:
			ResetCountdownTextBlock = CastExtensions.As<TextBlock>(target);
			break;
		case 20:
			SubscriptionStatusTextBlock = CastExtensions.As<TextBlock>(target);
			break;
		case 21:
			UserIdTextBox = CastExtensions.As<TextBox>(target);
			break;
		case 22:
			CopyToClipboardButton = CastExtensions.As<Button>(target);
			((ButtonBase)CopyToClipboardButton).Click += new RoutedEventHandler(CopyToClipboardButton_OnClick);
			break;
		case 23:
			ReferralShareUrlTextBox = CastExtensions.As<TextBox>(target);
			break;
		case 24:
			ReferralCopyLinkButton = CastExtensions.As<Button>(target);
			break;
		case 25:
			OAuthSignInButton = CastExtensions.As<Button>(target);
			break;
		case 26:
			OAuthSignOutButton = CastExtensions.As<Button>(target);
			break;
		case 27:
			OAuthAccountDisplayNameTextBlock = CastExtensions.As<TextBlock>(target);
			break;
		case 28:
			OAuthAccountStateTextBlock = CastExtensions.As<TextBlock>(target);
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
