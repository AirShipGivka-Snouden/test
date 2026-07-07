using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.Services.Interfaces;
using Ciphra.VPN.WinUI.AppWindows;
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.System;

namespace Ciphra.VPN.WinUI.Services;

public class WinUiStripeUrlLauncher : IStripeUrlLauncher
{
	public async Task<bool> LaunchStripeUrlAsync(string url, CancellationToken cancellationToken = default(CancellationToken))
	{
		try
		{
			if (!ShowPaymentWindow(url))
			{
				Uri uri = new Uri(url);
				TaskAwaiter<bool> taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter<bool>(Launcher.LaunchUriAsync(uri));
				if (!taskAwaiter.IsCompleted)
				{
					await taskAwaiter;
					TaskAwaiter<bool> taskAwaiter2 = default(TaskAwaiter<bool>);
					taskAwaiter = taskAwaiter2;
				}
				taskAwaiter.GetResult();
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error<string>(ex2, "Failed to launch Stripe URL: {Url}", url);
		}
		return false;
	}

	private bool ShowPaymentWindow(string url)
	{
		try
		{
			Log.Information("Opening payment window");
			PaymentWindow paymentWindow = new PaymentWindow();
			((Window)paymentWindow).Activate();
			WindowExtensions.Show((Window)(object)paymentWindow, true);
			WindowExtensions.ShowInTaskbar((Window)(object)paymentWindow);
			paymentWindow.Url = url;
			return true;
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error showing payment window");
		}
		return false;
	}
}
