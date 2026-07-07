using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.ViewModels;
using Ciphra.VPN.WinUI.Services;
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
public sealed class SplitTunnelPage : Page, IViewFor<SplitTunnelPageViewModel>, IViewFor, IActivatableView, IComponentConnector
{
	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Expander LocalNetworkExpander;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Expander RoutingExpander;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Expander AlwaysBlockExpander;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock ApplyMessageTextBlock;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button ResetButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBox BlockIpRangesTextBox;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBox BlockDomainsTextBox;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock RoutingModeCaption;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private StackPanel CriteriaPanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBox IpRangesTextBox;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBox DomainsTextBox;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private ComboBox RoutingModeComboBox;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private ToggleSwitch BypassLanToggle;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock StatusTextBlock;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button BackButton;

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
			ViewModel = (SplitTunnelPageViewModel)value;
		}
	}

	public SplitTunnelPageViewModel? ViewModel { get; set; }

	public SplitTunnelPage()
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected O, but got Unknown
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Expected O, but got Unknown
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Expected O, but got Unknown
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Expected O, but got Unknown
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Expected O, but got Unknown
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Expected O, but got Unknown
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Expected O, but got Unknown
		ViewModel = App.GetInstance<SplitTunnelPageViewModel>() ?? throw new InvalidOperationException("SplitTunnelPageViewModel is not registered in the container.");
		InitializeComponent();
		((ItemsControl)RoutingModeComboBox).ItemsSource = SplitTunnelPageViewModel.RoutingModes;
		ViewForMixins.WhenActivated((IActivatableView)(object)this, (Action<Action<IDisposable>>)delegate(Action<IDisposable> d)
		{
			d((IDisposable)PropertyBindingMixins.OneWayBind<SplitTunnelPageViewModel, SplitTunnelPage, string, string>(this, ViewModel, (Expression<Func<SplitTunnelPageViewModel, string>>)((SplitTunnelPageViewModel vm) => vm.StatusText), (Expression<Func<SplitTunnelPage, string>>)((SplitTunnelPage c) => c.StatusTextBlock.Text), (object)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.OneWayBind<SplitTunnelPageViewModel, SplitTunnelPage, string, string>(this, ViewModel, (Expression<Func<SplitTunnelPageViewModel, string>>)((SplitTunnelPageViewModel vm) => vm.ApplyMessageText), (Expression<Func<SplitTunnelPage, string>>)((SplitTunnelPage c) => c.ApplyMessageTextBlock.Text), (object)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.Bind<SplitTunnelPageViewModel, SplitTunnelPage, bool, bool>(this, ViewModel, (Expression<Func<SplitTunnelPageViewModel, bool>>)((SplitTunnelPageViewModel vm) => vm.UseSplitLocalNetwork), (Expression<Func<SplitTunnelPage, bool>>)((SplitTunnelPage c) => c.BypassLanToggle.IsOn), (object)null, (IBindingTypeConverter)null, (IBindingTypeConverter)null));
			d(ObservableExtensions.Subscribe<EnumSource<RoutingMode>>(Observable.ObserveOn<EnumSource<RoutingMode>>(WhenAnyMixin.WhenAnyValue<SplitTunnelPage, EnumSource<RoutingMode>>(this, (Expression<Func<SplitTunnelPage, EnumSource<RoutingMode>>>)((SplitTunnelPage x) => x.ViewModel.SelectedRoutingMode)), RxSchedulers.MainThreadScheduler), (Action<EnumSource<RoutingMode>>)delegate(EnumSource<RoutingMode> m)
			{
				((Selector)RoutingModeComboBox).SelectedItem = m;
			}));
			d(ObservableExtensions.Subscribe<EnumSource<RoutingMode>>(Observable.ObserveOn<EnumSource<RoutingMode>>(WhenAnyMixin.WhenAnyValue<SplitTunnelPage, EnumSource<RoutingMode>>(this, (Expression<Func<SplitTunnelPage, EnumSource<RoutingMode>>>)((SplitTunnelPage x) => x.ViewModel.SelectedRoutingMode)), RxSchedulers.MainThreadScheduler), (Action<EnumSource<RoutingMode>>)delegate(EnumSource<RoutingMode> m)
			{
				TextBlock routingModeCaption = RoutingModeCaption;
				RoutingMode value = m.Value;
				if (1 == 0)
				{
				}
				string text = value switch
				{
					RoutingMode.TunnelAll => Loc.Get("RoutingModeTunnelAllCaption"), 
					RoutingMode.TunnelOnly => Loc.Get("RoutingModeTunnelOnlyCaption"), 
					RoutingMode.BypassThese => Loc.Get("RoutingModeBypassCaption"), 
					_ => string.Empty, 
				};
				if (1 == 0)
				{
				}
				routingModeCaption.Text = text;
				((UIElement)CriteriaPanel).Visibility = (Visibility)(m.Value == RoutingMode.TunnelAll);
			}));
			d((IDisposable)PropertyBindingMixins.Bind<SplitTunnelPageViewModel, SplitTunnelPage, string, string>(this, ViewModel, (Expression<Func<SplitTunnelPageViewModel, string>>)((SplitTunnelPageViewModel vm) => vm.IpRangesText), (Expression<Func<SplitTunnelPage, string>>)((SplitTunnelPage c) => c.IpRangesTextBox.Text), (object)null, (IBindingTypeConverter)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.Bind<SplitTunnelPageViewModel, SplitTunnelPage, string, string>(this, ViewModel, (Expression<Func<SplitTunnelPageViewModel, string>>)((SplitTunnelPageViewModel vm) => vm.DomainsText), (Expression<Func<SplitTunnelPage, string>>)((SplitTunnelPage c) => c.DomainsTextBox.Text), (object)null, (IBindingTypeConverter)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.Bind<SplitTunnelPageViewModel, SplitTunnelPage, string, string>(this, ViewModel, (Expression<Func<SplitTunnelPageViewModel, string>>)((SplitTunnelPageViewModel vm) => vm.BlockIpRangesText), (Expression<Func<SplitTunnelPage, string>>)((SplitTunnelPage c) => c.BlockIpRangesTextBox.Text), (object)null, (IBindingTypeConverter)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.Bind<SplitTunnelPageViewModel, SplitTunnelPage, string, string>(this, ViewModel, (Expression<Func<SplitTunnelPageViewModel, string>>)((SplitTunnelPageViewModel vm) => vm.BlockDomainsText), (Expression<Func<SplitTunnelPage, string>>)((SplitTunnelPage c) => c.BlockDomainsTextBox.Text), (object)null, (IBindingTypeConverter)null, (IBindingTypeConverter)null));
		});
		((Selector)RoutingModeComboBox).SelectionChanged += (SelectionChangedEventHandler)delegate
		{
			if (((Selector)RoutingModeComboBox).SelectedItem is EnumSource<RoutingMode> selectedRoutingMode && ViewModel != null)
			{
				ViewModel.SelectedRoutingMode = selectedRoutingMode;
			}
		};
		((ButtonBase)ResetButton).Click += new RoutedEventHandler(ResetButton_OnClick);
		((UIElement)this).LostFocus += (RoutedEventHandler)delegate
		{
			ApplyPendingSplitTunnelChanges();
		};
		((UIElement)BypassLanToggle).LostFocus += (RoutedEventHandler)delegate
		{
			ApplyPendingSplitTunnelChanges();
		};
		((UIElement)RoutingModeComboBox).LostFocus += (RoutedEventHandler)delegate
		{
			ApplyPendingSplitTunnelChanges();
		};
		((UIElement)IpRangesTextBox).LostFocus += (RoutedEventHandler)delegate
		{
			ApplyPendingSplitTunnelChanges();
		};
		((UIElement)DomainsTextBox).LostFocus += (RoutedEventHandler)delegate
		{
			ApplyPendingSplitTunnelChanges();
		};
		((UIElement)BlockIpRangesTextBox).LostFocus += (RoutedEventHandler)delegate
		{
			ApplyPendingSplitTunnelChanges();
		};
		((UIElement)BlockDomainsTextBox).LostFocus += (RoutedEventHandler)delegate
		{
			ApplyPendingSplitTunnelChanges();
		};
		((FrameworkElement)this).Loaded += new RoutedEventHandler(SplitTunnelPage_Loaded);
		((FrameworkElement)this).Unloaded += (RoutedEventHandler)delegate
		{
			ApplyPendingSplitTunnelChanges();
		};
	}

	private void SplitTunnelPage_Loaded(object sender, RoutedEventArgs e)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			ViewModel?.GetSettings.Execute(Unit.Default);
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Error loading split tunnel page: {Message}", ex.Message);
		}
	}

	private void BackButton_OnClick(object sender, RoutedEventArgs e)
	{
		try
		{
			ApplyPendingSplitTunnelChanges();
			App.GetInstance<INavigationService>()?.NavigateTo(NavigationIntent.SettingsPage);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to navigate back from SplitTunnelPage");
		}
	}

	private void ResetButton_OnClick(object sender, RoutedEventArgs e)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			ViewModel?.ResetToDefaults.Execute(Unit.Default);
			CollapseAllExpanders();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to reset split tunnel defaults");
		}
	}

	private void CollapseAllExpanders()
	{
		LocalNetworkExpander.IsExpanded = false;
		RoutingExpander.IsExpanded = false;
		AlwaysBlockExpander.IsExpanded = false;
	}

	private void ApplyPendingSplitTunnelChanges()
	{
		try
		{
			ViewModel?.ApplyPendingChanges();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to apply split tunnel changes");
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("ms-appx:///Pages/SplitTunnelPage.xaml");
			Application.LoadComponent((object)this, uri, (ComponentResourceLocation)0);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Expected O, but got Unknown
		switch (connectionId)
		{
		case 3:
			LocalNetworkExpander = CastExtensions.As<Expander>(target);
			break;
		case 4:
			RoutingExpander = CastExtensions.As<Expander>(target);
			break;
		case 5:
			AlwaysBlockExpander = CastExtensions.As<Expander>(target);
			break;
		case 6:
			ApplyMessageTextBlock = CastExtensions.As<TextBlock>(target);
			break;
		case 7:
			ResetButton = CastExtensions.As<Button>(target);
			break;
		case 8:
			BlockIpRangesTextBox = CastExtensions.As<TextBox>(target);
			break;
		case 9:
			BlockDomainsTextBox = CastExtensions.As<TextBox>(target);
			break;
		case 10:
			RoutingModeCaption = CastExtensions.As<TextBlock>(target);
			break;
		case 11:
			CriteriaPanel = CastExtensions.As<StackPanel>(target);
			break;
		case 12:
			IpRangesTextBox = CastExtensions.As<TextBox>(target);
			break;
		case 13:
			DomainsTextBox = CastExtensions.As<TextBox>(target);
			break;
		case 14:
			RoutingModeComboBox = CastExtensions.As<ComboBox>(target);
			break;
		case 15:
			BypassLanToggle = CastExtensions.As<ToggleSwitch>(target);
			break;
		case 16:
			StatusTextBlock = CastExtensions.As<TextBlock>(target);
			break;
		case 17:
			BackButton = CastExtensions.As<Button>(target);
			((ButtonBase)BackButton).Click += new RoutedEventHandler(BackButton_OnClick);
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
