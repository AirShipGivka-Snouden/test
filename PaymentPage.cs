using System;
using System.CodeDom.Compiler;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Web;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.Web.WebView2.Core;
using Serilog;
using WinRT;
using WinRT.Ciphra_VPN_WinUIVtableClasses;
using Windows.System;

namespace Ciphra.VPN.WinUI.Dialogs;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(Ciphra_VPN_WinUI_Dialogs_TrafficLimitDialogPageWinRTTypeDetails))]
public sealed class PaymentPage : Page, IComponentConnector
{
	private const string PaymentPageUrl = "https://www.ciphravpn.com/payment";

	private const int MaxNavigationRetries = 1;

	private IAppAnalytics _analytics;

	private int _navigationRetryCount;

	private Uri _lastNavigationUri;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Grid LayoutGrid;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private WebView2 PaymentWebView2;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private StackPanel NavigationErrorPanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Button VisitWebsiteButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private bool _contentLoaded;

	public event EventHandler<Unit> CloseHostWindow;

	public PaymentPage()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		InitializeComponent();
		((FrameworkElement)this).Loaded += new RoutedEventHandler(PaymentPage_Loaded);
		PaymentWebView2.NavigationCompleted += PaymentWebView2OnNavigationCompleted;
	}

	public void NavigateTo(string url)
	{
		try
		{
			_navigationRetryCount = 0;
			_lastNavigationUri = new Uri(url);
			((UIElement)PaymentWebView2).Visibility = (Visibility)0;
			((UIElement)NavigationErrorPanel).Visibility = (Visibility)1;
			PaymentWebView2.Source = _lastNavigationUri;
			_analytics?.SendEvent("NavigatedToPaymentUrl", ("url", url));
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Failed to navigate to URL: {Url}", url);
		}
	}

	private void PaymentPage_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			_analytics = App.GetInstance<IAppAnalytics>();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to get IAppAnalytics instance");
		}
	}

	private async void VisitWebsiteButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Uri uri = _lastNavigationUri ?? new Uri("https://www.ciphravpn.com/payment");
			TaskAwaiter<bool> taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter<bool>(Launcher.LaunchUriAsync(uri));
			if (!taskAwaiter.IsCompleted)
			{
				await taskAwaiter;
				TaskAwaiter<bool> taskAwaiter2 = default(TaskAwaiter<bool>);
				taskAwaiter = taskAwaiter2;
			}
			taskAwaiter.GetResult();
			_analytics?.SendEvent("VisitWebsiteFromPaymentPage", ("url", uri.ToString()));
			this.CloseHostWindow?.Invoke(this, Unit.Default);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Failed to open website URL");
		}
	}

	private void PaymentWebView2OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Expected I4, but got Unknown
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		Log.Information<Uri, bool, CoreWebView2WebErrorStatus>("Payment WebView2 navigation completed. Url: {Url}, IsSuccess: {IsSuccess}, ErrorStatus: {ErrorStatus}", sender.Source, args.IsSuccess, args.WebErrorStatus);
		if (args.IsSuccess)
		{
			Log.Information<Uri>("Payment WebView2 navigated to: {Url}", sender.Source);
			string text = "";
			string text2 = "";
			SubscriptionType subscriptionType = SubscriptionType.Unknown;
			try
			{
				NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(sender.Source.Query);
				text = nameValueCollection["transaction_id"] ?? "";
				text2 = nameValueCollection["purchase_id"] ?? "";
				if (!string.IsNullOrEmpty(text2))
				{
					subscriptionType = ApiStoreService.TryParseSubscriptionType(text2);
				}
				Log.Information<string, string, SubscriptionType>("Parsed payment parameters: TransactionId={TransactionId}, PurchaseId={PurchaseId}, SubscriptionType={SubscriptionType}", text, text2, subscriptionType);
			}
			catch (Exception ex)
			{
				Log.Error<Uri>(ex, "Failed to parse query parameters from payment URL: {Url}", sender.Source);
			}
			if (sender.Source.AbsolutePath.Contains("success", StringComparison.OrdinalIgnoreCase))
			{
				Log.Information("Detected successful payment URL. Closing payment page.");
				if (subscriptionType != SubscriptionType.Unknown && !string.IsNullOrEmpty(text))
				{
					_analytics?.TrackSubscriptionPurchaseResult(subscriptionType, text, success: true);
				}
				else
				{
					_analytics?.SendEvent("PaymentResult", ("success", "true"));
				}
				this.CloseHostWindow?.Invoke(this, Unit.Default);
			}
			if (sender.Source.AbsolutePath.Contains("failure", StringComparison.OrdinalIgnoreCase))
			{
				Log.Information("Detected failed payment URL. Closing payment page.");
				if (subscriptionType != SubscriptionType.Unknown && !string.IsNullOrEmpty(text))
				{
					_analytics?.TrackSubscriptionPurchaseResult(subscriptionType, text, success: false);
				}
				else
				{
					_analytics?.SendEvent("PaymentResult", ("success", "false"));
				}
				this.CloseHostWindow?.Invoke(this, Unit.Default);
			}
		}
		else
		{
			CoreWebView2WebErrorStatus webErrorStatus = args.WebErrorStatus;
			Exception exception = new Exception($"WebView2 navigation failed with error: {webErrorStatus}");
			_analytics?.SendException(exception, "PaymentWebView2OnNavigationCompleted");
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
			if (flag && _navigationRetryCount < 1)
			{
				_navigationRetryCount++;
				Log.Information<int, CoreWebView2WebErrorStatus>("Retrying payment navigation (attempt {Attempt}) after {Error}", _navigationRetryCount, webErrorStatus);
				sender.Source = _lastNavigationUri ?? new Uri("https://www.ciphravpn.com/payment");
			}
			else
			{
				Log.Warning<CoreWebView2WebErrorStatus>("Payment WebView2 navigation failed permanently: {Error}", webErrorStatus);
				((UIElement)PaymentWebView2).Visibility = (Visibility)1;
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
			Uri uri = new Uri("ms-appx:///Dialogs/PaymentPage.xaml");
			Application.LoadComponent((object)this, uri, (ComponentResourceLocation)0);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		switch (connectionId)
		{
		case 2:
			LayoutGrid = CastExtensions.As<Grid>(target);
			break;
		case 3:
			PaymentWebView2 = CastExtensions.As<WebView2>(target);
			break;
		case 4:
			NavigationErrorPanel = CastExtensions.As<StackPanel>(target);
			break;
		case 5:
			VisitWebsiteButton = CastExtensions.As<Button>(target);
			((ButtonBase)VisitWebsiteButton).Click += new RoutedEventHandler(VisitWebsiteButton_Click);
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
