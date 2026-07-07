using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
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
public sealed class RateUsDialogPage : Page, IViewFor<RateUsDialogViewModel>, IViewFor, IActivatableView, IComponentConnector
{
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
			ViewModel = (RateUsDialogViewModel)value;
		}
	}

	public RateUsDialogViewModel? ViewModel { get; set; }

	public RateUsDialogPage(RateUsDialogViewModel viewModel)
	{
		ViewModel = viewModel ?? throw new ArgumentNullException("viewModel");
		InitializeComponent();
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("ms-appx:///Dialogs/RateUsDialogPage.xaml");
			Application.LoadComponent((object)this, uri, (ComponentResourceLocation)0);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		_contentLoaded = true;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public IComponentConnector GetBindingConnector(int connectionId, object target)
	{
		return null;
	}
}
