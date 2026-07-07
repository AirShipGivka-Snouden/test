using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
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
using Windows.System;

namespace Ciphra.VPN.WinUI.Dialogs;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(Ciphra_VPN_WinUI_Dialogs_TrafficLimitDialogPageWinRTTypeDetails))]
public sealed class TrialDialogPage : Page, IViewFor<TrialDialogViewModel>, IViewFor, IActivatableView, IComponentConnector
{
	private const string PaymentPageUrl = "https://www.ciphravpn.com/payment";

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private StackPanel ActiveTrialView;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private StackPanel ExpiredTrialView;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button VisitWebsiteButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBox UserIdTextBox;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button CopyToClipboardButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock DaysLeftTextBlock;

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
			ViewModel = (TrialDialogViewModel)value;
		}
	}

	public TrialDialogViewModel? ViewModel { get; set; }

	public TrialDialogPage(TrialDialogViewModel viewModel)
	{
		ViewModel = viewModel ?? throw new ArgumentNullException("viewModel");
		InitializeComponent();
		ViewForMixins.WhenActivated((IActivatableView)(object)this, (Action<Action<IDisposable>>)delegate(Action<IDisposable> d)
		{
			d((IDisposable)PropertyBindingMixins.OneWayBind<TrialDialogViewModel, TrialDialogPage, bool, Visibility>(this, ViewModel, (Expression<Func<TrialDialogViewModel, bool>>)((TrialDialogViewModel vm) => vm.IsTrialExpired), (Expression<Func<TrialDialogPage, Visibility>>)((TrialDialogPage v) => ((UIElement)v.ActiveTrialView).Visibility), (Func<bool, Visibility>)((bool isExpired) => (Visibility)(isExpired ? 1 : 0))));
			d((IDisposable)PropertyBindingMixins.OneWayBind<TrialDialogViewModel, TrialDialogPage, bool, Visibility>(this, ViewModel, (Expression<Func<TrialDialogViewModel, bool>>)((TrialDialogViewModel vm) => vm.IsTrialExpired), (Expression<Func<TrialDialogPage, Visibility>>)((TrialDialogPage v) => ((UIElement)v.ExpiredTrialView).Visibility), (Func<bool, Visibility>)((bool isExpired) => (Visibility)(!isExpired))));
			d((IDisposable)PropertyBindingMixins.OneWayBind<TrialDialogViewModel, TrialDialogPage, string, string>(this, ViewModel, (Expression<Func<TrialDialogViewModel, string>>)((TrialDialogViewModel vm) => vm.TrialDaysLeftText), (Expression<Func<TrialDialogPage, string>>)((TrialDialogPage v) => v.DaysLeftTextBlock.Text), (object)null, (IBindingTypeConverter)null));
			d((IDisposable)PropertyBindingMixins.OneWayBind<TrialDialogViewModel, TrialDialogPage, string, string>(this, ViewModel, (Expression<Func<TrialDialogViewModel, string>>)((TrialDialogViewModel vm) => vm.AccountInfo.UserToken), (Expression<Func<TrialDialogPage, string>>)((TrialDialogPage v) => v.UserIdTextBox.Text), (object)null, (IBindingTypeConverter)null));
		});
	}

	private async void VisitWebsiteButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			TaskAwaiter<bool> taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter<bool>(Launcher.LaunchUriAsync(new Uri("https://www.ciphravpn.com/payment")));
			if (!taskAwaiter.IsCompleted)
			{
				await taskAwaiter;
				TaskAwaiter<bool> taskAwaiter2 = default(TaskAwaiter<bool>);
				taskAwaiter = taskAwaiter2;
			}
			taskAwaiter.GetResult();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Failed to open website URL");
		}
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
			Uri uri = new Uri("ms-appx:///Dialogs/TrialDialogPage.xaml");
			Application.LoadComponent((object)this, uri, (ComponentResourceLocation)0);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		switch (connectionId)
		{
		case 2:
			ActiveTrialView = CastExtensions.As<StackPanel>(target);
			break;
		case 3:
			ExpiredTrialView = CastExtensions.As<StackPanel>(target);
			break;
		case 4:
			VisitWebsiteButton = CastExtensions.As<Button>(target);
			((ButtonBase)VisitWebsiteButton).Click += new RoutedEventHandler(VisitWebsiteButton_Click);
			break;
		case 5:
			UserIdTextBox = CastExtensions.As<TextBox>(target);
			break;
		case 6:
			CopyToClipboardButton = CastExtensions.As<Button>(target);
			((ButtonBase)CopyToClipboardButton).Click += new RoutedEventHandler(CopyToClipboardButton_OnClick);
			break;
		case 7:
			DaysLeftTextBlock = CastExtensions.As<TextBlock>(target);
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
