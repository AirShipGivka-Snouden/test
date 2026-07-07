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

public class WinUiOAuthLoginLauncher : IOAuthLoginLauncher
{
	public async Task LaunchLoginAsync(string authorizeUrl, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(authorizeUrl) || cancellationToken.IsCancellationRequested)
		{
			return;
		}
		try
		{
			if (!ShowAuthWindow(authorizeUrl))
			{
				Uri uri = new Uri(authorizeUrl);
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
			Log.Error(ex, "Failed to launch OAuth login URL");
			throw;
		}
	}

	private static bool ShowAuthWindow(string url)
	{
		try
		{
			Log.Information("Opening auth window");
			AuthWindow authWindow = new AuthWindow();
			((Window)authWindow).Activate();
			WindowExtensions.Show((Window)(object)authWindow, true);
			WindowExtensions.ShowInTaskbar((Window)(object)authWindow);
			authWindow.Url = url;
			return true;
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error showing auth window");
			return false;
		}
	}
}
