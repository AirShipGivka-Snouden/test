using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Ciphra.VPN.WinUI.Dialogs;
using Ciphra.VPN.WinUI.Extensions;
using Ciphra.VPN.WinUI.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Serilog;
using WinRT;
using WinRT.Ciphra_VPN_WinUIVtableClasses;

namespace Ciphra.VPN.WinUI.AppWindows;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.Markup.IComponentConnector")]
[WinRTExposedType(typeof(Ciphra_VPN_WinUI_AppWindows_PaymentWindowWinRTTypeDetails))]
public sealed class AuthWindow : Window, IComponentConnector
{
	private bool _activated;

	private AuthPage? _page;

	private string _url = string.Empty;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Grid LayoutGrid;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private bool _contentLoaded;

	public string Url
	{
		get
		{
			return _url;
		}
		set
		{
			_url = value;
			_page?.NavigateTo(_url);
		}
	}

	public AuthWindow()
	{
		InitializeComponent();
		((Window)this).Activated += AuthWindow_Activated;
	}

	private void AuthWindow_Activated(object sender, WindowActivatedEventArgs args)
	{
		if (_activated)
		{
			return;
		}
		try
		{
			((Window)this).AppWindow.SetIcon("Assets\\ciphra.ico");
			((Window)this).AppWindow.Title = Loc.Get("AuthWindowTitle");
			((Window)(object)this).SetDialogProperties(450, 700, resizable: true);
			_page = new AuthPage();
			_page.CloseHostWindow += delegate
			{
				((Window)this).Close();
			};
			((Panel)LayoutGrid).Children.Clear();
			((Panel)LayoutGrid).Children.Add((UIElement)(object)_page);
			if (!string.IsNullOrWhiteSpace(Url))
			{
				_page.NavigateTo(Url);
			}
			AppWindowPresenter presenter = ((Window)this).AppWindow.Presenter;
			OverlappedPresenter val = (OverlappedPresenter)(object)((presenter is OverlappedPresenter) ? presenter : null);
			if (val != null)
			{
				val.IsAlwaysOnTop = true;
			}
			_activated = true;
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error activating auth window");
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("ms-appx:///AppWindows/AuthWindow.xaml");
			Application.LoadComponent((object)this, uri, (ComponentResourceLocation)0);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			LayoutGrid = CastExtensions.As<Grid>(target);
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
