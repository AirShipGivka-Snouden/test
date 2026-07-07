using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace Ciphra.VPN.WinUI.Extensions;

public static class MessageDialogExtension
{
	private static TaskCompletionSource<ContentDialog> _currentDialogShowRequest;

	public static async Task<ContentDialogResult> ShowAsyncQueue(this ContentDialog dialog)
	{
		while (_currentDialogShowRequest != null)
		{
			await _currentDialogShowRequest.Task;
		}
		TaskCompletionSource<ContentDialog> request = (_currentDialogShowRequest = new TaskCompletionSource<ContentDialog>());
		TaskAwaiter<ContentDialogResult> taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter<ContentDialogResult>(dialog.ShowAsync());
		if (!taskAwaiter.IsCompleted)
		{
			await taskAwaiter;
			TaskAwaiter<ContentDialogResult> taskAwaiter2 = default(TaskAwaiter<ContentDialogResult>);
			taskAwaiter = taskAwaiter2;
		}
		ContentDialogResult result = taskAwaiter.GetResult();
		ContentDialogResult result2 = result;
		_currentDialogShowRequest = null;
		request.SetResult(dialog);
		return result2;
	}
}
