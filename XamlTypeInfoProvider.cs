using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Ciphra.VPN.Common.ViewModels;
using Ciphra.VPN.Common.ViewModels.DialogViewModels;
using Ciphra.VPN.WinUI.AppWindows;
using Ciphra.VPN.WinUI.Controls;
using Ciphra.VPN.WinUI.Controls.Icons;
using Ciphra.VPN.WinUI.Dialogs;
using Ciphra.VPN.WinUI.Pages;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.XamlTypeInfo;
using Microsoft.Web.WebView2.Core;
using ReactiveUI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;

namespace Ciphra.VPN.WinUI.Ciphra_VPN_WinUI_XamlTypeInfo;

[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
[DebuggerNonUserCode]
internal class XamlTypeInfoProvider
{
	private Dictionary<string, IXamlType> _xamlTypeCacheByName = new Dictionary<string, IXamlType>();

	private Dictionary<Type, IXamlType> _xamlTypeCacheByType = new Dictionary<Type, IXamlType>();

	private Dictionary<string, IXamlMember> _xamlMembers = new Dictionary<string, IXamlMember>();

	private string[] _typeNameTable = null;

	private Type[] _typeTable = null;

	private List<IXamlMetadataProvider> _otherProviders;

	private List<IXamlMetadataProvider> OtherProviders
	{
		get
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			if (_otherProviders == null)
			{
				List<IXamlMetadataProvider> list = new List<IXamlMetadataProvider>();
				IXamlMetadataProvider item = (IXamlMetadataProvider)new XamlControlsXamlMetaDataProvider();
				list.Add(item);
				_otherProviders = list;
			}
			return _otherProviders;
		}
	}

	public IXamlType GetXamlTypeByType(Type type)
	{
		IXamlType value;
		lock (_xamlTypeCacheByType)
		{
			if (_xamlTypeCacheByType.TryGetValue(type, out value))
			{
				return value;
			}
			int num = LookupTypeIndexByType(type);
			if (num != -1)
			{
				value = CreateXamlType(num);
			}
			XamlUserType xamlUserType = value as XamlUserType;
			if (value == null || (xamlUserType != null && xamlUserType.IsReturnTypeStub && !xamlUserType.IsLocalType))
			{
				IXamlType val = CheckOtherMetadataProvidersForType(type);
				if (val != null && (val.IsConstructible || value == null))
				{
					value = val;
				}
			}
			if (value != null)
			{
				_xamlTypeCacheByName.Add(value.FullName, value);
				_xamlTypeCacheByType.Add(value.UnderlyingType, value);
			}
		}
		return value;
	}

	public IXamlType GetXamlTypeByName(string typeName)
	{
		if (string.IsNullOrEmpty(typeName))
		{
			return null;
		}
		IXamlType value;
		lock (_xamlTypeCacheByType)
		{
			if (_xamlTypeCacheByName.TryGetValue(typeName, out value))
			{
				return value;
			}
			int num = LookupTypeIndexByName(typeName);
			if (num != -1)
			{
				value = CreateXamlType(num);
			}
			XamlUserType xamlUserType = value as XamlUserType;
			if (value == null || (xamlUserType != null && xamlUserType.IsReturnTypeStub && !xamlUserType.IsLocalType))
			{
				IXamlType val = CheckOtherMetadataProvidersForName(typeName);
				if (val != null && (val.IsConstructible || value == null))
				{
					value = val;
				}
			}
			if (value != null)
			{
				_xamlTypeCacheByName.Add(value.FullName, value);
				_xamlTypeCacheByType.Add(value.UnderlyingType, value);
			}
		}
		return value;
	}

	public IXamlMember GetMemberByLongName(string longMemberName)
	{
		if (string.IsNullOrEmpty(longMemberName))
		{
			return null;
		}
		IXamlMember value;
		lock (_xamlMembers)
		{
			if (_xamlMembers.TryGetValue(longMemberName, out value))
			{
				return value;
			}
			value = CreateXamlMember(longMemberName);
			if (value != null)
			{
				_xamlMembers.Add(longMemberName, value);
			}
		}
		return value;
	}

	private void InitTypeTables()
	{
		_typeNameTable = new string[109];
		_typeNameTable[0] = "Microsoft.UI.Xaml.Controls.XamlControlsResources";
		_typeNameTable[1] = "Microsoft.UI.Xaml.ResourceDictionary";
		_typeNameTable[2] = "Object";
		_typeNameTable[3] = "Boolean";
		_typeNameTable[4] = "Microsoft.UI.Xaml.Media.MicaBackdrop";
		_typeNameTable[5] = "Microsoft.UI.Xaml.Media.SystemBackdrop";
		_typeNameTable[6] = "Microsoft.UI.Composition.SystemBackdrops.MicaKind";
		_typeNameTable[7] = "System.Enum";
		_typeNameTable[8] = "System.ValueType";
		_typeNameTable[9] = "Microsoft.UI.Xaml.Controls.ProgressRing";
		_typeNameTable[10] = "Microsoft.UI.Xaml.Controls.Control";
		_typeNameTable[11] = "Double";
		_typeNameTable[12] = "Microsoft.UI.Xaml.Controls.ProgressRingTemplateSettings";
		_typeNameTable[13] = "Microsoft.UI.Xaml.DependencyObject";
		_typeNameTable[14] = "Ciphra.VPN.WinUI.AppWindows.AuthWindow";
		_typeNameTable[15] = "Microsoft.UI.Xaml.Window";
		_typeNameTable[16] = "String";
		_typeNameTable[17] = "Ciphra.VPN.WinUI.AppWindows.PaymentWindow";
		_typeNameTable[18] = "Ciphra.VPN.WinUI.Controls.AppTitleBar";
		_typeNameTable[19] = "Microsoft.UI.Xaml.Controls.UserControl";
		_typeNameTable[20] = "Microsoft.UI.Xaml.Controls.NavigationView";
		_typeNameTable[21] = "Microsoft.UI.Xaml.Controls.ContentControl";
		_typeNameTable[22] = "Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode";
		_typeNameTable[23] = "Microsoft.UI.Xaml.Controls.NavigationViewOverflowLabelMode";
		_typeNameTable[24] = "Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible";
		_typeNameTable[25] = "System.Collections.Generic.IList`1<Object>";
		_typeNameTable[26] = "Microsoft.UI.Xaml.Controls.AutoSuggestBox";
		_typeNameTable[27] = "Microsoft.UI.Xaml.UIElement";
		_typeNameTable[28] = "Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode";
		_typeNameTable[29] = "Microsoft.UI.Xaml.DataTemplate";
		_typeNameTable[30] = "Microsoft.UI.Xaml.Style";
		_typeNameTable[31] = "Microsoft.UI.Xaml.Controls.StyleSelector";
		_typeNameTable[32] = "Microsoft.UI.Xaml.Controls.DataTemplateSelector";
		_typeNameTable[33] = "Microsoft.UI.Xaml.Controls.NavigationViewSelectionFollowsFocus";
		_typeNameTable[34] = "Microsoft.UI.Xaml.Controls.NavigationViewShoulderNavigationEnabled";
		_typeNameTable[35] = "Microsoft.UI.Xaml.Controls.NavigationViewTemplateSettings";
		_typeNameTable[36] = "Microsoft.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter";
		_typeNameTable[37] = "Microsoft.UI.Xaml.Controls.IconElement";
		_typeNameTable[38] = "Microsoft.UI.Xaml.Controls.InfoBadge";
		_typeNameTable[39] = "Microsoft.UI.Xaml.Controls.Primitives.NavigationViewItemPresenterTemplateSettings";
		_typeNameTable[40] = "Microsoft.UI.Xaml.Controls.NavigationViewItem";
		_typeNameTable[41] = "Microsoft.UI.Xaml.Controls.NavigationViewItemBase";
		_typeNameTable[42] = "Ciphra.VPN.WinUI.Controls.Icons.Locations";
		_typeNameTable[43] = "Ciphra.VPN.WinUI.Controls.Icons.VpnIcon";
		_typeNameTable[44] = "Ciphra.VPN.WinUI.Controls.AppShell";
		_typeNameTable[45] = "Ciphra.VPN.Common.ViewModels.AppShellViewModel";
		_typeNameTable[46] = "ReactiveUI.ReactiveObject";
		_typeNameTable[47] = "Ciphra.VPN.WinUI.Controls.Icons.Location";
		_typeNameTable[48] = "Ciphra.VPN.WinUI.Dialogs.AuthDialogPage";
		_typeNameTable[49] = "Microsoft.UI.Xaml.Controls.Page";
		_typeNameTable[50] = "Ciphra.VPN.Common.ViewModels.DialogViewModels.AuthDialogViewModel";
		_typeNameTable[51] = "Microsoft.UI.Xaml.Controls.WebView2";
		_typeNameTable[52] = "Microsoft.UI.Xaml.FrameworkElement";
		_typeNameTable[53] = "Microsoft.Web.WebView2.Core.CoreWebView2";
		_typeNameTable[54] = "Windows.UI.Color";
		_typeNameTable[55] = "System.Uri";
		_typeNameTable[56] = "Ciphra.VPN.WinUI.Dialogs.AuthPage";
		_typeNameTable[57] = "Ciphra.VPN.WinUI.Dialogs.ElevationPromptDialog";
		_typeNameTable[58] = "Ciphra.VPN.WinUI.Dialogs.ErrorDialogPage";
		_typeNameTable[59] = "Ciphra.VPN.Common.ViewModels.DialogViewModels.ErrorDialogViewModel";
		_typeNameTable[60] = "Ciphra.VPN.WinUI.Dialogs.PaymentPage";
		_typeNameTable[61] = "Ciphra.VPN.WinUI.Dialogs.PromoDialogPage";
		_typeNameTable[62] = "Ciphra.VPN.WinUI.Dialogs.RateUsDialogPage";
		_typeNameTable[63] = "Ciphra.VPN.Common.ViewModels.DialogViewModels.RateUsDialogViewModel";
		_typeNameTable[64] = "Ciphra.VPN.WinUI.Dialogs.SubscribeDialogPage";
		_typeNameTable[65] = "Ciphra.VPN.Common.ViewModels.DialogViewModels.SubscriptionDialogViewModel";
		_typeNameTable[66] = "Ciphra.VPN.WinUI.Dialogs.TrafficLimitDialogPage";
		_typeNameTable[67] = "Ciphra.VPN.Common.ViewModels.DialogViewModels.TrafficLimitDialogViewModel";
		_typeNameTable[68] = "Ciphra.VPN.WinUI.Dialogs.TrialDialogPage";
		_typeNameTable[69] = "Ciphra.VPN.Common.ViewModels.DialogViewModels.TrialDialogViewModel";
		_typeNameTable[70] = "Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop";
		_typeNameTable[71] = "H.NotifyIcon.TaskbarIcon";
		_typeNameTable[72] = "Microsoft.UI.Xaml.Media.ImageSource";
		_typeNameTable[73] = "H.NotifyIcon.ContextMenuMode";
		_typeNameTable[74] = "System.Windows.Input.ICommand";
		_typeNameTable[75] = "H.NotifyIcon.Core.TrayIcon";
		_typeNameTable[76] = "H.NotifyIcon.Core.PopupActivationMode";
		_typeNameTable[77] = "Microsoft.UI.Xaml.Controls.Primitives.Popup";
		_typeNameTable[78] = "Microsoft.UI.Xaml.Controls.Primitives.PlacementMode";
		_typeNameTable[79] = "Microsoft.UI.Xaml.Thickness";
		_typeNameTable[80] = "Guid";
		_typeNameTable[81] = "System.Drawing.Icon";
		_typeNameTable[82] = "System.MarshalByRefObject";
		_typeNameTable[83] = "Microsoft.UI.Xaml.Controls.ToolTip";
		_typeNameTable[84] = "Ciphra.VPN.WinUI.MainWindow";
		_typeNameTable[85] = "Microsoft.UI.Xaml.Controls.Primitives.ToggleButton";
		_typeNameTable[86] = "Microsoft.UI.Xaml.Controls.ListViewItem";
		_typeNameTable[87] = "Ciphra.VPN.WinUI.Pages.LocationsPage";
		_typeNameTable[88] = "Ciphra.VPN.Common.ViewModels.LocationsPageViewModel";
		_typeNameTable[89] = "Microsoft.UI.Xaml.Media.RadialGradientBrush";
		_typeNameTable[90] = "Microsoft.UI.Xaml.Media.XamlCompositionBrushBase";
		_typeNameTable[91] = "Windows.Foundation.Collections.IObservableVector`1<Microsoft.UI.Xaml.Media.GradientStop>";
		_typeNameTable[92] = "Microsoft.UI.Xaml.Media.GradientStop";
		_typeNameTable[93] = "Windows.Foundation.Point";
		_typeNameTable[94] = "Microsoft.UI.Composition.CompositionColorSpace";
		_typeNameTable[95] = "Microsoft.UI.Xaml.Media.BrushMappingMode";
		_typeNameTable[96] = "Microsoft.UI.Xaml.Media.GradientSpreadMethod";
		_typeNameTable[97] = "Ciphra.VPN.WinUI.Pages.MainPage";
		_typeNameTable[98] = "Ciphra.VPN.Common.ViewModels.MainPageViewModel";
		_typeNameTable[99] = "Ciphra.VPN.WinUI.Pages.SettingsPage";
		_typeNameTable[100] = "Ciphra.VPN.Common.ViewModels.SettingsPageViewModel";
		_typeNameTable[101] = "Microsoft.UI.Xaml.Controls.Expander";
		_typeNameTable[102] = "Microsoft.UI.Xaml.Controls.ExpandDirection";
		_typeNameTable[103] = "Microsoft.UI.Xaml.Controls.ExpanderTemplateSettings";
		_typeNameTable[104] = "Ciphra.VPN.WinUI.Pages.SplitTunnelPage";
		_typeNameTable[105] = "Ciphra.VPN.Common.ViewModels.SplitTunnelPageViewModel";
		_typeNameTable[106] = "Microsoft.UI.Xaml.Controls.TreeViewNode";
		_typeNameTable[107] = "System.Collections.Generic.IList`1<Microsoft.UI.Xaml.Controls.TreeViewNode>";
		_typeNameTable[108] = "Int32";
		_typeTable = new Type[109];
		_typeTable[0] = typeof(XamlControlsResources);
		_typeTable[1] = typeof(ResourceDictionary);
		_typeTable[2] = typeof(object);
		_typeTable[3] = typeof(bool);
		_typeTable[4] = typeof(MicaBackdrop);
		_typeTable[5] = typeof(SystemBackdrop);
		_typeTable[6] = typeof(MicaKind);
		_typeTable[7] = typeof(Enum);
		_typeTable[8] = typeof(ValueType);
		_typeTable[9] = typeof(ProgressRing);
		_typeTable[10] = typeof(Control);
		_typeTable[11] = typeof(double);
		_typeTable[12] = typeof(ProgressRingTemplateSettings);
		_typeTable[13] = typeof(DependencyObject);
		_typeTable[14] = typeof(AuthWindow);
		_typeTable[15] = typeof(Window);
		_typeTable[16] = typeof(string);
		_typeTable[17] = typeof(PaymentWindow);
		_typeTable[18] = typeof(AppTitleBar);
		_typeTable[19] = typeof(UserControl);
		_typeTable[20] = typeof(NavigationView);
		_typeTable[21] = typeof(ContentControl);
		_typeTable[22] = typeof(NavigationViewPaneDisplayMode);
		_typeTable[23] = typeof(NavigationViewOverflowLabelMode);
		_typeTable[24] = typeof(NavigationViewBackButtonVisible);
		_typeTable[25] = typeof(IList<object>);
		_typeTable[26] = typeof(AutoSuggestBox);
		_typeTable[27] = typeof(UIElement);
		_typeTable[28] = typeof(NavigationViewDisplayMode);
		_typeTable[29] = typeof(DataTemplate);
		_typeTable[30] = typeof(Style);
		_typeTable[31] = typeof(StyleSelector);
		_typeTable[32] = typeof(DataTemplateSelector);
		_typeTable[33] = typeof(NavigationViewSelectionFollowsFocus);
		_typeTable[34] = typeof(NavigationViewShoulderNavigationEnabled);
		_typeTable[35] = typeof(NavigationViewTemplateSettings);
		_typeTable[36] = typeof(NavigationViewItemPresenter);
		_typeTable[37] = typeof(IconElement);
		_typeTable[38] = typeof(InfoBadge);
		_typeTable[39] = typeof(NavigationViewItemPresenterTemplateSettings);
		_typeTable[40] = typeof(NavigationViewItem);
		_typeTable[41] = typeof(NavigationViewItemBase);
		_typeTable[42] = typeof(Locations);
		_typeTable[43] = typeof(VpnIcon);
		_typeTable[44] = typeof(AppShell);
		_typeTable[45] = typeof(AppShellViewModel);
		_typeTable[46] = typeof(ReactiveObject);
		_typeTable[47] = typeof(Location);
		_typeTable[48] = typeof(AuthDialogPage);
		_typeTable[49] = typeof(Page);
		_typeTable[50] = typeof(AuthDialogViewModel);
		_typeTable[51] = typeof(WebView2);
		_typeTable[52] = typeof(FrameworkElement);
		_typeTable[53] = typeof(CoreWebView2);
		_typeTable[54] = typeof(Color);
		_typeTable[55] = typeof(Uri);
		_typeTable[56] = typeof(AuthPage);
		_typeTable[57] = typeof(ElevationPromptDialog);
		_typeTable[58] = typeof(ErrorDialogPage);
		_typeTable[59] = typeof(ErrorDialogViewModel);
		_typeTable[60] = typeof(PaymentPage);
		_typeTable[61] = typeof(PromoDialogPage);
		_typeTable[62] = typeof(RateUsDialogPage);
		_typeTable[63] = typeof(RateUsDialogViewModel);
		_typeTable[64] = typeof(SubscribeDialogPage);
		_typeTable[65] = typeof(SubscriptionDialogViewModel);
		_typeTable[66] = typeof(TrafficLimitDialogPage);
		_typeTable[67] = typeof(TrafficLimitDialogViewModel);
		_typeTable[68] = typeof(TrialDialogPage);
		_typeTable[69] = typeof(TrialDialogViewModel);
		_typeTable[70] = typeof(DesktopAcrylicBackdrop);
		_typeTable[71] = typeof(TaskbarIcon);
		_typeTable[72] = typeof(ImageSource);
		_typeTable[73] = typeof(ContextMenuMode);
		_typeTable[74] = typeof(ICommand);
		_typeTable[75] = typeof(TrayIcon);
		_typeTable[76] = typeof(PopupActivationMode);
		_typeTable[77] = typeof(Popup);
		_typeTable[78] = typeof(PlacementMode);
		_typeTable[79] = typeof(Thickness);
		_typeTable[80] = typeof(Guid);
		_typeTable[81] = typeof(Icon);
		_typeTable[82] = typeof(MarshalByRefObject);
		_typeTable[83] = typeof(ToolTip);
		_typeTable[84] = typeof(MainWindow);
		_typeTable[85] = typeof(ToggleButton);
		_typeTable[86] = typeof(ListViewItem);
		_typeTable[87] = typeof(LocationsPage);
		_typeTable[88] = typeof(LocationsPageViewModel);
		_typeTable[89] = typeof(RadialGradientBrush);
		_typeTable[90] = typeof(XamlCompositionBrushBase);
		_typeTable[91] = typeof(IObservableVector<GradientStop>);
		_typeTable[92] = typeof(GradientStop);
		_typeTable[93] = typeof(Point);
		_typeTable[94] = typeof(CompositionColorSpace);
		_typeTable[95] = typeof(BrushMappingMode);
		_typeTable[96] = typeof(GradientSpreadMethod);
		_typeTable[97] = typeof(MainPage);
		_typeTable[98] = typeof(MainPageViewModel);
		_typeTable[99] = typeof(SettingsPage);
		_typeTable[100] = typeof(SettingsPageViewModel);
		_typeTable[101] = typeof(Expander);
		_typeTable[102] = typeof(ExpandDirection);
		_typeTable[103] = typeof(ExpanderTemplateSettings);
		_typeTable[104] = typeof(SplitTunnelPage);
		_typeTable[105] = typeof(SplitTunnelPageViewModel);
		_typeTable[106] = typeof(TreeViewNode);
		_typeTable[107] = typeof(IList<TreeViewNode>);
		_typeTable[108] = typeof(int);
	}

	private int LookupTypeIndexByName(string typeName)
	{
		if (_typeNameTable == null)
		{
			InitTypeTables();
		}
		for (int i = 0; i < _typeNameTable.Length; i++)
		{
			if (string.CompareOrdinal(_typeNameTable[i], typeName) == 0)
			{
				return i;
			}
		}
		return -1;
	}

	private int LookupTypeIndexByType(Type type)
	{
		if (_typeTable == null)
		{
			InitTypeTables();
		}
		for (int i = 0; i < _typeTable.Length; i++)
		{
			if (type == _typeTable[i])
			{
				return i;
			}
		}
		return -1;
	}

	private object Activate_0_XamlControlsResources()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new XamlControlsResources();
	}

	private object Activate_4_MicaBackdrop()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new MicaBackdrop();
	}

	private object Activate_9_ProgressRing()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new ProgressRing();
	}

	private object Activate_14_AuthWindow()
	{
		return new AuthWindow();
	}

	private object Activate_17_PaymentWindow()
	{
		return new PaymentWindow();
	}

	private object Activate_18_AppTitleBar()
	{
		return new AppTitleBar();
	}

	private object Activate_20_NavigationView()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new NavigationView();
	}

	private object Activate_35_NavigationViewTemplateSettings()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new NavigationViewTemplateSettings();
	}

	private object Activate_36_NavigationViewItemPresenter()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new NavigationViewItemPresenter();
	}

	private object Activate_38_InfoBadge()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new InfoBadge();
	}

	private object Activate_39_NavigationViewItemPresenterTemplateSettings()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new NavigationViewItemPresenterTemplateSettings();
	}

	private object Activate_40_NavigationViewItem()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new NavigationViewItem();
	}

	private object Activate_42_Locations()
	{
		return new Locations();
	}

	private object Activate_43_VpnIcon()
	{
		return new VpnIcon();
	}

	private object Activate_46_ReactiveObject()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new ReactiveObject();
	}

	private object Activate_47_Location()
	{
		return new Location();
	}

	private object Activate_51_WebView2()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new WebView2();
	}

	private object Activate_56_AuthPage()
	{
		return new AuthPage();
	}

	private object Activate_57_ElevationPromptDialog()
	{
		return new ElevationPromptDialog();
	}

	private object Activate_59_ErrorDialogViewModel()
	{
		return new ErrorDialogViewModel();
	}

	private object Activate_60_PaymentPage()
	{
		return new PaymentPage();
	}

	private object Activate_61_PromoDialogPage()
	{
		return new PromoDialogPage();
	}

	private object Activate_70_DesktopAcrylicBackdrop()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new DesktopAcrylicBackdrop();
	}

	private object Activate_71_TaskbarIcon()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new TaskbarIcon();
	}

	private object Activate_75_TrayIcon()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new TrayIcon();
	}

	private object Activate_87_LocationsPage()
	{
		return new LocationsPage();
	}

	private object Activate_89_RadialGradientBrush()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new RadialGradientBrush();
	}

	private object Activate_97_MainPage()
	{
		return new MainPage();
	}

	private object Activate_99_SettingsPage()
	{
		return new SettingsPage();
	}

	private object Activate_101_Expander()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new Expander();
	}

	private object Activate_104_SplitTunnelPage()
	{
		return new SplitTunnelPage();
	}

	private object Activate_106_TreeViewNode()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return (object)new TreeViewNode();
	}

	private void StaticInitializer_0_XamlControlsResources()
	{
		RuntimeHelpers.RunClassConstructor(typeof(XamlControlsResources).TypeHandle);
	}

	private void StaticInitializer_4_MicaBackdrop()
	{
		RuntimeHelpers.RunClassConstructor(typeof(MicaBackdrop).TypeHandle);
	}

	private void StaticInitializer_6_MicaKind()
	{
		RuntimeHelpers.RunClassConstructor(typeof(MicaKind).TypeHandle);
	}

	private void StaticInitializer_7_Enum()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Enum).TypeHandle);
	}

	private void StaticInitializer_8_ValueType()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ValueType).TypeHandle);
	}

	private void StaticInitializer_9_ProgressRing()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ProgressRing).TypeHandle);
	}

	private void StaticInitializer_12_ProgressRingTemplateSettings()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ProgressRingTemplateSettings).TypeHandle);
	}

	private void StaticInitializer_14_AuthWindow()
	{
		RuntimeHelpers.RunClassConstructor(typeof(AuthWindow).TypeHandle);
	}

	private void StaticInitializer_17_PaymentWindow()
	{
		RuntimeHelpers.RunClassConstructor(typeof(PaymentWindow).TypeHandle);
	}

	private void StaticInitializer_18_AppTitleBar()
	{
		RuntimeHelpers.RunClassConstructor(typeof(AppTitleBar).TypeHandle);
	}

	private void StaticInitializer_20_NavigationView()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationView).TypeHandle);
	}

	private void StaticInitializer_22_NavigationViewPaneDisplayMode()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewPaneDisplayMode).TypeHandle);
	}

	private void StaticInitializer_23_NavigationViewOverflowLabelMode()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewOverflowLabelMode).TypeHandle);
	}

	private void StaticInitializer_24_NavigationViewBackButtonVisible()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewBackButtonVisible).TypeHandle);
	}

	private void StaticInitializer_25_IList()
	{
		RuntimeHelpers.RunClassConstructor(typeof(IList<object>).TypeHandle);
	}

	private void StaticInitializer_28_NavigationViewDisplayMode()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewDisplayMode).TypeHandle);
	}

	private void StaticInitializer_33_NavigationViewSelectionFollowsFocus()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewSelectionFollowsFocus).TypeHandle);
	}

	private void StaticInitializer_34_NavigationViewShoulderNavigationEnabled()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewShoulderNavigationEnabled).TypeHandle);
	}

	private void StaticInitializer_35_NavigationViewTemplateSettings()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewTemplateSettings).TypeHandle);
	}

	private void StaticInitializer_36_NavigationViewItemPresenter()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewItemPresenter).TypeHandle);
	}

	private void StaticInitializer_38_InfoBadge()
	{
		RuntimeHelpers.RunClassConstructor(typeof(InfoBadge).TypeHandle);
	}

	private void StaticInitializer_39_NavigationViewItemPresenterTemplateSettings()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewItemPresenterTemplateSettings).TypeHandle);
	}

	private void StaticInitializer_40_NavigationViewItem()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewItem).TypeHandle);
	}

	private void StaticInitializer_41_NavigationViewItemBase()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewItemBase).TypeHandle);
	}

	private void StaticInitializer_42_Locations()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Locations).TypeHandle);
	}

	private void StaticInitializer_43_VpnIcon()
	{
		RuntimeHelpers.RunClassConstructor(typeof(VpnIcon).TypeHandle);
	}

	private void StaticInitializer_44_AppShell()
	{
		RuntimeHelpers.RunClassConstructor(typeof(AppShell).TypeHandle);
	}

	private void StaticInitializer_45_AppShellViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(AppShellViewModel).TypeHandle);
	}

	private void StaticInitializer_46_ReactiveObject()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ReactiveObject).TypeHandle);
	}

	private void StaticInitializer_47_Location()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Location).TypeHandle);
	}

	private void StaticInitializer_48_AuthDialogPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(AuthDialogPage).TypeHandle);
	}

	private void StaticInitializer_50_AuthDialogViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(AuthDialogViewModel).TypeHandle);
	}

	private void StaticInitializer_51_WebView2()
	{
		RuntimeHelpers.RunClassConstructor(typeof(WebView2).TypeHandle);
	}

	private void StaticInitializer_53_CoreWebView2()
	{
		RuntimeHelpers.RunClassConstructor(typeof(CoreWebView2).TypeHandle);
	}

	private void StaticInitializer_54_Color()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Color).TypeHandle);
	}

	private void StaticInitializer_55_Uri()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Uri).TypeHandle);
	}

	private void StaticInitializer_56_AuthPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(AuthPage).TypeHandle);
	}

	private void StaticInitializer_57_ElevationPromptDialog()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ElevationPromptDialog).TypeHandle);
	}

	private void StaticInitializer_58_ErrorDialogPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ErrorDialogPage).TypeHandle);
	}

	private void StaticInitializer_59_ErrorDialogViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ErrorDialogViewModel).TypeHandle);
	}

	private void StaticInitializer_60_PaymentPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(PaymentPage).TypeHandle);
	}

	private void StaticInitializer_61_PromoDialogPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(PromoDialogPage).TypeHandle);
	}

	private void StaticInitializer_62_RateUsDialogPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(RateUsDialogPage).TypeHandle);
	}

	private void StaticInitializer_63_RateUsDialogViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(RateUsDialogViewModel).TypeHandle);
	}

	private void StaticInitializer_64_SubscribeDialogPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(SubscribeDialogPage).TypeHandle);
	}

	private void StaticInitializer_65_SubscriptionDialogViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(SubscriptionDialogViewModel).TypeHandle);
	}

	private void StaticInitializer_66_TrafficLimitDialogPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(TrafficLimitDialogPage).TypeHandle);
	}

	private void StaticInitializer_67_TrafficLimitDialogViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(TrafficLimitDialogViewModel).TypeHandle);
	}

	private void StaticInitializer_68_TrialDialogPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(TrialDialogPage).TypeHandle);
	}

	private void StaticInitializer_69_TrialDialogViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(TrialDialogViewModel).TypeHandle);
	}

	private void StaticInitializer_70_DesktopAcrylicBackdrop()
	{
		RuntimeHelpers.RunClassConstructor(typeof(DesktopAcrylicBackdrop).TypeHandle);
	}

	private void StaticInitializer_71_TaskbarIcon()
	{
		RuntimeHelpers.RunClassConstructor(typeof(TaskbarIcon).TypeHandle);
	}

	private void StaticInitializer_73_ContextMenuMode()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ContextMenuMode).TypeHandle);
	}

	private void StaticInitializer_74_ICommand()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ICommand).TypeHandle);
	}

	private void StaticInitializer_75_TrayIcon()
	{
		RuntimeHelpers.RunClassConstructor(typeof(TrayIcon).TypeHandle);
	}

	private void StaticInitializer_76_PopupActivationMode()
	{
		RuntimeHelpers.RunClassConstructor(typeof(PopupActivationMode).TypeHandle);
	}

	private void StaticInitializer_79_Thickness()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Thickness).TypeHandle);
	}

	private void StaticInitializer_80_Guid()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Guid).TypeHandle);
	}

	private void StaticInitializer_81_Icon()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Icon).TypeHandle);
	}

	private void StaticInitializer_82_MarshalByRefObject()
	{
		RuntimeHelpers.RunClassConstructor(typeof(MarshalByRefObject).TypeHandle);
	}

	private void StaticInitializer_84_MainWindow()
	{
		RuntimeHelpers.RunClassConstructor(typeof(MainWindow).TypeHandle);
	}

	private void StaticInitializer_87_LocationsPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(LocationsPage).TypeHandle);
	}

	private void StaticInitializer_88_LocationsPageViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(LocationsPageViewModel).TypeHandle);
	}

	private void StaticInitializer_89_RadialGradientBrush()
	{
		RuntimeHelpers.RunClassConstructor(typeof(RadialGradientBrush).TypeHandle);
	}

	private void StaticInitializer_91_IObservableVector()
	{
		RuntimeHelpers.RunClassConstructor(typeof(IObservableVector<GradientStop>).TypeHandle);
	}

	private void StaticInitializer_94_CompositionColorSpace()
	{
		RuntimeHelpers.RunClassConstructor(typeof(CompositionColorSpace).TypeHandle);
	}

	private void StaticInitializer_97_MainPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(MainPage).TypeHandle);
	}

	private void StaticInitializer_98_MainPageViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(MainPageViewModel).TypeHandle);
	}

	private void StaticInitializer_99_SettingsPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(SettingsPage).TypeHandle);
	}

	private void StaticInitializer_100_SettingsPageViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(SettingsPageViewModel).TypeHandle);
	}

	private void StaticInitializer_101_Expander()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Expander).TypeHandle);
	}

	private void StaticInitializer_102_ExpandDirection()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ExpandDirection).TypeHandle);
	}

	private void StaticInitializer_103_ExpanderTemplateSettings()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ExpanderTemplateSettings).TypeHandle);
	}

	private void StaticInitializer_104_SplitTunnelPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(SplitTunnelPage).TypeHandle);
	}

	private void StaticInitializer_105_SplitTunnelPageViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(SplitTunnelPageViewModel).TypeHandle);
	}

	private void StaticInitializer_106_TreeViewNode()
	{
		RuntimeHelpers.RunClassConstructor(typeof(TreeViewNode).TypeHandle);
	}

	private void StaticInitializer_107_IList()
	{
		RuntimeHelpers.RunClassConstructor(typeof(IList<TreeViewNode>).TypeHandle);
	}

	private void MapAdd_0_XamlControlsResources(object instance, object key, object item)
	{
		IDictionary<object, object> dictionary = (IDictionary<object, object>)instance;
		dictionary.Add(key, item);
	}

	private void VectorAdd_25_IList(object instance, object item)
	{
		ICollection<object> collection = (ICollection<object>)instance;
		collection.Add(item);
	}

	private void VectorAdd_91_IObservableVector(object instance, object item)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected O, but got Unknown
		ICollection<GradientStop> collection = (ICollection<GradientStop>)instance;
		GradientStop item2 = (GradientStop)item;
		collection.Add(item2);
	}

	private void VectorAdd_107_IList(object instance, object item)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected O, but got Unknown
		ICollection<TreeViewNode> collection = (ICollection<TreeViewNode>)instance;
		TreeViewNode item2 = (TreeViewNode)item;
		collection.Add(item2);
	}

	private IXamlType CreateXamlType(int typeIndex)
	{
		XamlSystemBaseType result = null;
		string fullName = _typeNameTable[typeIndex];
		Type type = _typeTable[typeIndex];
		switch (typeIndex)
		{
		case 0:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.ResourceDictionary"));
			xamlUserType.Activator = Activate_0_XamlControlsResources;
			xamlUserType.StaticInitializer = StaticInitializer_0_XamlControlsResources;
			xamlUserType.DictionaryAdd = MapAdd_0_XamlControlsResources;
			xamlUserType.AddMemberName("UseCompactResources");
			result = xamlUserType;
			break;
		}
		case 1:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 2:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 3:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 4:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Media.SystemBackdrop"));
			xamlUserType.Activator = Activate_4_MicaBackdrop;
			xamlUserType.StaticInitializer = StaticInitializer_4_MicaBackdrop;
			xamlUserType.AddMemberName("Kind");
			result = xamlUserType;
			break;
		}
		case 5:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 6:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType.StaticInitializer = StaticInitializer_6_MicaKind;
			xamlUserType.AddEnumValue("Base", (object)(MicaKind)0);
			xamlUserType.AddEnumValue("BaseAlt", (object)(MicaKind)1);
			result = xamlUserType;
			break;
		}
		case 7:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.ValueType"));
			xamlUserType.StaticInitializer = StaticInitializer_7_Enum;
			result = xamlUserType;
			break;
		}
		case 8:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"));
			xamlUserType.StaticInitializer = StaticInitializer_8_ValueType;
			result = xamlUserType;
			break;
		}
		case 9:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Control"));
			xamlUserType.Activator = Activate_9_ProgressRing;
			xamlUserType.StaticInitializer = StaticInitializer_9_ProgressRing;
			xamlUserType.AddMemberName("IsActive");
			xamlUserType.AddMemberName("IsIndeterminate");
			xamlUserType.AddMemberName("Maximum");
			xamlUserType.AddMemberName("Minimum");
			xamlUserType.AddMemberName("TemplateSettings");
			xamlUserType.AddMemberName("Value");
			result = xamlUserType;
			break;
		}
		case 10:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 11:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 12:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.DependencyObject"));
			xamlUserType.StaticInitializer = StaticInitializer_12_ProgressRingTemplateSettings;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 13:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 14:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Window"));
			xamlUserType.Activator = Activate_14_AuthWindow;
			xamlUserType.StaticInitializer = StaticInitializer_14_AuthWindow;
			xamlUserType.AddMemberName("Url");
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 15:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 16:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 17:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Window"));
			xamlUserType.Activator = Activate_17_PaymentWindow;
			xamlUserType.StaticInitializer = StaticInitializer_17_PaymentWindow;
			xamlUserType.AddMemberName("Url");
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 18:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.UserControl"));
			xamlUserType.Activator = Activate_18_AppTitleBar;
			xamlUserType.StaticInitializer = StaticInitializer_18_AppTitleBar;
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 19:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 20:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ContentControl"));
			xamlUserType.Activator = Activate_20_NavigationView;
			xamlUserType.StaticInitializer = StaticInitializer_20_NavigationView;
			xamlUserType.AddMemberName("PaneDisplayMode");
			xamlUserType.AddMemberName("OverflowLabelMode");
			xamlUserType.AddMemberName("IsTitleBarAutoPaddingEnabled");
			xamlUserType.AddMemberName("IsBackButtonVisible");
			xamlUserType.AddMemberName("IsBackEnabled");
			xamlUserType.AddMemberName("IsSettingsVisible");
			xamlUserType.AddMemberName("MenuItems");
			xamlUserType.AddMemberName("FooterMenuItems");
			xamlUserType.AddMemberName("AlwaysShowHeader");
			xamlUserType.AddMemberName("AutoSuggestBox");
			xamlUserType.AddMemberName("CompactModeThresholdWidth");
			xamlUserType.AddMemberName("CompactPaneLength");
			xamlUserType.AddMemberName("ContentOverlay");
			xamlUserType.AddMemberName("DisplayMode");
			xamlUserType.AddMemberName("ExpandedModeThresholdWidth");
			xamlUserType.AddMemberName("FooterMenuItemsSource");
			xamlUserType.AddMemberName("Header");
			xamlUserType.AddMemberName("HeaderTemplate");
			xamlUserType.AddMemberName("IsPaneOpen");
			xamlUserType.AddMemberName("IsPaneToggleButtonVisible");
			xamlUserType.AddMemberName("IsPaneVisible");
			xamlUserType.AddMemberName("MenuItemContainerStyle");
			xamlUserType.AddMemberName("MenuItemContainerStyleSelector");
			xamlUserType.AddMemberName("MenuItemTemplate");
			xamlUserType.AddMemberName("MenuItemTemplateSelector");
			xamlUserType.AddMemberName("MenuItemsSource");
			xamlUserType.AddMemberName("OpenPaneLength");
			xamlUserType.AddMemberName("PaneCustomContent");
			xamlUserType.AddMemberName("PaneFooter");
			xamlUserType.AddMemberName("PaneHeader");
			xamlUserType.AddMemberName("PaneTitle");
			xamlUserType.AddMemberName("PaneToggleButtonStyle");
			xamlUserType.AddMemberName("SelectedItem");
			xamlUserType.AddMemberName("SelectionFollowsFocus");
			xamlUserType.AddMemberName("SettingsItem");
			xamlUserType.AddMemberName("ShoulderNavigationEnabled");
			xamlUserType.AddMemberName("TemplateSettings");
			result = xamlUserType;
			break;
		}
		case 21:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 22:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType.StaticInitializer = StaticInitializer_22_NavigationViewPaneDisplayMode;
			xamlUserType.AddEnumValue("Auto", (object)(NavigationViewPaneDisplayMode)0);
			xamlUserType.AddEnumValue("Left", (object)(NavigationViewPaneDisplayMode)1);
			xamlUserType.AddEnumValue("Top", (object)(NavigationViewPaneDisplayMode)2);
			xamlUserType.AddEnumValue("LeftCompact", (object)(NavigationViewPaneDisplayMode)3);
			xamlUserType.AddEnumValue("LeftMinimal", (object)(NavigationViewPaneDisplayMode)4);
			result = xamlUserType;
			break;
		}
		case 23:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType.StaticInitializer = StaticInitializer_23_NavigationViewOverflowLabelMode;
			xamlUserType.AddEnumValue("MoreLabel", (object)(NavigationViewOverflowLabelMode)0);
			xamlUserType.AddEnumValue("NoLabel", (object)(NavigationViewOverflowLabelMode)1);
			result = xamlUserType;
			break;
		}
		case 24:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType.StaticInitializer = StaticInitializer_24_NavigationViewBackButtonVisible;
			xamlUserType.AddEnumValue("Collapsed", (object)(NavigationViewBackButtonVisible)0);
			xamlUserType.AddEnumValue("Visible", (object)(NavigationViewBackButtonVisible)1);
			xamlUserType.AddEnumValue("Auto", (object)(NavigationViewBackButtonVisible)2);
			result = xamlUserType;
			break;
		}
		case 25:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, null);
			xamlUserType.StaticInitializer = StaticInitializer_25_IList;
			xamlUserType.CollectionAdd = VectorAdd_25_IList;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 26:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 27:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 28:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType.StaticInitializer = StaticInitializer_28_NavigationViewDisplayMode;
			xamlUserType.AddEnumValue("Minimal", (object)(NavigationViewDisplayMode)0);
			xamlUserType.AddEnumValue("Compact", (object)(NavigationViewDisplayMode)1);
			xamlUserType.AddEnumValue("Expanded", (object)(NavigationViewDisplayMode)2);
			result = xamlUserType;
			break;
		}
		case 29:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 30:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 31:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 32:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 33:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType.StaticInitializer = StaticInitializer_33_NavigationViewSelectionFollowsFocus;
			xamlUserType.AddEnumValue("Disabled", (object)(NavigationViewSelectionFollowsFocus)0);
			xamlUserType.AddEnumValue("Enabled", (object)(NavigationViewSelectionFollowsFocus)1);
			result = xamlUserType;
			break;
		}
		case 34:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType.StaticInitializer = StaticInitializer_34_NavigationViewShoulderNavigationEnabled;
			xamlUserType.AddEnumValue("WhenSelectionFollowsFocus", (object)(NavigationViewShoulderNavigationEnabled)0);
			xamlUserType.AddEnumValue("Always", (object)(NavigationViewShoulderNavigationEnabled)1);
			xamlUserType.AddEnumValue("Never", (object)(NavigationViewShoulderNavigationEnabled)2);
			result = xamlUserType;
			break;
		}
		case 35:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.DependencyObject"));
			xamlUserType.StaticInitializer = StaticInitializer_35_NavigationViewTemplateSettings;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 36:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ContentControl"));
			xamlUserType.Activator = Activate_36_NavigationViewItemPresenter;
			xamlUserType.StaticInitializer = StaticInitializer_36_NavigationViewItemPresenter;
			xamlUserType.AddMemberName("Icon");
			xamlUserType.AddMemberName("InfoBadge");
			xamlUserType.AddMemberName("TemplateSettings");
			result = xamlUserType;
			break;
		}
		case 37:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 38:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Control"));
			xamlUserType.StaticInitializer = StaticInitializer_38_InfoBadge;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 39:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.DependencyObject"));
			xamlUserType.StaticInitializer = StaticInitializer_39_NavigationViewItemPresenterTemplateSettings;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 40:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItemBase"));
			xamlUserType.Activator = Activate_40_NavigationViewItem;
			xamlUserType.StaticInitializer = StaticInitializer_40_NavigationViewItem;
			xamlUserType.AddMemberName("Icon");
			xamlUserType.AddMemberName("CompactPaneLength");
			xamlUserType.AddMemberName("HasUnrealizedChildren");
			xamlUserType.AddMemberName("InfoBadge");
			xamlUserType.AddMemberName("IsChildSelected");
			xamlUserType.AddMemberName("IsExpanded");
			xamlUserType.AddMemberName("MenuItems");
			xamlUserType.AddMemberName("MenuItemsSource");
			xamlUserType.AddMemberName("SelectsOnInvoked");
			result = xamlUserType;
			break;
		}
		case 41:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ContentControl"));
			xamlUserType.StaticInitializer = StaticInitializer_41_NavigationViewItemBase;
			xamlUserType.AddMemberName("IsSelected");
			result = xamlUserType;
			break;
		}
		case 42:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.UserControl"));
			xamlUserType.Activator = Activate_42_Locations;
			xamlUserType.StaticInitializer = StaticInitializer_42_Locations;
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 43:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.UserControl"));
			xamlUserType.Activator = Activate_43_VpnIcon;
			xamlUserType.StaticInitializer = StaticInitializer_43_VpnIcon;
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 44:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.UserControl"));
			xamlUserType.StaticInitializer = StaticInitializer_44_AppShell;
			xamlUserType.AddMemberName("ViewModel");
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 45:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("ReactiveUI.ReactiveObject"));
			xamlUserType.StaticInitializer = StaticInitializer_45_AppShellViewModel;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 46:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"));
			xamlUserType.Activator = Activate_46_ReactiveObject;
			xamlUserType.StaticInitializer = StaticInitializer_46_ReactiveObject;
			result = xamlUserType;
			break;
		}
		case 47:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.UserControl"));
			xamlUserType.Activator = Activate_47_Location;
			xamlUserType.StaticInitializer = StaticInitializer_47_Location;
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 48:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType.StaticInitializer = StaticInitializer_48_AuthDialogPage;
			xamlUserType.AddMemberName("ViewModel");
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 49:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 50:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("ReactiveUI.ReactiveObject"));
			xamlUserType.StaticInitializer = StaticInitializer_50_AuthDialogViewModel;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 51:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.FrameworkElement"));
			xamlUserType.Activator = Activate_51_WebView2;
			xamlUserType.StaticInitializer = StaticInitializer_51_WebView2;
			xamlUserType.AddMemberName("CanGoBack");
			xamlUserType.AddMemberName("CanGoForward");
			xamlUserType.AddMemberName("CoreWebView2");
			xamlUserType.AddMemberName("DefaultBackgroundColor");
			xamlUserType.AddMemberName("Source");
			result = xamlUserType;
			break;
		}
		case 52:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 53:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"));
			xamlUserType.StaticInitializer = StaticInitializer_53_CoreWebView2;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 54:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.ValueType"));
			xamlUserType.StaticInitializer = StaticInitializer_54_Color;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 55:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"));
			xamlUserType.StaticInitializer = StaticInitializer_55_Uri;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 56:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType.Activator = Activate_56_AuthPage;
			xamlUserType.StaticInitializer = StaticInitializer_56_AuthPage;
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 57:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType.Activator = Activate_57_ElevationPromptDialog;
			xamlUserType.StaticInitializer = StaticInitializer_57_ElevationPromptDialog;
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 58:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType.StaticInitializer = StaticInitializer_58_ErrorDialogPage;
			xamlUserType.AddMemberName("ViewModel");
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 59:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("ReactiveUI.ReactiveObject"));
			xamlUserType.StaticInitializer = StaticInitializer_59_ErrorDialogViewModel;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 60:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType.Activator = Activate_60_PaymentPage;
			xamlUserType.StaticInitializer = StaticInitializer_60_PaymentPage;
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 61:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType.Activator = Activate_61_PromoDialogPage;
			xamlUserType.StaticInitializer = StaticInitializer_61_PromoDialogPage;
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 62:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType.StaticInitializer = StaticInitializer_62_RateUsDialogPage;
			xamlUserType.AddMemberName("ViewModel");
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 63:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"));
			xamlUserType.StaticInitializer = StaticInitializer_63_RateUsDialogViewModel;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 64:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType.StaticInitializer = StaticInitializer_64_SubscribeDialogPage;
			xamlUserType.AddMemberName("ViewModel");
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 65:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"));
			xamlUserType.StaticInitializer = StaticInitializer_65_SubscriptionDialogViewModel;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 66:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType.StaticInitializer = StaticInitializer_66_TrafficLimitDialogPage;
			xamlUserType.AddMemberName("ViewModel");
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 67:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("ReactiveUI.ReactiveObject"));
			xamlUserType.StaticInitializer = StaticInitializer_67_TrafficLimitDialogViewModel;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 68:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType.StaticInitializer = StaticInitializer_68_TrialDialogPage;
			xamlUserType.AddMemberName("ViewModel");
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 69:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("ReactiveUI.ReactiveObject"));
			xamlUserType.StaticInitializer = StaticInitializer_69_TrialDialogViewModel;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 70:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Media.SystemBackdrop"));
			xamlUserType.Activator = Activate_70_DesktopAcrylicBackdrop;
			xamlUserType.StaticInitializer = StaticInitializer_70_DesktopAcrylicBackdrop;
			result = xamlUserType;
			break;
		}
		case 71:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.FrameworkElement"));
			xamlUserType.Activator = Activate_71_TaskbarIcon;
			xamlUserType.StaticInitializer = StaticInitializer_71_TaskbarIcon;
			xamlUserType.AddMemberName("IconSource");
			xamlUserType.AddMemberName("ContextMenuMode");
			xamlUserType.AddMemberName("ToolTipText");
			xamlUserType.AddMemberName("CustomName");
			xamlUserType.AddMemberName("DoubleClickCommand");
			xamlUserType.AddMemberName("NoLeftClickDelay");
			xamlUserType.AddMemberName("LeftClickCommand");
			xamlUserType.AddMemberName("TrayIcon");
			xamlUserType.AddMemberName("IsCreated");
			xamlUserType.AddMemberName("IsDisposed");
			xamlUserType.AddMemberName("SupportsCustomToolTips");
			xamlUserType.AddMemberName("DoubleClickCommandParameter");
			xamlUserType.AddMemberName("LeftClickCommandParameter");
			xamlUserType.AddMemberName("RightClickCommand");
			xamlUserType.AddMemberName("RightClickCommandParameter");
			xamlUserType.AddMemberName("MiddleClickCommand");
			xamlUserType.AddMemberName("MiddleClickCommandParameter");
			xamlUserType.AddMemberName("MenuActivation");
			xamlUserType.AddMemberName("PopupActivation");
			xamlUserType.AddMemberName("TrayPopup");
			xamlUserType.AddMemberName("TrayPopupResolved");
			xamlUserType.AddMemberName("PopupPlacement");
			xamlUserType.AddMemberName("PopupOffset");
			xamlUserType.AddMemberName("Id");
			xamlUserType.AddMemberName("Icon");
			xamlUserType.AddMemberName("TrayToolTip");
			xamlUserType.AddMemberName("TrayToolTipResolved");
			xamlUserType.AddMemberName("ParentTaskbarIcon");
			result = xamlUserType;
			break;
		}
		case 72:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 73:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType.StaticInitializer = StaticInitializer_73_ContextMenuMode;
			xamlUserType.AddEnumValue("PopupMenu", (object)(ContextMenuMode)0);
			xamlUserType.AddEnumValue("SecondWindow", (object)(ContextMenuMode)1);
			xamlUserType.AddEnumValue("ActiveWindow", (object)(ContextMenuMode)2);
			result = xamlUserType;
			break;
		}
		case 74:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, null);
			xamlUserType.StaticInitializer = StaticInitializer_74_ICommand;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 75:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"));
			xamlUserType.StaticInitializer = StaticInitializer_75_TrayIcon;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 76:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType.StaticInitializer = StaticInitializer_76_PopupActivationMode;
			xamlUserType.AddEnumValue("LeftClick", (object)(PopupActivationMode)0);
			xamlUserType.AddEnumValue("RightClick", (object)(PopupActivationMode)1);
			xamlUserType.AddEnumValue("DoubleClick", (object)(PopupActivationMode)2);
			xamlUserType.AddEnumValue("LeftOrRightClick", (object)(PopupActivationMode)3);
			xamlUserType.AddEnumValue("LeftOrDoubleClick", (object)(PopupActivationMode)4);
			xamlUserType.AddEnumValue("MiddleClick", (object)(PopupActivationMode)5);
			xamlUserType.AddEnumValue("All", (object)(PopupActivationMode)6);
			xamlUserType.AddEnumValue("None", (object)(PopupActivationMode)7);
			result = xamlUserType;
			break;
		}
		case 77:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 78:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 79:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.ValueType"));
			xamlUserType.StaticInitializer = StaticInitializer_79_Thickness;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 80:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.ValueType"));
			xamlUserType.StaticInitializer = StaticInitializer_80_Guid;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 81:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.MarshalByRefObject"));
			xamlUserType.StaticInitializer = StaticInitializer_81_Icon;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 82:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"));
			xamlUserType.StaticInitializer = StaticInitializer_82_MarshalByRefObject;
			result = xamlUserType;
			break;
		}
		case 83:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 84:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Window"));
			xamlUserType.StaticInitializer = StaticInitializer_84_MainWindow;
			xamlUserType.AddMemberName("Restore");
			xamlUserType.AddMemberName("Exit");
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 85:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 86:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 87:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType.Activator = Activate_87_LocationsPage;
			xamlUserType.StaticInitializer = StaticInitializer_87_LocationsPage;
			xamlUserType.AddMemberName("ViewModel");
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 88:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("ReactiveUI.ReactiveObject"));
			xamlUserType.StaticInitializer = StaticInitializer_88_LocationsPageViewModel;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 89:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Media.XamlCompositionBrushBase"));
			xamlUserType.Activator = Activate_89_RadialGradientBrush;
			xamlUserType.StaticInitializer = StaticInitializer_89_RadialGradientBrush;
			xamlUserType.SetContentPropertyName("Microsoft.UI.Xaml.Media.RadialGradientBrush.GradientStops");
			xamlUserType.AddMemberName("GradientStops");
			xamlUserType.AddMemberName("Center");
			xamlUserType.AddMemberName("RadiusX");
			xamlUserType.AddMemberName("RadiusY");
			xamlUserType.AddMemberName("GradientOrigin");
			xamlUserType.AddMemberName("InterpolationSpace");
			xamlUserType.AddMemberName("MappingMode");
			xamlUserType.AddMemberName("SpreadMethod");
			result = xamlUserType;
			break;
		}
		case 90:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 91:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, null);
			xamlUserType.StaticInitializer = StaticInitializer_91_IObservableVector;
			xamlUserType.CollectionAdd = VectorAdd_91_IObservableVector;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 92:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 93:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 94:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType.StaticInitializer = StaticInitializer_94_CompositionColorSpace;
			xamlUserType.AddEnumValue("Auto", (object)(CompositionColorSpace)0);
			xamlUserType.AddEnumValue("Hsl", (object)(CompositionColorSpace)1);
			xamlUserType.AddEnumValue("Rgb", (object)(CompositionColorSpace)2);
			xamlUserType.AddEnumValue("HslLinear", (object)(CompositionColorSpace)3);
			xamlUserType.AddEnumValue("RgbLinear", (object)(CompositionColorSpace)4);
			result = xamlUserType;
			break;
		}
		case 95:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 96:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 97:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType.Activator = Activate_97_MainPage;
			xamlUserType.StaticInitializer = StaticInitializer_97_MainPage;
			xamlUserType.AddMemberName("ViewModel");
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 98:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("ReactiveUI.ReactiveObject"));
			xamlUserType.StaticInitializer = StaticInitializer_98_MainPageViewModel;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 99:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType.Activator = Activate_99_SettingsPage;
			xamlUserType.StaticInitializer = StaticInitializer_99_SettingsPage;
			xamlUserType.AddMemberName("ViewModel");
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 100:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("ReactiveUI.ReactiveObject"));
			xamlUserType.StaticInitializer = StaticInitializer_100_SettingsPageViewModel;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 101:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ContentControl"));
			xamlUserType.Activator = Activate_101_Expander;
			xamlUserType.StaticInitializer = StaticInitializer_101_Expander;
			xamlUserType.AddMemberName("Header");
			xamlUserType.AddMemberName("IsExpanded");
			xamlUserType.AddMemberName("ExpandDirection");
			xamlUserType.AddMemberName("HeaderTemplate");
			xamlUserType.AddMemberName("HeaderTemplateSelector");
			xamlUserType.AddMemberName("TemplateSettings");
			result = xamlUserType;
			break;
		}
		case 102:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType.StaticInitializer = StaticInitializer_102_ExpandDirection;
			xamlUserType.AddEnumValue("Down", (object)(ExpandDirection)0);
			xamlUserType.AddEnumValue("Up", (object)(ExpandDirection)1);
			result = xamlUserType;
			break;
		}
		case 103:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.DependencyObject"));
			xamlUserType.StaticInitializer = StaticInitializer_103_ExpanderTemplateSettings;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 104:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType.Activator = Activate_104_SplitTunnelPage;
			xamlUserType.StaticInitializer = StaticInitializer_104_SplitTunnelPage;
			xamlUserType.AddMemberName("ViewModel");
			xamlUserType.SetIsLocalType();
			result = xamlUserType;
			break;
		}
		case 105:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("ReactiveUI.ReactiveObject"));
			xamlUserType.StaticInitializer = StaticInitializer_105_SplitTunnelPageViewModel;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 106:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.DependencyObject"));
			xamlUserType.Activator = Activate_106_TreeViewNode;
			xamlUserType.StaticInitializer = StaticInitializer_106_TreeViewNode;
			xamlUserType.AddMemberName("Children");
			xamlUserType.AddMemberName("Content");
			xamlUserType.AddMemberName("Depth");
			xamlUserType.AddMemberName("HasChildren");
			xamlUserType.AddMemberName("HasUnrealizedChildren");
			xamlUserType.AddMemberName("IsExpanded");
			xamlUserType.AddMemberName("Parent");
			xamlUserType.SetIsBindable();
			result = xamlUserType;
			break;
		}
		case 107:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, null);
			xamlUserType.StaticInitializer = StaticInitializer_107_IList;
			xamlUserType.CollectionAdd = VectorAdd_107_IList;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 108:
			result = new XamlSystemBaseType(fullName, type);
			break;
		}
		return (IXamlType)(object)result;
	}

	private IXamlType CheckOtherMetadataProvidersForName(string typeName)
	{
		IXamlType val = null;
		IXamlType result = null;
		foreach (IXamlMetadataProvider otherProvider in OtherProviders)
		{
			val = otherProvider.GetXamlType(typeName);
			if (val != null)
			{
				if (val.IsConstructible)
				{
					return val;
				}
				result = val;
			}
		}
		return result;
	}

	private IXamlType CheckOtherMetadataProvidersForType(Type type)
	{
		IXamlType val = null;
		IXamlType result = null;
		foreach (IXamlMetadataProvider otherProvider in OtherProviders)
		{
			val = otherProvider.GetXamlType(type);
			if (val != null)
			{
				if (val.IsConstructible)
				{
					return val;
				}
				result = val;
			}
		}
		return result;
	}

	private object get_0_XamlControlsResources_UseCompactResources(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		XamlControlsResources val = (XamlControlsResources)instance;
		return val.UseCompactResources;
	}

	private void set_0_XamlControlsResources_UseCompactResources(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		XamlControlsResources val = (XamlControlsResources)instance;
		val.UseCompactResources = (bool)Value;
	}

	private object get_1_MicaBackdrop_Kind(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		MicaBackdrop val = (MicaBackdrop)instance;
		return val.Kind;
	}

	private void set_1_MicaBackdrop_Kind(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		MicaBackdrop val = (MicaBackdrop)instance;
		val.Kind = (MicaKind)Value;
	}

	private object get_2_ProgressRing_IsActive(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		ProgressRing val = (ProgressRing)instance;
		return val.IsActive;
	}

	private void set_2_ProgressRing_IsActive(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		ProgressRing val = (ProgressRing)instance;
		val.IsActive = (bool)Value;
	}

	private object get_3_ProgressRing_IsIndeterminate(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		ProgressRing val = (ProgressRing)instance;
		return val.IsIndeterminate;
	}

	private void set_3_ProgressRing_IsIndeterminate(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		ProgressRing val = (ProgressRing)instance;
		val.IsIndeterminate = (bool)Value;
	}

	private object get_4_ProgressRing_Maximum(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		ProgressRing val = (ProgressRing)instance;
		return val.Maximum;
	}

	private void set_4_ProgressRing_Maximum(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		ProgressRing val = (ProgressRing)instance;
		val.Maximum = (double)Value;
	}

	private object get_5_ProgressRing_Minimum(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		ProgressRing val = (ProgressRing)instance;
		return val.Minimum;
	}

	private void set_5_ProgressRing_Minimum(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		ProgressRing val = (ProgressRing)instance;
		val.Minimum = (double)Value;
	}

	private object get_6_ProgressRing_TemplateSettings(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		ProgressRing val = (ProgressRing)instance;
		return val.TemplateSettings;
	}

	private object get_7_ProgressRing_Value(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		ProgressRing val = (ProgressRing)instance;
		return val.Value;
	}

	private void set_7_ProgressRing_Value(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		ProgressRing val = (ProgressRing)instance;
		val.Value = (double)Value;
	}

	private object get_8_AuthWindow_Url(object instance)
	{
		AuthWindow authWindow = (AuthWindow)instance;
		return authWindow.Url;
	}

	private void set_8_AuthWindow_Url(object instance, object Value)
	{
		AuthWindow authWindow = (AuthWindow)instance;
		authWindow.Url = (string)Value;
	}

	private object get_9_PaymentWindow_Url(object instance)
	{
		PaymentWindow paymentWindow = (PaymentWindow)instance;
		return paymentWindow.Url;
	}

	private void set_9_PaymentWindow_Url(object instance, object Value)
	{
		PaymentWindow paymentWindow = (PaymentWindow)instance;
		paymentWindow.Url = (string)Value;
	}

	private object get_10_NavigationView_PaneDisplayMode(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		NavigationView val = (NavigationView)instance;
		return val.PaneDisplayMode;
	}

	private void set_10_NavigationView_PaneDisplayMode(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		NavigationView val = (NavigationView)instance;
		val.PaneDisplayMode = (NavigationViewPaneDisplayMode)Value;
	}

	private object get_11_NavigationView_OverflowLabelMode(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		NavigationView val = (NavigationView)instance;
		return val.OverflowLabelMode;
	}

	private void set_11_NavigationView_OverflowLabelMode(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		NavigationView val = (NavigationView)instance;
		val.OverflowLabelMode = (NavigationViewOverflowLabelMode)Value;
	}

	private object get_12_NavigationView_IsTitleBarAutoPaddingEnabled(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.IsTitleBarAutoPaddingEnabled;
	}

	private void set_12_NavigationView_IsTitleBarAutoPaddingEnabled(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.IsTitleBarAutoPaddingEnabled = (bool)Value;
	}

	private object get_13_NavigationView_IsBackButtonVisible(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		NavigationView val = (NavigationView)instance;
		return val.IsBackButtonVisible;
	}

	private void set_13_NavigationView_IsBackButtonVisible(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		NavigationView val = (NavigationView)instance;
		val.IsBackButtonVisible = (NavigationViewBackButtonVisible)Value;
	}

	private object get_14_NavigationView_IsBackEnabled(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.IsBackEnabled;
	}

	private void set_14_NavigationView_IsBackEnabled(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.IsBackEnabled = (bool)Value;
	}

	private object get_15_NavigationView_IsSettingsVisible(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.IsSettingsVisible;
	}

	private void set_15_NavigationView_IsSettingsVisible(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.IsSettingsVisible = (bool)Value;
	}

	private object get_16_NavigationView_MenuItems(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.MenuItems;
	}

	private object get_17_NavigationView_FooterMenuItems(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.FooterMenuItems;
	}

	private object get_18_NavigationView_AlwaysShowHeader(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.AlwaysShowHeader;
	}

	private void set_18_NavigationView_AlwaysShowHeader(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.AlwaysShowHeader = (bool)Value;
	}

	private object get_19_NavigationView_AutoSuggestBox(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.AutoSuggestBox;
	}

	private void set_19_NavigationView_AutoSuggestBox(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.AutoSuggestBox = (AutoSuggestBox)Value;
	}

	private object get_20_NavigationView_CompactModeThresholdWidth(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.CompactModeThresholdWidth;
	}

	private void set_20_NavigationView_CompactModeThresholdWidth(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.CompactModeThresholdWidth = (double)Value;
	}

	private object get_21_NavigationView_CompactPaneLength(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.CompactPaneLength;
	}

	private void set_21_NavigationView_CompactPaneLength(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.CompactPaneLength = (double)Value;
	}

	private object get_22_NavigationView_ContentOverlay(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.ContentOverlay;
	}

	private void set_22_NavigationView_ContentOverlay(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.ContentOverlay = (UIElement)Value;
	}

	private object get_23_NavigationView_DisplayMode(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		NavigationView val = (NavigationView)instance;
		return val.DisplayMode;
	}

	private object get_24_NavigationView_ExpandedModeThresholdWidth(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.ExpandedModeThresholdWidth;
	}

	private void set_24_NavigationView_ExpandedModeThresholdWidth(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.ExpandedModeThresholdWidth = (double)Value;
	}

	private object get_25_NavigationView_FooterMenuItemsSource(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.FooterMenuItemsSource;
	}

	private void set_25_NavigationView_FooterMenuItemsSource(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.FooterMenuItemsSource = Value;
	}

	private object get_26_NavigationView_Header(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.Header;
	}

	private void set_26_NavigationView_Header(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.Header = Value;
	}

	private object get_27_NavigationView_HeaderTemplate(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.HeaderTemplate;
	}

	private void set_27_NavigationView_HeaderTemplate(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.HeaderTemplate = (DataTemplate)Value;
	}

	private object get_28_NavigationView_IsPaneOpen(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.IsPaneOpen;
	}

	private void set_28_NavigationView_IsPaneOpen(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.IsPaneOpen = (bool)Value;
	}

	private object get_29_NavigationView_IsPaneToggleButtonVisible(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.IsPaneToggleButtonVisible;
	}

	private void set_29_NavigationView_IsPaneToggleButtonVisible(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.IsPaneToggleButtonVisible = (bool)Value;
	}

	private object get_30_NavigationView_IsPaneVisible(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.IsPaneVisible;
	}

	private void set_30_NavigationView_IsPaneVisible(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.IsPaneVisible = (bool)Value;
	}

	private object get_31_NavigationView_MenuItemContainerStyle(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.MenuItemContainerStyle;
	}

	private void set_31_NavigationView_MenuItemContainerStyle(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.MenuItemContainerStyle = (Style)Value;
	}

	private object get_32_NavigationView_MenuItemContainerStyleSelector(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.MenuItemContainerStyleSelector;
	}

	private void set_32_NavigationView_MenuItemContainerStyleSelector(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.MenuItemContainerStyleSelector = (StyleSelector)Value;
	}

	private object get_33_NavigationView_MenuItemTemplate(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.MenuItemTemplate;
	}

	private void set_33_NavigationView_MenuItemTemplate(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.MenuItemTemplate = (DataTemplate)Value;
	}

	private object get_34_NavigationView_MenuItemTemplateSelector(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.MenuItemTemplateSelector;
	}

	private void set_34_NavigationView_MenuItemTemplateSelector(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.MenuItemTemplateSelector = (DataTemplateSelector)Value;
	}

	private object get_35_NavigationView_MenuItemsSource(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.MenuItemsSource;
	}

	private void set_35_NavigationView_MenuItemsSource(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.MenuItemsSource = Value;
	}

	private object get_36_NavigationView_OpenPaneLength(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.OpenPaneLength;
	}

	private void set_36_NavigationView_OpenPaneLength(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.OpenPaneLength = (double)Value;
	}

	private object get_37_NavigationView_PaneCustomContent(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.PaneCustomContent;
	}

	private void set_37_NavigationView_PaneCustomContent(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.PaneCustomContent = (UIElement)Value;
	}

	private object get_38_NavigationView_PaneFooter(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.PaneFooter;
	}

	private void set_38_NavigationView_PaneFooter(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.PaneFooter = (UIElement)Value;
	}

	private object get_39_NavigationView_PaneHeader(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.PaneHeader;
	}

	private void set_39_NavigationView_PaneHeader(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.PaneHeader = (UIElement)Value;
	}

	private object get_40_NavigationView_PaneTitle(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.PaneTitle;
	}

	private void set_40_NavigationView_PaneTitle(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.PaneTitle = (string)Value;
	}

	private object get_41_NavigationView_PaneToggleButtonStyle(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.PaneToggleButtonStyle;
	}

	private void set_41_NavigationView_PaneToggleButtonStyle(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.PaneToggleButtonStyle = (Style)Value;
	}

	private object get_42_NavigationView_SelectedItem(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.SelectedItem;
	}

	private void set_42_NavigationView_SelectedItem(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		val.SelectedItem = Value;
	}

	private object get_43_NavigationView_SelectionFollowsFocus(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		NavigationView val = (NavigationView)instance;
		return val.SelectionFollowsFocus;
	}

	private void set_43_NavigationView_SelectionFollowsFocus(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		NavigationView val = (NavigationView)instance;
		val.SelectionFollowsFocus = (NavigationViewSelectionFollowsFocus)Value;
	}

	private object get_44_NavigationView_SettingsItem(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.SettingsItem;
	}

	private object get_45_NavigationView_ShoulderNavigationEnabled(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		NavigationView val = (NavigationView)instance;
		return val.ShoulderNavigationEnabled;
	}

	private void set_45_NavigationView_ShoulderNavigationEnabled(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		NavigationView val = (NavigationView)instance;
		val.ShoulderNavigationEnabled = (NavigationViewShoulderNavigationEnabled)Value;
	}

	private object get_46_NavigationView_TemplateSettings(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationView val = (NavigationView)instance;
		return val.TemplateSettings;
	}

	private object get_47_NavigationViewItemPresenter_Icon(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItemPresenter val = (NavigationViewItemPresenter)instance;
		return val.Icon;
	}

	private void set_47_NavigationViewItemPresenter_Icon(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationViewItemPresenter val = (NavigationViewItemPresenter)instance;
		val.Icon = (IconElement)Value;
	}

	private object get_48_NavigationViewItemPresenter_InfoBadge(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItemPresenter val = (NavigationViewItemPresenter)instance;
		return val.InfoBadge;
	}

	private void set_48_NavigationViewItemPresenter_InfoBadge(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationViewItemPresenter val = (NavigationViewItemPresenter)instance;
		val.InfoBadge = (InfoBadge)Value;
	}

	private object get_49_NavigationViewItemPresenter_TemplateSettings(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItemPresenter val = (NavigationViewItemPresenter)instance;
		return val.TemplateSettings;
	}

	private object get_50_NavigationViewItemBase_IsSelected(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItemBase val = (NavigationViewItemBase)instance;
		return val.IsSelected;
	}

	private void set_50_NavigationViewItemBase_IsSelected(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItemBase val = (NavigationViewItemBase)instance;
		val.IsSelected = (bool)Value;
	}

	private object get_51_NavigationViewItem_Icon(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		return val.Icon;
	}

	private void set_51_NavigationViewItem_Icon(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		val.Icon = (IconElement)Value;
	}

	private object get_52_NavigationViewItem_CompactPaneLength(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		return val.CompactPaneLength;
	}

	private object get_53_NavigationViewItem_HasUnrealizedChildren(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		return val.HasUnrealizedChildren;
	}

	private void set_53_NavigationViewItem_HasUnrealizedChildren(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		val.HasUnrealizedChildren = (bool)Value;
	}

	private object get_54_NavigationViewItem_InfoBadge(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		return val.InfoBadge;
	}

	private void set_54_NavigationViewItem_InfoBadge(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		val.InfoBadge = (InfoBadge)Value;
	}

	private object get_55_NavigationViewItem_IsChildSelected(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		return val.IsChildSelected;
	}

	private void set_55_NavigationViewItem_IsChildSelected(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		val.IsChildSelected = (bool)Value;
	}

	private object get_56_NavigationViewItem_IsExpanded(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		return val.IsExpanded;
	}

	private void set_56_NavigationViewItem_IsExpanded(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		val.IsExpanded = (bool)Value;
	}

	private object get_57_NavigationViewItem_MenuItems(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		return val.MenuItems;
	}

	private object get_58_NavigationViewItem_MenuItemsSource(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		return val.MenuItemsSource;
	}

	private void set_58_NavigationViewItem_MenuItemsSource(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		val.MenuItemsSource = Value;
	}

	private object get_59_NavigationViewItem_SelectsOnInvoked(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		return val.SelectsOnInvoked;
	}

	private void set_59_NavigationViewItem_SelectsOnInvoked(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NavigationViewItem val = (NavigationViewItem)instance;
		val.SelectsOnInvoked = (bool)Value;
	}

	private object get_60_AppShell_ViewModel(object instance)
	{
		AppShell appShell = (AppShell)instance;
		return appShell.ViewModel;
	}

	private void set_60_AppShell_ViewModel(object instance, object Value)
	{
		AppShell appShell = (AppShell)instance;
		appShell.ViewModel = (AppShellViewModel)Value;
	}

	private object get_61_AuthDialogPage_ViewModel(object instance)
	{
		AuthDialogPage authDialogPage = (AuthDialogPage)instance;
		return authDialogPage.ViewModel;
	}

	private void set_61_AuthDialogPage_ViewModel(object instance, object Value)
	{
		AuthDialogPage authDialogPage = (AuthDialogPage)instance;
		authDialogPage.ViewModel = (AuthDialogViewModel)Value;
	}

	private object get_62_WebView2_CanGoBack(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		WebView2 val = (WebView2)instance;
		return val.CanGoBack;
	}

	private void set_62_WebView2_CanGoBack(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		WebView2 val = (WebView2)instance;
		val.CanGoBack = (bool)Value;
	}

	private object get_63_WebView2_CanGoForward(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		WebView2 val = (WebView2)instance;
		return val.CanGoForward;
	}

	private void set_63_WebView2_CanGoForward(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		WebView2 val = (WebView2)instance;
		val.CanGoForward = (bool)Value;
	}

	private object get_64_WebView2_CoreWebView2(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		WebView2 val = (WebView2)instance;
		return val.CoreWebView2;
	}

	private object get_65_WebView2_DefaultBackgroundColor(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WebView2 val = (WebView2)instance;
		return val.DefaultBackgroundColor;
	}

	private void set_65_WebView2_DefaultBackgroundColor(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		WebView2 val = (WebView2)instance;
		val.DefaultBackgroundColor = (Color)Value;
	}

	private object get_66_WebView2_Source(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		WebView2 val = (WebView2)instance;
		return val.Source;
	}

	private void set_66_WebView2_Source(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		WebView2 val = (WebView2)instance;
		val.Source = (Uri)Value;
	}

	private object get_67_ErrorDialogPage_ViewModel(object instance)
	{
		ErrorDialogPage errorDialogPage = (ErrorDialogPage)instance;
		return errorDialogPage.ViewModel;
	}

	private void set_67_ErrorDialogPage_ViewModel(object instance, object Value)
	{
		ErrorDialogPage errorDialogPage = (ErrorDialogPage)instance;
		errorDialogPage.ViewModel = (ErrorDialogViewModel)Value;
	}

	private object get_68_RateUsDialogPage_ViewModel(object instance)
	{
		RateUsDialogPage rateUsDialogPage = (RateUsDialogPage)instance;
		return rateUsDialogPage.ViewModel;
	}

	private void set_68_RateUsDialogPage_ViewModel(object instance, object Value)
	{
		RateUsDialogPage rateUsDialogPage = (RateUsDialogPage)instance;
		rateUsDialogPage.ViewModel = (RateUsDialogViewModel)Value;
	}

	private object get_69_SubscribeDialogPage_ViewModel(object instance)
	{
		SubscribeDialogPage subscribeDialogPage = (SubscribeDialogPage)instance;
		return subscribeDialogPage.ViewModel;
	}

	private void set_69_SubscribeDialogPage_ViewModel(object instance, object Value)
	{
		SubscribeDialogPage subscribeDialogPage = (SubscribeDialogPage)instance;
		subscribeDialogPage.ViewModel = (SubscriptionDialogViewModel)Value;
	}

	private object get_70_TrafficLimitDialogPage_ViewModel(object instance)
	{
		TrafficLimitDialogPage trafficLimitDialogPage = (TrafficLimitDialogPage)instance;
		return trafficLimitDialogPage.ViewModel;
	}

	private void set_70_TrafficLimitDialogPage_ViewModel(object instance, object Value)
	{
		TrafficLimitDialogPage trafficLimitDialogPage = (TrafficLimitDialogPage)instance;
		trafficLimitDialogPage.ViewModel = (TrafficLimitDialogViewModel)Value;
	}

	private object get_71_TrialDialogPage_ViewModel(object instance)
	{
		TrialDialogPage trialDialogPage = (TrialDialogPage)instance;
		return trialDialogPage.ViewModel;
	}

	private void set_71_TrialDialogPage_ViewModel(object instance, object Value)
	{
		TrialDialogPage trialDialogPage = (TrialDialogPage)instance;
		trialDialogPage.ViewModel = (TrialDialogViewModel)Value;
	}

	private object get_72_TaskbarIcon_IconSource(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.IconSource;
	}

	private void set_72_TaskbarIcon_IconSource(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.IconSource = (ImageSource)Value;
	}

	private object get_73_TaskbarIcon_ContextMenuMode(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.ContextMenuMode;
	}

	private void set_73_TaskbarIcon_ContextMenuMode(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		TaskbarIcon val = (TaskbarIcon)instance;
		val.ContextMenuMode = (ContextMenuMode)Value;
	}

	private object get_74_TaskbarIcon_ToolTipText(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.ToolTipText;
	}

	private void set_74_TaskbarIcon_ToolTipText(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.ToolTipText = (string)Value;
	}

	private object get_75_TaskbarIcon_CustomName(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.CustomName;
	}

	private void set_75_TaskbarIcon_CustomName(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.CustomName = (string)Value;
	}

	private object get_76_TaskbarIcon_DoubleClickCommand(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.DoubleClickCommand;
	}

	private void set_76_TaskbarIcon_DoubleClickCommand(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.DoubleClickCommand = (ICommand)Value;
	}

	private object get_77_TaskbarIcon_NoLeftClickDelay(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.NoLeftClickDelay;
	}

	private void set_77_TaskbarIcon_NoLeftClickDelay(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.NoLeftClickDelay = (bool)Value;
	}

	private object get_78_TaskbarIcon_LeftClickCommand(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.LeftClickCommand;
	}

	private void set_78_TaskbarIcon_LeftClickCommand(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.LeftClickCommand = (ICommand)Value;
	}

	private object get_79_TaskbarIcon_TrayIcon(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.TrayIcon;
	}

	private object get_80_TaskbarIcon_IsCreated(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.IsCreated;
	}

	private object get_81_TaskbarIcon_IsDisposed(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.IsDisposed;
	}

	private object get_82_TaskbarIcon_SupportsCustomToolTips(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.SupportsCustomToolTips;
	}

	private object get_83_TaskbarIcon_DoubleClickCommandParameter(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.DoubleClickCommandParameter;
	}

	private void set_83_TaskbarIcon_DoubleClickCommandParameter(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.DoubleClickCommandParameter = Value;
	}

	private object get_84_TaskbarIcon_LeftClickCommandParameter(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.LeftClickCommandParameter;
	}

	private void set_84_TaskbarIcon_LeftClickCommandParameter(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.LeftClickCommandParameter = Value;
	}

	private object get_85_TaskbarIcon_RightClickCommand(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.RightClickCommand;
	}

	private void set_85_TaskbarIcon_RightClickCommand(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.RightClickCommand = (ICommand)Value;
	}

	private object get_86_TaskbarIcon_RightClickCommandParameter(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.RightClickCommandParameter;
	}

	private void set_86_TaskbarIcon_RightClickCommandParameter(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.RightClickCommandParameter = Value;
	}

	private object get_87_TaskbarIcon_MiddleClickCommand(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.MiddleClickCommand;
	}

	private void set_87_TaskbarIcon_MiddleClickCommand(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.MiddleClickCommand = (ICommand)Value;
	}

	private object get_88_TaskbarIcon_MiddleClickCommandParameter(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.MiddleClickCommandParameter;
	}

	private void set_88_TaskbarIcon_MiddleClickCommandParameter(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.MiddleClickCommandParameter = Value;
	}

	private object get_89_TaskbarIcon_MenuActivation(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.MenuActivation;
	}

	private void set_89_TaskbarIcon_MenuActivation(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		TaskbarIcon val = (TaskbarIcon)instance;
		val.MenuActivation = (PopupActivationMode)Value;
	}

	private object get_90_TaskbarIcon_PopupActivation(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.PopupActivation;
	}

	private void set_90_TaskbarIcon_PopupActivation(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		TaskbarIcon val = (TaskbarIcon)instance;
		val.PopupActivation = (PopupActivationMode)Value;
	}

	private object get_91_TaskbarIcon_TrayPopup(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.TrayPopup;
	}

	private void set_91_TaskbarIcon_TrayPopup(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.TrayPopup = (UIElement)Value;
	}

	private object get_92_TaskbarIcon_TrayPopupResolved(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.TrayPopupResolved;
	}

	private object get_93_TaskbarIcon_PopupPlacement(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.PopupPlacement;
	}

	private void set_93_TaskbarIcon_PopupPlacement(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		TaskbarIcon val = (TaskbarIcon)instance;
		val.PopupPlacement = (PlacementMode)Value;
	}

	private object get_94_TaskbarIcon_PopupOffset(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.PopupOffset;
	}

	private void set_94_TaskbarIcon_PopupOffset(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		TaskbarIcon val = (TaskbarIcon)instance;
		val.PopupOffset = (Thickness)Value;
	}

	private object get_95_TaskbarIcon_Id(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.Id;
	}

	private void set_95_TaskbarIcon_Id(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.Id = (Guid)Value;
	}

	private object get_96_TaskbarIcon_Icon(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.Icon;
	}

	private void set_96_TaskbarIcon_Icon(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.Icon = (Icon)Value;
	}

	private object get_97_TaskbarIcon_TrayToolTip(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.TrayToolTip;
	}

	private void set_97_TaskbarIcon_TrayToolTip(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		val.TrayToolTip = (UIElement)Value;
	}

	private object get_98_TaskbarIcon_TrayToolTipResolved(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TaskbarIcon val = (TaskbarIcon)instance;
		return val.TrayToolTipResolved;
	}

	private object get_99_TaskbarIcon_ParentTaskbarIcon(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		return TaskbarIcon.GetParentTaskbarIcon((DependencyObject)instance);
	}

	private void set_99_TaskbarIcon_ParentTaskbarIcon(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_0012: Expected O, but got Unknown
		TaskbarIcon.SetParentTaskbarIcon((DependencyObject)instance, (TaskbarIcon)Value);
	}

	private object get_100_MainWindow_Restore(object instance)
	{
		MainWindow mainWindow = (MainWindow)instance;
		return mainWindow.Restore;
	}

	private object get_101_MainWindow_Exit(object instance)
	{
		MainWindow mainWindow = (MainWindow)instance;
		return mainWindow.Exit;
	}

	private object get_102_LocationsPage_ViewModel(object instance)
	{
		LocationsPage locationsPage = (LocationsPage)instance;
		return locationsPage.ViewModel;
	}

	private void set_102_LocationsPage_ViewModel(object instance, object Value)
	{
		LocationsPage locationsPage = (LocationsPage)instance;
		locationsPage.ViewModel = (LocationsPageViewModel)Value;
	}

	private object get_103_RadialGradientBrush_GradientStops(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		RadialGradientBrush val = (RadialGradientBrush)instance;
		return val.GradientStops;
	}

	private object get_104_RadialGradientBrush_Center(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		RadialGradientBrush val = (RadialGradientBrush)instance;
		return val.Center;
	}

	private void set_104_RadialGradientBrush_Center(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		RadialGradientBrush val = (RadialGradientBrush)instance;
		val.Center = (Point)Value;
	}

	private object get_105_RadialGradientBrush_RadiusX(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		RadialGradientBrush val = (RadialGradientBrush)instance;
		return val.RadiusX;
	}

	private void set_105_RadialGradientBrush_RadiusX(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		RadialGradientBrush val = (RadialGradientBrush)instance;
		val.RadiusX = (double)Value;
	}

	private object get_106_RadialGradientBrush_RadiusY(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		RadialGradientBrush val = (RadialGradientBrush)instance;
		return val.RadiusY;
	}

	private void set_106_RadialGradientBrush_RadiusY(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		RadialGradientBrush val = (RadialGradientBrush)instance;
		val.RadiusY = (double)Value;
	}

	private object get_107_RadialGradientBrush_GradientOrigin(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		RadialGradientBrush val = (RadialGradientBrush)instance;
		return val.GradientOrigin;
	}

	private void set_107_RadialGradientBrush_GradientOrigin(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		RadialGradientBrush val = (RadialGradientBrush)instance;
		val.GradientOrigin = (Point)Value;
	}

	private object get_108_RadialGradientBrush_InterpolationSpace(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		RadialGradientBrush val = (RadialGradientBrush)instance;
		return val.InterpolationSpace;
	}

	private void set_108_RadialGradientBrush_InterpolationSpace(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		RadialGradientBrush val = (RadialGradientBrush)instance;
		val.InterpolationSpace = (CompositionColorSpace)Value;
	}

	private object get_109_RadialGradientBrush_MappingMode(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		RadialGradientBrush val = (RadialGradientBrush)instance;
		return val.MappingMode;
	}

	private void set_109_RadialGradientBrush_MappingMode(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		RadialGradientBrush val = (RadialGradientBrush)instance;
		val.MappingMode = (BrushMappingMode)Value;
	}

	private object get_110_RadialGradientBrush_SpreadMethod(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		RadialGradientBrush val = (RadialGradientBrush)instance;
		return val.SpreadMethod;
	}

	private void set_110_RadialGradientBrush_SpreadMethod(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		RadialGradientBrush val = (RadialGradientBrush)instance;
		val.SpreadMethod = (GradientSpreadMethod)Value;
	}

	private object get_111_MainPage_ViewModel(object instance)
	{
		MainPage mainPage = (MainPage)instance;
		return mainPage.ViewModel;
	}

	private void set_111_MainPage_ViewModel(object instance, object Value)
	{
		MainPage mainPage = (MainPage)instance;
		mainPage.ViewModel = (MainPageViewModel)Value;
	}

	private object get_112_SettingsPage_ViewModel(object instance)
	{
		SettingsPage settingsPage = (SettingsPage)instance;
		return settingsPage.ViewModel;
	}

	private void set_112_SettingsPage_ViewModel(object instance, object Value)
	{
		SettingsPage settingsPage = (SettingsPage)instance;
		settingsPage.ViewModel = (SettingsPageViewModel)Value;
	}

	private object get_113_Expander_Header(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		Expander val = (Expander)instance;
		return val.Header;
	}

	private void set_113_Expander_Header(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		Expander val = (Expander)instance;
		val.Header = Value;
	}

	private object get_114_Expander_IsExpanded(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		Expander val = (Expander)instance;
		return val.IsExpanded;
	}

	private void set_114_Expander_IsExpanded(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		Expander val = (Expander)instance;
		val.IsExpanded = (bool)Value;
	}

	private object get_115_Expander_ExpandDirection(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		Expander val = (Expander)instance;
		return val.ExpandDirection;
	}

	private void set_115_Expander_ExpandDirection(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		Expander val = (Expander)instance;
		val.ExpandDirection = (ExpandDirection)Value;
	}

	private object get_116_Expander_HeaderTemplate(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		Expander val = (Expander)instance;
		return val.HeaderTemplate;
	}

	private void set_116_Expander_HeaderTemplate(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		Expander val = (Expander)instance;
		val.HeaderTemplate = (DataTemplate)Value;
	}

	private object get_117_Expander_HeaderTemplateSelector(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		Expander val = (Expander)instance;
		return val.HeaderTemplateSelector;
	}

	private void set_117_Expander_HeaderTemplateSelector(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		Expander val = (Expander)instance;
		val.HeaderTemplateSelector = (DataTemplateSelector)Value;
	}

	private object get_118_Expander_TemplateSettings(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		Expander val = (Expander)instance;
		return val.TemplateSettings;
	}

	private object get_119_SplitTunnelPage_ViewModel(object instance)
	{
		SplitTunnelPage splitTunnelPage = (SplitTunnelPage)instance;
		return splitTunnelPage.ViewModel;
	}

	private void set_119_SplitTunnelPage_ViewModel(object instance, object Value)
	{
		SplitTunnelPage splitTunnelPage = (SplitTunnelPage)instance;
		splitTunnelPage.ViewModel = (SplitTunnelPageViewModel)Value;
	}

	private object get_120_TreeViewNode_Children(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TreeViewNode val = (TreeViewNode)instance;
		return val.Children;
	}

	private object get_121_TreeViewNode_Content(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TreeViewNode val = (TreeViewNode)instance;
		return val.Content;
	}

	private void set_121_TreeViewNode_Content(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TreeViewNode val = (TreeViewNode)instance;
		val.Content = Value;
	}

	private object get_122_TreeViewNode_Depth(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TreeViewNode val = (TreeViewNode)instance;
		return val.Depth;
	}

	private object get_123_TreeViewNode_HasChildren(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TreeViewNode val = (TreeViewNode)instance;
		return val.HasChildren;
	}

	private object get_124_TreeViewNode_HasUnrealizedChildren(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TreeViewNode val = (TreeViewNode)instance;
		return val.HasUnrealizedChildren;
	}

	private void set_124_TreeViewNode_HasUnrealizedChildren(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TreeViewNode val = (TreeViewNode)instance;
		val.HasUnrealizedChildren = (bool)Value;
	}

	private object get_125_TreeViewNode_IsExpanded(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TreeViewNode val = (TreeViewNode)instance;
		return val.IsExpanded;
	}

	private void set_125_TreeViewNode_IsExpanded(object instance, object Value)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TreeViewNode val = (TreeViewNode)instance;
		val.IsExpanded = (bool)Value;
	}

	private object get_126_TreeViewNode_Parent(object instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TreeViewNode val = (TreeViewNode)instance;
		return val.Parent;
	}

	private IXamlMember CreateXamlMember(string longMemberName)
	{
		XamlMember xamlMember = null;
		switch (longMemberName)
		{
		case "Microsoft.UI.Xaml.Controls.XamlControlsResources.UseCompactResources":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.XamlControlsResources");
			xamlMember = new XamlMember(this, "UseCompactResources", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_0_XamlControlsResources_UseCompactResources;
			xamlMember.Setter = set_0_XamlControlsResources_UseCompactResources;
			break;
		}
		case "Microsoft.UI.Xaml.Media.MicaBackdrop.Kind":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Media.MicaBackdrop");
			xamlMember = new XamlMember(this, "Kind", "Microsoft.UI.Composition.SystemBackdrops.MicaKind");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_1_MicaBackdrop_Kind;
			xamlMember.Setter = set_1_MicaBackdrop_Kind;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.ProgressRing.IsActive":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressRing");
			xamlMember = new XamlMember(this, "IsActive", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_2_ProgressRing_IsActive;
			xamlMember.Setter = set_2_ProgressRing_IsActive;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.ProgressRing.IsIndeterminate":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressRing");
			xamlMember = new XamlMember(this, "IsIndeterminate", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_3_ProgressRing_IsIndeterminate;
			xamlMember.Setter = set_3_ProgressRing_IsIndeterminate;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.ProgressRing.Maximum":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressRing");
			xamlMember = new XamlMember(this, "Maximum", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_4_ProgressRing_Maximum;
			xamlMember.Setter = set_4_ProgressRing_Maximum;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.ProgressRing.Minimum":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressRing");
			xamlMember = new XamlMember(this, "Minimum", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_5_ProgressRing_Minimum;
			xamlMember.Setter = set_5_ProgressRing_Minimum;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.ProgressRing.TemplateSettings":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressRing");
			xamlMember = new XamlMember(this, "TemplateSettings", "Microsoft.UI.Xaml.Controls.ProgressRingTemplateSettings");
			xamlMember.Getter = get_6_ProgressRing_TemplateSettings;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Microsoft.UI.Xaml.Controls.ProgressRing.Value":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressRing");
			xamlMember = new XamlMember(this, "Value", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_7_ProgressRing_Value;
			xamlMember.Setter = set_7_ProgressRing_Value;
			break;
		}
		case "Ciphra.VPN.WinUI.AppWindows.AuthWindow.Url":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.AppWindows.AuthWindow");
			xamlMember = new XamlMember(this, "Url", "String");
			xamlMember.Getter = get_8_AuthWindow_Url;
			xamlMember.Setter = set_8_AuthWindow_Url;
			break;
		}
		case "Ciphra.VPN.WinUI.AppWindows.PaymentWindow.Url":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.AppWindows.PaymentWindow");
			xamlMember = new XamlMember(this, "Url", "String");
			xamlMember.Getter = get_9_PaymentWindow_Url;
			xamlMember.Setter = set_9_PaymentWindow_Url;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.PaneDisplayMode":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "PaneDisplayMode", "Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_10_NavigationView_PaneDisplayMode;
			xamlMember.Setter = set_10_NavigationView_PaneDisplayMode;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.OverflowLabelMode":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "OverflowLabelMode", "Microsoft.UI.Xaml.Controls.NavigationViewOverflowLabelMode");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_11_NavigationView_OverflowLabelMode;
			xamlMember.Setter = set_11_NavigationView_OverflowLabelMode;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.IsTitleBarAutoPaddingEnabled":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "IsTitleBarAutoPaddingEnabled", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_12_NavigationView_IsTitleBarAutoPaddingEnabled;
			xamlMember.Setter = set_12_NavigationView_IsTitleBarAutoPaddingEnabled;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.IsBackButtonVisible":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "IsBackButtonVisible", "Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_13_NavigationView_IsBackButtonVisible;
			xamlMember.Setter = set_13_NavigationView_IsBackButtonVisible;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.IsBackEnabled":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "IsBackEnabled", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_14_NavigationView_IsBackEnabled;
			xamlMember.Setter = set_14_NavigationView_IsBackEnabled;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.IsSettingsVisible":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "IsSettingsVisible", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_15_NavigationView_IsSettingsVisible;
			xamlMember.Setter = set_15_NavigationView_IsSettingsVisible;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.MenuItems":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "MenuItems", "System.Collections.Generic.IList`1<Object>");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_16_NavigationView_MenuItems;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.FooterMenuItems":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "FooterMenuItems", "System.Collections.Generic.IList`1<Object>");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_17_NavigationView_FooterMenuItems;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.AlwaysShowHeader":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "AlwaysShowHeader", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_18_NavigationView_AlwaysShowHeader;
			xamlMember.Setter = set_18_NavigationView_AlwaysShowHeader;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.AutoSuggestBox":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "AutoSuggestBox", "Microsoft.UI.Xaml.Controls.AutoSuggestBox");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_19_NavigationView_AutoSuggestBox;
			xamlMember.Setter = set_19_NavigationView_AutoSuggestBox;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.CompactModeThresholdWidth":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "CompactModeThresholdWidth", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_20_NavigationView_CompactModeThresholdWidth;
			xamlMember.Setter = set_20_NavigationView_CompactModeThresholdWidth;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.CompactPaneLength":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "CompactPaneLength", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_21_NavigationView_CompactPaneLength;
			xamlMember.Setter = set_21_NavigationView_CompactPaneLength;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.ContentOverlay":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "ContentOverlay", "Microsoft.UI.Xaml.UIElement");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_22_NavigationView_ContentOverlay;
			xamlMember.Setter = set_22_NavigationView_ContentOverlay;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.DisplayMode":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "DisplayMode", "Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_23_NavigationView_DisplayMode;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.ExpandedModeThresholdWidth":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "ExpandedModeThresholdWidth", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_24_NavigationView_ExpandedModeThresholdWidth;
			xamlMember.Setter = set_24_NavigationView_ExpandedModeThresholdWidth;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.FooterMenuItemsSource":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "FooterMenuItemsSource", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_25_NavigationView_FooterMenuItemsSource;
			xamlMember.Setter = set_25_NavigationView_FooterMenuItemsSource;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.Header":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "Header", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_26_NavigationView_Header;
			xamlMember.Setter = set_26_NavigationView_Header;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.HeaderTemplate":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "HeaderTemplate", "Microsoft.UI.Xaml.DataTemplate");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_27_NavigationView_HeaderTemplate;
			xamlMember.Setter = set_27_NavigationView_HeaderTemplate;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.IsPaneOpen":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "IsPaneOpen", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_28_NavigationView_IsPaneOpen;
			xamlMember.Setter = set_28_NavigationView_IsPaneOpen;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.IsPaneToggleButtonVisible":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "IsPaneToggleButtonVisible", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_29_NavigationView_IsPaneToggleButtonVisible;
			xamlMember.Setter = set_29_NavigationView_IsPaneToggleButtonVisible;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.IsPaneVisible":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "IsPaneVisible", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_30_NavigationView_IsPaneVisible;
			xamlMember.Setter = set_30_NavigationView_IsPaneVisible;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.MenuItemContainerStyle":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "MenuItemContainerStyle", "Microsoft.UI.Xaml.Style");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_31_NavigationView_MenuItemContainerStyle;
			xamlMember.Setter = set_31_NavigationView_MenuItemContainerStyle;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.MenuItemContainerStyleSelector":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "MenuItemContainerStyleSelector", "Microsoft.UI.Xaml.Controls.StyleSelector");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_32_NavigationView_MenuItemContainerStyleSelector;
			xamlMember.Setter = set_32_NavigationView_MenuItemContainerStyleSelector;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.MenuItemTemplate":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "MenuItemTemplate", "Microsoft.UI.Xaml.DataTemplate");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_33_NavigationView_MenuItemTemplate;
			xamlMember.Setter = set_33_NavigationView_MenuItemTemplate;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.MenuItemTemplateSelector":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "MenuItemTemplateSelector", "Microsoft.UI.Xaml.Controls.DataTemplateSelector");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_34_NavigationView_MenuItemTemplateSelector;
			xamlMember.Setter = set_34_NavigationView_MenuItemTemplateSelector;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.MenuItemsSource":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "MenuItemsSource", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_35_NavigationView_MenuItemsSource;
			xamlMember.Setter = set_35_NavigationView_MenuItemsSource;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.OpenPaneLength":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "OpenPaneLength", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_36_NavigationView_OpenPaneLength;
			xamlMember.Setter = set_36_NavigationView_OpenPaneLength;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.PaneCustomContent":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "PaneCustomContent", "Microsoft.UI.Xaml.UIElement");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_37_NavigationView_PaneCustomContent;
			xamlMember.Setter = set_37_NavigationView_PaneCustomContent;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.PaneFooter":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "PaneFooter", "Microsoft.UI.Xaml.UIElement");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_38_NavigationView_PaneFooter;
			xamlMember.Setter = set_38_NavigationView_PaneFooter;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.PaneHeader":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "PaneHeader", "Microsoft.UI.Xaml.UIElement");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_39_NavigationView_PaneHeader;
			xamlMember.Setter = set_39_NavigationView_PaneHeader;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.PaneTitle":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "PaneTitle", "String");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_40_NavigationView_PaneTitle;
			xamlMember.Setter = set_40_NavigationView_PaneTitle;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.PaneToggleButtonStyle":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "PaneToggleButtonStyle", "Microsoft.UI.Xaml.Style");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_41_NavigationView_PaneToggleButtonStyle;
			xamlMember.Setter = set_41_NavigationView_PaneToggleButtonStyle;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.SelectedItem":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "SelectedItem", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_42_NavigationView_SelectedItem;
			xamlMember.Setter = set_42_NavigationView_SelectedItem;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.SelectionFollowsFocus":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "SelectionFollowsFocus", "Microsoft.UI.Xaml.Controls.NavigationViewSelectionFollowsFocus");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_43_NavigationView_SelectionFollowsFocus;
			xamlMember.Setter = set_43_NavigationView_SelectionFollowsFocus;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.SettingsItem":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "SettingsItem", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_44_NavigationView_SettingsItem;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.ShoulderNavigationEnabled":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "ShoulderNavigationEnabled", "Microsoft.UI.Xaml.Controls.NavigationViewShoulderNavigationEnabled");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_45_NavigationView_ShoulderNavigationEnabled;
			xamlMember.Setter = set_45_NavigationView_ShoulderNavigationEnabled;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationView.TemplateSettings":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "TemplateSettings", "Microsoft.UI.Xaml.Controls.NavigationViewTemplateSettings");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_46_NavigationView_TemplateSettings;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Microsoft.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter.Icon":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter");
			xamlMember = new XamlMember(this, "Icon", "Microsoft.UI.Xaml.Controls.IconElement");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_47_NavigationViewItemPresenter_Icon;
			xamlMember.Setter = set_47_NavigationViewItemPresenter_Icon;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter.InfoBadge":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter");
			xamlMember = new XamlMember(this, "InfoBadge", "Microsoft.UI.Xaml.Controls.InfoBadge");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_48_NavigationViewItemPresenter_InfoBadge;
			xamlMember.Setter = set_48_NavigationViewItemPresenter_InfoBadge;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter.TemplateSettings":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter");
			xamlMember = new XamlMember(this, "TemplateSettings", "Microsoft.UI.Xaml.Controls.Primitives.NavigationViewItemPresenterTemplateSettings");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_49_NavigationViewItemPresenter_TemplateSettings;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationViewItemBase.IsSelected":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItemBase");
			xamlMember = new XamlMember(this, "IsSelected", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_50_NavigationViewItemBase_IsSelected;
			xamlMember.Setter = set_50_NavigationViewItemBase_IsSelected;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.Icon":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "Icon", "Microsoft.UI.Xaml.Controls.IconElement");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_51_NavigationViewItem_Icon;
			xamlMember.Setter = set_51_NavigationViewItem_Icon;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.CompactPaneLength":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "CompactPaneLength", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_52_NavigationViewItem_CompactPaneLength;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.HasUnrealizedChildren":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "HasUnrealizedChildren", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_53_NavigationViewItem_HasUnrealizedChildren;
			xamlMember.Setter = set_53_NavigationViewItem_HasUnrealizedChildren;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.InfoBadge":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "InfoBadge", "Microsoft.UI.Xaml.Controls.InfoBadge");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_54_NavigationViewItem_InfoBadge;
			xamlMember.Setter = set_54_NavigationViewItem_InfoBadge;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.IsChildSelected":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "IsChildSelected", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_55_NavigationViewItem_IsChildSelected;
			xamlMember.Setter = set_55_NavigationViewItem_IsChildSelected;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.IsExpanded":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "IsExpanded", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_56_NavigationViewItem_IsExpanded;
			xamlMember.Setter = set_56_NavigationViewItem_IsExpanded;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.MenuItems":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "MenuItems", "System.Collections.Generic.IList`1<Object>");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_57_NavigationViewItem_MenuItems;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.MenuItemsSource":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "MenuItemsSource", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_58_NavigationViewItem_MenuItemsSource;
			xamlMember.Setter = set_58_NavigationViewItem_MenuItemsSource;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.SelectsOnInvoked":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "SelectsOnInvoked", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_59_NavigationViewItem_SelectsOnInvoked;
			xamlMember.Setter = set_59_NavigationViewItem_SelectsOnInvoked;
			break;
		}
		case "Ciphra.VPN.WinUI.Controls.AppShell.ViewModel":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.Controls.AppShell");
			xamlMember = new XamlMember(this, "ViewModel", "Ciphra.VPN.Common.ViewModels.AppShellViewModel");
			xamlMember.Getter = get_60_AppShell_ViewModel;
			xamlMember.Setter = set_60_AppShell_ViewModel;
			break;
		}
		case "Ciphra.VPN.WinUI.Dialogs.AuthDialogPage.ViewModel":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.Dialogs.AuthDialogPage");
			xamlMember = new XamlMember(this, "ViewModel", "Ciphra.VPN.Common.ViewModels.DialogViewModels.AuthDialogViewModel");
			xamlMember.Getter = get_61_AuthDialogPage_ViewModel;
			xamlMember.Setter = set_61_AuthDialogPage_ViewModel;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.WebView2.CanGoBack":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.WebView2");
			xamlMember = new XamlMember(this, "CanGoBack", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_62_WebView2_CanGoBack;
			xamlMember.Setter = set_62_WebView2_CanGoBack;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.WebView2.CanGoForward":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.WebView2");
			xamlMember = new XamlMember(this, "CanGoForward", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_63_WebView2_CanGoForward;
			xamlMember.Setter = set_63_WebView2_CanGoForward;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.WebView2.CoreWebView2":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.WebView2");
			xamlMember = new XamlMember(this, "CoreWebView2", "Microsoft.Web.WebView2.Core.CoreWebView2");
			xamlMember.Getter = get_64_WebView2_CoreWebView2;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Microsoft.UI.Xaml.Controls.WebView2.DefaultBackgroundColor":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.WebView2");
			xamlMember = new XamlMember(this, "DefaultBackgroundColor", "Windows.UI.Color");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_65_WebView2_DefaultBackgroundColor;
			xamlMember.Setter = set_65_WebView2_DefaultBackgroundColor;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.WebView2.Source":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.WebView2");
			xamlMember = new XamlMember(this, "Source", "System.Uri");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_66_WebView2_Source;
			xamlMember.Setter = set_66_WebView2_Source;
			break;
		}
		case "Ciphra.VPN.WinUI.Dialogs.ErrorDialogPage.ViewModel":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.Dialogs.ErrorDialogPage");
			xamlMember = new XamlMember(this, "ViewModel", "Ciphra.VPN.Common.ViewModels.DialogViewModels.ErrorDialogViewModel");
			xamlMember.Getter = get_67_ErrorDialogPage_ViewModel;
			xamlMember.Setter = set_67_ErrorDialogPage_ViewModel;
			break;
		}
		case "Ciphra.VPN.WinUI.Dialogs.RateUsDialogPage.ViewModel":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.Dialogs.RateUsDialogPage");
			xamlMember = new XamlMember(this, "ViewModel", "Ciphra.VPN.Common.ViewModels.DialogViewModels.RateUsDialogViewModel");
			xamlMember.Getter = get_68_RateUsDialogPage_ViewModel;
			xamlMember.Setter = set_68_RateUsDialogPage_ViewModel;
			break;
		}
		case "Ciphra.VPN.WinUI.Dialogs.SubscribeDialogPage.ViewModel":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.Dialogs.SubscribeDialogPage");
			xamlMember = new XamlMember(this, "ViewModel", "Ciphra.VPN.Common.ViewModels.DialogViewModels.SubscriptionDialogViewModel");
			xamlMember.Getter = get_69_SubscribeDialogPage_ViewModel;
			xamlMember.Setter = set_69_SubscribeDialogPage_ViewModel;
			break;
		}
		case "Ciphra.VPN.WinUI.Dialogs.TrafficLimitDialogPage.ViewModel":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.Dialogs.TrafficLimitDialogPage");
			xamlMember = new XamlMember(this, "ViewModel", "Ciphra.VPN.Common.ViewModels.DialogViewModels.TrafficLimitDialogViewModel");
			xamlMember.Getter = get_70_TrafficLimitDialogPage_ViewModel;
			xamlMember.Setter = set_70_TrafficLimitDialogPage_ViewModel;
			break;
		}
		case "Ciphra.VPN.WinUI.Dialogs.TrialDialogPage.ViewModel":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.Dialogs.TrialDialogPage");
			xamlMember = new XamlMember(this, "ViewModel", "Ciphra.VPN.Common.ViewModels.DialogViewModels.TrialDialogViewModel");
			xamlMember.Getter = get_71_TrialDialogPage_ViewModel;
			xamlMember.Setter = set_71_TrialDialogPage_ViewModel;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.IconSource":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "IconSource", "Microsoft.UI.Xaml.Media.ImageSource");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_72_TaskbarIcon_IconSource;
			xamlMember.Setter = set_72_TaskbarIcon_IconSource;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.ContextMenuMode":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "ContextMenuMode", "H.NotifyIcon.ContextMenuMode");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_73_TaskbarIcon_ContextMenuMode;
			xamlMember.Setter = set_73_TaskbarIcon_ContextMenuMode;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.ToolTipText":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "ToolTipText", "String");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_74_TaskbarIcon_ToolTipText;
			xamlMember.Setter = set_74_TaskbarIcon_ToolTipText;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.CustomName":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "CustomName", "String");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_75_TaskbarIcon_CustomName;
			xamlMember.Setter = set_75_TaskbarIcon_CustomName;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.DoubleClickCommand":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "DoubleClickCommand", "System.Windows.Input.ICommand");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_76_TaskbarIcon_DoubleClickCommand;
			xamlMember.Setter = set_76_TaskbarIcon_DoubleClickCommand;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.NoLeftClickDelay":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "NoLeftClickDelay", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_77_TaskbarIcon_NoLeftClickDelay;
			xamlMember.Setter = set_77_TaskbarIcon_NoLeftClickDelay;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.LeftClickCommand":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "LeftClickCommand", "System.Windows.Input.ICommand");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_78_TaskbarIcon_LeftClickCommand;
			xamlMember.Setter = set_78_TaskbarIcon_LeftClickCommand;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.TrayIcon":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "TrayIcon", "H.NotifyIcon.Core.TrayIcon");
			xamlMember.Getter = get_79_TaskbarIcon_TrayIcon;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.IsCreated":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "IsCreated", "Boolean");
			xamlMember.Getter = get_80_TaskbarIcon_IsCreated;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.IsDisposed":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "IsDisposed", "Boolean");
			xamlMember.Getter = get_81_TaskbarIcon_IsDisposed;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.SupportsCustomToolTips":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "SupportsCustomToolTips", "Boolean");
			xamlMember.Getter = get_82_TaskbarIcon_SupportsCustomToolTips;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.DoubleClickCommandParameter":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "DoubleClickCommandParameter", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_83_TaskbarIcon_DoubleClickCommandParameter;
			xamlMember.Setter = set_83_TaskbarIcon_DoubleClickCommandParameter;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.LeftClickCommandParameter":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "LeftClickCommandParameter", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_84_TaskbarIcon_LeftClickCommandParameter;
			xamlMember.Setter = set_84_TaskbarIcon_LeftClickCommandParameter;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.RightClickCommand":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "RightClickCommand", "System.Windows.Input.ICommand");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_85_TaskbarIcon_RightClickCommand;
			xamlMember.Setter = set_85_TaskbarIcon_RightClickCommand;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.RightClickCommandParameter":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "RightClickCommandParameter", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_86_TaskbarIcon_RightClickCommandParameter;
			xamlMember.Setter = set_86_TaskbarIcon_RightClickCommandParameter;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.MiddleClickCommand":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "MiddleClickCommand", "System.Windows.Input.ICommand");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_87_TaskbarIcon_MiddleClickCommand;
			xamlMember.Setter = set_87_TaskbarIcon_MiddleClickCommand;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.MiddleClickCommandParameter":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "MiddleClickCommandParameter", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_88_TaskbarIcon_MiddleClickCommandParameter;
			xamlMember.Setter = set_88_TaskbarIcon_MiddleClickCommandParameter;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.MenuActivation":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "MenuActivation", "H.NotifyIcon.Core.PopupActivationMode");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_89_TaskbarIcon_MenuActivation;
			xamlMember.Setter = set_89_TaskbarIcon_MenuActivation;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.PopupActivation":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "PopupActivation", "H.NotifyIcon.Core.PopupActivationMode");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_90_TaskbarIcon_PopupActivation;
			xamlMember.Setter = set_90_TaskbarIcon_PopupActivation;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.TrayPopup":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "TrayPopup", "Microsoft.UI.Xaml.UIElement");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_91_TaskbarIcon_TrayPopup;
			xamlMember.Setter = set_91_TaskbarIcon_TrayPopup;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.TrayPopupResolved":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "TrayPopupResolved", "Microsoft.UI.Xaml.Controls.Primitives.Popup");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_92_TaskbarIcon_TrayPopupResolved;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.PopupPlacement":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "PopupPlacement", "Microsoft.UI.Xaml.Controls.Primitives.PlacementMode");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_93_TaskbarIcon_PopupPlacement;
			xamlMember.Setter = set_93_TaskbarIcon_PopupPlacement;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.PopupOffset":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "PopupOffset", "Microsoft.UI.Xaml.Thickness");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_94_TaskbarIcon_PopupOffset;
			xamlMember.Setter = set_94_TaskbarIcon_PopupOffset;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.Id":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "Id", "Guid");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_95_TaskbarIcon_Id;
			xamlMember.Setter = set_95_TaskbarIcon_Id;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.Icon":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "Icon", "System.Drawing.Icon");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_96_TaskbarIcon_Icon;
			xamlMember.Setter = set_96_TaskbarIcon_Icon;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.TrayToolTip":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "TrayToolTip", "Microsoft.UI.Xaml.UIElement");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_97_TaskbarIcon_TrayToolTip;
			xamlMember.Setter = set_97_TaskbarIcon_TrayToolTip;
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.TrayToolTipResolved":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "TrayToolTipResolved", "Microsoft.UI.Xaml.Controls.ToolTip");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_98_TaskbarIcon_TrayToolTipResolved;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "H.NotifyIcon.TaskbarIcon.ParentTaskbarIcon":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("H.NotifyIcon.TaskbarIcon");
			xamlMember = new XamlMember(this, "ParentTaskbarIcon", "H.NotifyIcon.TaskbarIcon");
			xamlMember.SetTargetTypeName("Microsoft.UI.Xaml.DependencyObject");
			xamlMember.SetIsDependencyProperty();
			xamlMember.SetIsAttachable();
			xamlMember.Getter = get_99_TaskbarIcon_ParentTaskbarIcon;
			xamlMember.Setter = set_99_TaskbarIcon_ParentTaskbarIcon;
			break;
		}
		case "Ciphra.VPN.WinUI.MainWindow.Restore":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.MainWindow");
			xamlMember = new XamlMember(this, "Restore", "System.Windows.Input.ICommand");
			xamlMember.Getter = get_100_MainWindow_Restore;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Ciphra.VPN.WinUI.MainWindow.Exit":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.MainWindow");
			xamlMember = new XamlMember(this, "Exit", "System.Windows.Input.ICommand");
			xamlMember.Getter = get_101_MainWindow_Exit;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Ciphra.VPN.WinUI.Pages.LocationsPage.ViewModel":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.Pages.LocationsPage");
			xamlMember = new XamlMember(this, "ViewModel", "Ciphra.VPN.Common.ViewModels.LocationsPageViewModel");
			xamlMember.Getter = get_102_LocationsPage_ViewModel;
			xamlMember.Setter = set_102_LocationsPage_ViewModel;
			break;
		}
		case "Microsoft.UI.Xaml.Media.RadialGradientBrush.GradientStops":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Media.RadialGradientBrush");
			xamlMember = new XamlMember(this, "GradientStops", "Windows.Foundation.Collections.IObservableVector`1<Microsoft.UI.Xaml.Media.GradientStop>");
			xamlMember.Getter = get_103_RadialGradientBrush_GradientStops;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Microsoft.UI.Xaml.Media.RadialGradientBrush.Center":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Media.RadialGradientBrush");
			xamlMember = new XamlMember(this, "Center", "Windows.Foundation.Point");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_104_RadialGradientBrush_Center;
			xamlMember.Setter = set_104_RadialGradientBrush_Center;
			break;
		}
		case "Microsoft.UI.Xaml.Media.RadialGradientBrush.RadiusX":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Media.RadialGradientBrush");
			xamlMember = new XamlMember(this, "RadiusX", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_105_RadialGradientBrush_RadiusX;
			xamlMember.Setter = set_105_RadialGradientBrush_RadiusX;
			break;
		}
		case "Microsoft.UI.Xaml.Media.RadialGradientBrush.RadiusY":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Media.RadialGradientBrush");
			xamlMember = new XamlMember(this, "RadiusY", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_106_RadialGradientBrush_RadiusY;
			xamlMember.Setter = set_106_RadialGradientBrush_RadiusY;
			break;
		}
		case "Microsoft.UI.Xaml.Media.RadialGradientBrush.GradientOrigin":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Media.RadialGradientBrush");
			xamlMember = new XamlMember(this, "GradientOrigin", "Windows.Foundation.Point");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_107_RadialGradientBrush_GradientOrigin;
			xamlMember.Setter = set_107_RadialGradientBrush_GradientOrigin;
			break;
		}
		case "Microsoft.UI.Xaml.Media.RadialGradientBrush.InterpolationSpace":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Media.RadialGradientBrush");
			xamlMember = new XamlMember(this, "InterpolationSpace", "Microsoft.UI.Composition.CompositionColorSpace");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_108_RadialGradientBrush_InterpolationSpace;
			xamlMember.Setter = set_108_RadialGradientBrush_InterpolationSpace;
			break;
		}
		case "Microsoft.UI.Xaml.Media.RadialGradientBrush.MappingMode":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Media.RadialGradientBrush");
			xamlMember = new XamlMember(this, "MappingMode", "Microsoft.UI.Xaml.Media.BrushMappingMode");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_109_RadialGradientBrush_MappingMode;
			xamlMember.Setter = set_109_RadialGradientBrush_MappingMode;
			break;
		}
		case "Microsoft.UI.Xaml.Media.RadialGradientBrush.SpreadMethod":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Media.RadialGradientBrush");
			xamlMember = new XamlMember(this, "SpreadMethod", "Microsoft.UI.Xaml.Media.GradientSpreadMethod");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_110_RadialGradientBrush_SpreadMethod;
			xamlMember.Setter = set_110_RadialGradientBrush_SpreadMethod;
			break;
		}
		case "Ciphra.VPN.WinUI.Pages.MainPage.ViewModel":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.Pages.MainPage");
			xamlMember = new XamlMember(this, "ViewModel", "Ciphra.VPN.Common.ViewModels.MainPageViewModel");
			xamlMember.Getter = get_111_MainPage_ViewModel;
			xamlMember.Setter = set_111_MainPage_ViewModel;
			break;
		}
		case "Ciphra.VPN.WinUI.Pages.SettingsPage.ViewModel":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.Pages.SettingsPage");
			xamlMember = new XamlMember(this, "ViewModel", "Ciphra.VPN.Common.ViewModels.SettingsPageViewModel");
			xamlMember.Getter = get_112_SettingsPage_ViewModel;
			xamlMember.Setter = set_112_SettingsPage_ViewModel;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.Expander.Header":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Expander");
			xamlMember = new XamlMember(this, "Header", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_113_Expander_Header;
			xamlMember.Setter = set_113_Expander_Header;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.Expander.IsExpanded":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Expander");
			xamlMember = new XamlMember(this, "IsExpanded", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_114_Expander_IsExpanded;
			xamlMember.Setter = set_114_Expander_IsExpanded;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.Expander.ExpandDirection":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Expander");
			xamlMember = new XamlMember(this, "ExpandDirection", "Microsoft.UI.Xaml.Controls.ExpandDirection");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_115_Expander_ExpandDirection;
			xamlMember.Setter = set_115_Expander_ExpandDirection;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.Expander.HeaderTemplate":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Expander");
			xamlMember = new XamlMember(this, "HeaderTemplate", "Microsoft.UI.Xaml.DataTemplate");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_116_Expander_HeaderTemplate;
			xamlMember.Setter = set_116_Expander_HeaderTemplate;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.Expander.HeaderTemplateSelector":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Expander");
			xamlMember = new XamlMember(this, "HeaderTemplateSelector", "Microsoft.UI.Xaml.Controls.DataTemplateSelector");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_117_Expander_HeaderTemplateSelector;
			xamlMember.Setter = set_117_Expander_HeaderTemplateSelector;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.Expander.TemplateSettings":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Expander");
			xamlMember = new XamlMember(this, "TemplateSettings", "Microsoft.UI.Xaml.Controls.ExpanderTemplateSettings");
			xamlMember.Getter = get_118_Expander_TemplateSettings;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Ciphra.VPN.WinUI.Pages.SplitTunnelPage.ViewModel":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Ciphra.VPN.WinUI.Pages.SplitTunnelPage");
			xamlMember = new XamlMember(this, "ViewModel", "Ciphra.VPN.Common.ViewModels.SplitTunnelPageViewModel");
			xamlMember.Getter = get_119_SplitTunnelPage_ViewModel;
			xamlMember.Setter = set_119_SplitTunnelPage_ViewModel;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Children":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Children", "System.Collections.Generic.IList`1<Microsoft.UI.Xaml.Controls.TreeViewNode>");
			xamlMember.Getter = get_120_TreeViewNode_Children;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Content":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Content", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_121_TreeViewNode_Content;
			xamlMember.Setter = set_121_TreeViewNode_Content;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Depth":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Depth", "Int32");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_122_TreeViewNode_Depth;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.HasChildren":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "HasChildren", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_123_TreeViewNode_HasChildren;
			xamlMember.SetIsReadOnly();
			break;
		}
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.HasUnrealizedChildren":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "HasUnrealizedChildren", "Boolean");
			xamlMember.Getter = get_124_TreeViewNode_HasUnrealizedChildren;
			xamlMember.Setter = set_124_TreeViewNode_HasUnrealizedChildren;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.IsExpanded":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "IsExpanded", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_125_TreeViewNode_IsExpanded;
			xamlMember.Setter = set_125_TreeViewNode_IsExpanded;
			break;
		}
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Parent":
		{
			XamlUserType xamlUserType = (XamlUserType)(object)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Parent", "Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember.Getter = get_126_TreeViewNode_Parent;
			xamlMember.SetIsReadOnly();
			break;
		}
		}
		return (IXamlMember)(object)xamlMember;
	}
}
