using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reactive.Linq;
using Ciphra.VPN.Common.ViewModels.DialogViewModels;
using Ciphra.VPN.WinUI.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using ReactiveUI;
using WinRT;
using WinRT.Ciphra_VPN_WinUIVtableClasses;

namespace Ciphra.VPN.WinUI.Dialogs;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(Ciphra_VPN_WinUI_Dialogs_TrafficLimitDialogPageWinRTTypeDetails))]
public sealed class AuthDialogPage : Page, IViewFor<AuthDialogViewModel>, IViewFor, IActivatableView, IComponentConnector
{
	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button GenerateTokenButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBox TokenTextBox;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private StackPanel ValidationStatusStackPanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private ProgressRing LoadingRing;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private FontIcon ValidationIcon;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBlock ValidationText;

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
			ViewModel = (AuthDialogViewModel)value;
		}
	}

	public AuthDialogViewModel? ViewModel { get; set; }

	public AuthDialogPage(AuthDialogViewModel authDialogViewModel)
	{
		ViewModel = authDialogViewModel ?? throw new ArgumentNullException("authDialogViewModel");
		InitializeComponent();
		PropertyBindingMixins.Bind<AuthDialogViewModel, AuthDialogPage, string, string>(this, ViewModel, (Expression<Func<AuthDialogViewModel, string>>)((AuthDialogViewModel vm) => vm.UserToken), (Expression<Func<AuthDialogPage, string>>)((AuthDialogPage c) => c.TokenTextBox.Text), (object)null, (IBindingTypeConverter)null, (IBindingTypeConverter)null);
		ObservableExtensions.Subscribe<AuthDialogViewModel.AuthDialogState>(Observable.ObserveOn<AuthDialogViewModel.AuthDialogState>(WhenAnyMixin.WhenAnyValue<AuthDialogViewModel, AuthDialogViewModel.AuthDialogState>(ViewModel, (Expression<Func<AuthDialogViewModel, AuthDialogViewModel.AuthDialogState>>)((AuthDialogViewModel vm) => vm.State)), RxSchedulers.MainThreadScheduler), (Action<AuthDialogViewModel.AuthDialogState>)HandleDialogStateChange);
		((ButtonBase)GenerateTokenButton).Command = ViewModel.CreateToken;
	}

	private void HandleDialogStateChange(AuthDialogViewModel.AuthDialogState dialogState)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Expected O, but got Unknown
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Expected O, but got Unknown
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Expected O, but got Unknown
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Expected O, but got Unknown
		if (dialogState == AuthDialogViewModel.AuthDialogState.Initializing)
		{
			((UIElement)ValidationStatusStackPanel).Visibility = (Visibility)1;
			return;
		}
		((UIElement)ValidationStatusStackPanel).Visibility = (Visibility)0;
		if (dialogState == AuthDialogViewModel.AuthDialogState.Validating)
		{
			((UIElement)LoadingRing).Visibility = (Visibility)0;
			((UIElement)ValidationIcon).Visibility = (Visibility)1;
			((UIElement)ValidationText).Visibility = (Visibility)0;
			ValidationText.Foreground = (Brush)new SolidColorBrush(Colors.Orange);
			ValidationText.Text = Loc.Get("TokenValidating");
			return;
		}
		((UIElement)LoadingRing).Visibility = (Visibility)1;
		((UIElement)ValidationIcon).Visibility = (Visibility)0;
		if (dialogState == AuthDialogViewModel.AuthDialogState.TokenValid)
		{
			ValidationIcon.Glyph = "\ue73e";
			((IconElement)ValidationIcon).Foreground = (Brush)new SolidColorBrush(Colors.LightGreen);
			ValidationText.Text = Loc.Get("TokenValid");
			ValidationText.Foreground = (Brush)new SolidColorBrush(Colors.LightGreen);
		}
		else
		{
			ValidationIcon.Glyph = "\ue711";
			((IconElement)ValidationIcon).Foreground = (Brush)new SolidColorBrush(Colors.Red);
			ValidationText.Text = Loc.Get("TokenInvalid");
			ValidationText.Foreground = (Brush)new SolidColorBrush(Colors.Red);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("ms-appx:///Dialogs/AuthDialogPage.xaml");
			Application.LoadComponent((object)this, uri, (ComponentResourceLocation)0);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 2:
			GenerateTokenButton = CastExtensions.As<Button>(target);
			break;
		case 3:
			TokenTextBox = CastExtensions.As<TextBox>(target);
			break;
		case 4:
			ValidationStatusStackPanel = CastExtensions.As<StackPanel>(target);
			break;
		case 5:
			LoadingRing = CastExtensions.As<ProgressRing>(target);
			break;
		case 6:
			ValidationIcon = CastExtensions.As<FontIcon>(target);
			break;
		case 7:
			ValidationText = CastExtensions.As<TextBlock>(target);
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
