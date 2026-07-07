using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reactive;
using Ciphra.VPN.Common.ViewModels;
using Ciphra.VPN.Common.ViewModels.DataViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using ReactiveUI;
using Serilog;
using WinRT;
using WinRT.Ciphra_VPN_WinUIVtableClasses;

namespace Ciphra.VPN.WinUI.Pages;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(Ciphra_VPN_WinUI_Dialogs_TrafficLimitDialogPageWinRTTypeDetails))]
public sealed class LocationsPage : Page, IViewFor<LocationsPageViewModel>, IViewFor, IActivatableView, IComponentConnector
{
	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private StackPanel LoadingPanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private ListView ServersListView;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Border UpgradeButtonSection;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button UpgradeButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private AutoSuggestBox FilterAutoSuggestBox;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button RefreshButton;

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
			ViewModel = (LocationsPageViewModel)value;
		}
	}

	public LocationsPageViewModel? ViewModel { get; set; }

	public LocationsPage()
	{
		ViewModel = App.GetInstance<LocationsPageViewModel>() ?? throw new InvalidOperationException("AccountPageViewModel is not registered in the container.");
		InitializeComponent();
		ViewForMixins.WhenActivated((IActivatableView)(object)this, (Action<Action<IDisposable>>)delegate(Action<IDisposable> d)
		{
			d((IDisposable)PropertyBindingMixins.OneWayBind<LocationsPageViewModel, LocationsPage, IEnumerable<VpnServerViewModel>, object>(this, ViewModel, (Expression<Func<LocationsPageViewModel, IEnumerable<VpnServerViewModel>>>)((LocationsPageViewModel vm) => vm.VpnServers), (Expression<Func<LocationsPage, object>>)((LocationsPage v) => ((ItemsControl)v.ServersListView).ItemsSource), (object)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.Bind<LocationsPageViewModel, LocationsPage, string, string>(this, ViewModel, (Expression<Func<LocationsPageViewModel, string>>)((LocationsPageViewModel vm) => vm.FilterQuery), (Expression<Func<LocationsPage, string>>)((LocationsPage v) => v.FilterAutoSuggestBox.Text), (object)null, (IBindingTypeConverter)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.OneWayBind<LocationsPageViewModel, LocationsPage, AccountInfoViewModel.SubscriptionStatus, Visibility>(this, ViewModel, (Expression<Func<LocationsPageViewModel, AccountInfoViewModel.SubscriptionStatus>>)((LocationsPageViewModel vm) => vm.AccountInfo.SubStatus), (Expression<Func<LocationsPage, Visibility>>)((LocationsPage c) => ((UIElement)c.UpgradeButtonSection).Visibility), (Func<AccountInfoViewModel.SubscriptionStatus, Visibility>)((AccountInfoViewModel.SubscriptionStatus t) => (Visibility)(t == AccountInfoViewModel.SubscriptionStatus.Active))));
			d((IDisposable)PropertyBindingMixins.OneWayBind<LocationsPageViewModel, LocationsPage, AccountInfoViewModel.SubscriptionStatus, Thickness>(this, ViewModel, (Expression<Func<LocationsPageViewModel, AccountInfoViewModel.SubscriptionStatus>>)((LocationsPageViewModel vm) => vm.AccountInfo.SubStatus), (Expression<Func<LocationsPage, Thickness>>)((LocationsPage v) => ((Control)v.ServersListView).Padding), (Func<AccountInfoViewModel.SubscriptionStatus, Thickness>)((AccountInfoViewModel.SubscriptionStatus t) => (t == AccountInfoViewModel.SubscriptionStatus.Active) ? new Thickness(0.0) : new Thickness(0.0, 0.0, 0.0, 90.0))));
			d((IDisposable)PropertyBindingMixins.OneWayBind<LocationsPageViewModel, LocationsPage, bool, Visibility>(this, ViewModel, (Expression<Func<LocationsPageViewModel, bool>>)((LocationsPageViewModel vm) => vm.IsLoading), (Expression<Func<LocationsPage, Visibility>>)((LocationsPage v) => ((UIElement)v.LoadingPanel).Visibility), (Func<bool, Visibility>)((bool isLoading) => (Visibility)(!isLoading))));
		});
		((ButtonBase)RefreshButton).Command = ViewModel?.GetServers;
		((ButtonBase)UpgradeButton).Command = ViewModel?.AccountInfo.ManageSubscription;
	}

	private void ServersListView_OnItemClick(object sender, ItemClickEventArgs e)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (e.ClickedItem is VpnServerViewModel vpnServerViewModel)
			{
				vpnServerViewModel.Select.Execute(Unit.Default);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to navigate to server");
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("ms-appx:///Pages/LocationsPage.xaml");
			Application.LoadComponent((object)this, uri, (ComponentResourceLocation)0);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		switch (connectionId)
		{
		case 19:
			LoadingPanel = CastExtensions.As<StackPanel>(target);
			break;
		case 20:
			ServersListView = CastExtensions.As<ListView>(target);
			((ListViewBase)ServersListView).ItemClick += new ItemClickEventHandler(ServersListView_OnItemClick);
			break;
		case 21:
			UpgradeButtonSection = CastExtensions.As<Border>(target);
			break;
		case 22:
			UpgradeButton = CastExtensions.As<Button>(target);
			break;
		case 25:
			FilterAutoSuggestBox = CastExtensions.As<AutoSuggestBox>(target);
			break;
		case 26:
			RefreshButton = CastExtensions.As<Button>(target);
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
