using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Linq.Expressions;
using Ciphra.VPN.Common.ViewModels.DialogViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using ReactiveUI;
using Serilog;
using WinRT;
using WinRT.Ciphra_VPN_WinUIVtableClasses;
using Windows.ApplicationModel.DataTransfer;

namespace Ciphra.VPN.WinUI.Dialogs;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(Ciphra_VPN_WinUI_Dialogs_TrafficLimitDialogPageWinRTTypeDetails))]
public sealed class SubscribeDialogPage : Page, IViewFor<SubscriptionDialogViewModel>, IViewFor, IActivatableView, IComponentConnector
{
	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button Sub12MonthButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock TwelveMonthsRun;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button Sub3MonthButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock ThreeMonthsRun;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button Sub1MonthButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock OneMonthRun;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBox UserIdTextBox;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button CopyToClipboardButton;

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
			ViewModel = (SubscriptionDialogViewModel)value;
		}
	}

	public SubscriptionDialogViewModel? ViewModel { get; set; }

	public SubscribeDialogPage(SubscriptionDialogViewModel viewModel)
	{
		ViewModel = viewModel ?? throw new ArgumentNullException("viewModel");
		InitializeComponent();
		PropertyBindingMixins.OneWayBind<SubscriptionDialogViewModel, SubscribeDialogPage, string, string>(this, ViewModel, (Expression<Func<SubscriptionDialogViewModel, string>>)((SubscriptionDialogViewModel vm) => vm.SubscriptionManager.Price1), (Expression<Func<SubscribeDialogPage, string>>)((SubscribeDialogPage c) => c.OneMonthRun.Text), (object)null, (IBindingTypeConverter)null);
		PropertyBindingMixins.OneWayBind<SubscriptionDialogViewModel, SubscribeDialogPage, string, string>(this, ViewModel, (Expression<Func<SubscriptionDialogViewModel, string>>)((SubscriptionDialogViewModel vm) => vm.SubscriptionManager.Price3), (Expression<Func<SubscribeDialogPage, string>>)((SubscribeDialogPage c) => c.ThreeMonthsRun.Text), (object)null, (IBindingTypeConverter)null);
		PropertyBindingMixins.OneWayBind<SubscriptionDialogViewModel, SubscribeDialogPage, string, string>(this, ViewModel, (Expression<Func<SubscriptionDialogViewModel, string>>)((SubscriptionDialogViewModel vm) => vm.SubscriptionManager.Price12), (Expression<Func<SubscribeDialogPage, string>>)((SubscribeDialogPage c) => c.TwelveMonthsRun.Text), (object)null, (IBindingTypeConverter)null);
		PropertyBindingMixins.OneWayBind<SubscriptionDialogViewModel, SubscribeDialogPage, string, string>(this, ViewModel, (Expression<Func<SubscriptionDialogViewModel, string>>)((SubscriptionDialogViewModel vm) => vm.AccountInfo.UserToken), (Expression<Func<SubscribeDialogPage, string>>)((SubscribeDialogPage c) => c.UserIdTextBox.Text), (object)null, (IBindingTypeConverter)null);
		((ButtonBase)Sub1MonthButton).Command = ViewModel.SubscriptionManager.Buy1;
		((ButtonBase)Sub3MonthButton).Command = ViewModel.SubscriptionManager.Buy3;
		((ButtonBase)Sub12MonthButton).Command = ViewModel.SubscriptionManager.Buy12;
	}

	private void CopyToClipboardButton_OnClick(object sender, RoutedEventArgs e)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		try
		{
			if (ViewModel?.AccountInfo.UserToken != null)
			{
				DataPackage val = new DataPackage();
				val.SetText(ViewModel.AccountInfo.UserToken);
				Clipboard.SetContent(val);
			}
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Error copying User ID to clipboard: {Message}", ex.Message);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("ms-appx:///Dialogs/SubscribeDialogPage.xaml");
			Application.LoadComponent((object)this, uri, (ComponentResourceLocation)0);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Expected O, but got Unknown
		switch (connectionId)
		{
		case 2:
			Sub12MonthButton = CastExtensions.As<Button>(target);
			break;
		case 3:
			TwelveMonthsRun = CastExtensions.As<TextBlock>(target);
			break;
		case 4:
			Sub3MonthButton = CastExtensions.As<Button>(target);
			break;
		case 5:
			ThreeMonthsRun = CastExtensions.As<TextBlock>(target);
			break;
		case 6:
			Sub1MonthButton = CastExtensions.As<Button>(target);
			break;
		case 7:
			OneMonthRun = CastExtensions.As<TextBlock>(target);
			break;
		case 8:
			UserIdTextBox = CastExtensions.As<TextBox>(target);
			break;
		case 9:
			CopyToClipboardButton = CastExtensions.As<Button>(target);
			((ButtonBase)CopyToClipboardButton).Click += new RoutedEventHandler(CopyToClipboardButton_OnClick);
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
