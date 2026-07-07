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
using WinRT;
using WinRT.Ciphra_VPN_WinUIVtableClasses;

namespace Ciphra.VPN.WinUI.Dialogs;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(Ciphra_VPN_WinUI_Dialogs_TrafficLimitDialogPageWinRTTypeDetails))]
public sealed class ErrorDialogPage : Page, IViewFor<ErrorDialogViewModel>, IViewFor, IActivatableView, IComponentConnector
{
	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private TextBox ExceptionDialogMessageTextBox;

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
			ViewModel = (ErrorDialogViewModel)value;
		}
	}

	public ErrorDialogViewModel? ViewModel { get; set; }

	public ErrorDialogPage(ErrorDialogViewModel errorDialogViewModel)
	{
		ViewModel = errorDialogViewModel ?? throw new ArgumentNullException("errorDialogViewModel");
		InitializeComponent();
		PropertyBindingMixins.OneWayBind<ErrorDialogViewModel, ErrorDialogPage, string, string>(this, ViewModel, (Expression<Func<ErrorDialogViewModel, string>>)((ErrorDialogViewModel vm) => vm.ErrorMessage), (Expression<Func<ErrorDialogPage, string>>)((ErrorDialogPage v) => v.ExceptionDialogMessageTextBox.Text), (object)null, (IBindingTypeConverter)null);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("ms-appx:///Dialogs/ErrorDialogPage.xaml");
			Application.LoadComponent((object)this, uri, (ComponentResourceLocation)0);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			ExceptionDialogMessageTextBox = CastExtensions.As<TextBox>(target);
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
