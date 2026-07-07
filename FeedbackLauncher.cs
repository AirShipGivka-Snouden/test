using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Ciphra.VPN.Common.App;
using Windows.ApplicationModel;
using Windows.System;

namespace Ciphra.VPN.WinUI.Services;

public class FeedbackLauncher : IFeedbackLauncher
{
	private const string FeedBackFormUrl = "https://forms.gle/K1xTyK4izVLNmSeC6";

	public async Task LaunchStoreReviewFormAsync()
	{
		try
		{
			TaskAwaiter<bool> taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter<bool>(Launcher.LaunchUriAsync(new Uri("ms-windows-store:REVIEW?PFN=" + Package.Current.Id.FamilyName)));
			if (!taskAwaiter.IsCompleted)
			{
				await taskAwaiter;
				TaskAwaiter<bool> taskAwaiter2 = default(TaskAwaiter<bool>);
				taskAwaiter = taskAwaiter2;
			}
			taskAwaiter.GetResult();
		}
		catch
		{
		}
	}

	public async Task LaunchFeedbackFormAsync()
	{
		try
		{
			TaskAwaiter<bool> taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter<bool>(Launcher.LaunchUriAsync(new Uri("https://forms.gle/K1xTyK4izVLNmSeC6")));
			if (!taskAwaiter.IsCompleted)
			{
				await taskAwaiter;
				TaskAwaiter<bool> taskAwaiter2 = default(TaskAwaiter<bool>);
				taskAwaiter = taskAwaiter2;
			}
			taskAwaiter.GetResult();
		}
		catch
		{
		}
	}
}
