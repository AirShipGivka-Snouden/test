using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;

namespace VpnHood.Core.Toolkit.Utils;

public static class TaskExtensions
{
	extension(CancellationTokenSource cancellationTokenSource)
	{
		public void TryCancel()
		{
			try
			{
				if (!cancellationTokenSource.IsCancellationRequested)
				{
					cancellationTokenSource.Cancel();
				}
			}
			catch (Exception exception)
			{
				VhLogger.Instance.LogError(exception, "Failed to cancel the CancellationTokenSource. This is not a critical error.");
			}
		}

		public async Task TryCancelAsync()
		{
			try
			{
				if (!cancellationTokenSource.IsCancellationRequested)
				{
					await cancellationTokenSource.CancelAsync().Vhc();
				}
			}
			catch (Exception exception)
			{
				VhLogger.Instance.LogError(exception, "Failed to cancel the CancellationTokenSource. This is not a critical error.");
			}
		}
	}

	public static bool DefaultContinueOnCapturedContext { get; set; }

	public static ConfiguredTaskAwaitable Vhc(this Task task)
	{
		return task.ConfigureAwait(DefaultContinueOnCapturedContext);
	}

	public static ConfiguredTaskAwaitable<T> Vhc<T>(this Task<T> task)
	{
		return task.ConfigureAwait(DefaultContinueOnCapturedContext);
	}

	public static ConfiguredValueTaskAwaitable Vhc(this ValueTask task)
	{
		return task.ConfigureAwait(DefaultContinueOnCapturedContext);
	}

	public static ConfiguredValueTaskAwaitable<T> Vhc<T>(this ValueTask<T> task)
	{
		return task.ConfigureAwait(DefaultContinueOnCapturedContext);
	}

	public static void VhBlock(this ValueTask task)
	{
		if (task.IsCompleted)
		{
			task.GetAwaiter().GetResult();
		}
		else
		{
			task.AsTask().GetAwaiter().GetResult();
		}
	}
}
