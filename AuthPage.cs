using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Ciphra.VPN.Common.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.Web.WebView2.Core;
using Serilog;
using WinRT;
using WinRT.Ciphra_VPN_WinUIVtableClasses;

namespace Ciphra.VPN.WinUI.Dialogs;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(Ciphra_VPN_WinUI_Dialogs_TrafficLimitDialogPageWinRTTypeDetails))]
public sealed class AuthPage : Page, IComponentConnector
{
	private const string CallbackScheme = "ciphra://";

	private const int MaxNavigationRetries = 1;

	private int _navigationRetryCount;

	private Uri? _lastNavigationUri;

	private readonly Task _initTask;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Grid LayoutGrid;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private WebView2 AuthWebView2;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private StackPanel NavigationErrorPanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private bool _contentLoaded;

	public event EventHandler<Unit>? CloseHostWindow;

	public AuthPage()
	{
		InitializeComponent();
		AuthWebView2.NavigationCompleted += AuthWebView2_NavigationCompleted;
		_initTask = InitializeCoreWebView2Async();
	}

	private async Task InitializeCoreWebView2Async()
	{
		try
		{
			TaskAwaiter taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter(AuthWebView2.EnsureCoreWebView2Async());
			if (!taskAwaiter.IsCompleted)
			{
				await taskAwaiter;
				TaskAwaiter taskAwaiter2 = default(TaskAwaiter);
				taskAwaiter = taskAwaiter2;
			}
			taskAwaiter.GetResult();
			AuthWebView2.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Failed to initialize CoreWebView2 / hook NavigationStarting");
		}
	}

	private void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(args.Uri) || !args.Uri.StartsWith("ciphra://", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		Log.Information("AuthPage intercepted ciphra:// callback in WebView");
		args.Cancel = true;
		try
		{
			App.GetInstance<OAuthAccountViewModel>()?.HandleCallback(args.Uri);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to dispatch OAuth callback from WebView");
		}
		finally
		{
			this.CloseHostWindow?.Invoke(this, Unit.Default);
		}
	}

	public async void NavigateTo(string url)
	{
		try
		{
			_navigationRetryCount = 0;
			_lastNavigationUri = new Uri(url);
			((UIElement)AuthWebView2).Visibility = (Visibility)0;
			((UIElement)NavigationErrorPanel).Visibility = (Visibility)1;
			await _initTask.ConfigureAwait(continueOnCapturedContext: true);
			TaskAwaiter taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter(AuthWebView2.EnsureCoreWebView2Async());
			if (!taskAwaiter.IsCompleted)
			{
				await taskAwaiter;
				TaskAwaiter taskAwaiter2 = default(TaskAwaiter);
				taskAwaiter = taskAwaiter2;
			}
			taskAwaiter.GetResult();
			AuthWebView2.Source = _lastNavigationUri;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Failed to navigate to auth URL");
		}
	}

	private void AuthWebView2_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected I4, but got Unknown
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		Log.Information<bool, CoreWebView2WebErrorStatus>("Auth WebView2 navigation completed. IsSuccess: {IsSuccess}, ErrorStatus: {ErrorStatus}", args.IsSuccess, args.WebErrorStatus);
		if (!args.IsSuccess)
		{
			CoreWebView2WebErrorStatus webErrorStatus = args.WebErrorStatus;
			bool flag;
			switch (webErrorStatus - 7)
			{
			case 0:
			case 2:
			case 3:
			case 6:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag && _navigationRetryCount < 1 && _lastNavigationUri != null)
			{
				_navigationRetryCount++;
				Log.Information<int, CoreWebView2WebErrorStatus>("Retrying auth navigation (attempt {Attempt}) after {Error}", _navigationRetryCount, args.WebErrorStatus);
				sender.Source = _lastNavigationUri;
			}
			else
			{
				Log.Warning<CoreWebView2WebErrorStatus>("Auth WebView2 navigation failed permanently: {Error}", args.WebErrorStatus);
				((UIElement)AuthWebView2).Visibility = (Visibility)1;
				((UIElement)NavigationErrorPanel).Visibility = (Visibility)0;
			}
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("ms-appx:///Dialogs/AuthPage.xaml");
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
			LayoutGrid = CastExtensions.As<Grid>(target);
			break;
		case 3:
			AuthWebView2 = CastExtensions.As<WebView2>(target);
			break;
		case 4:
			NavigationErrorPanel = CastExtensions.As<StackPanel>(target);
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
