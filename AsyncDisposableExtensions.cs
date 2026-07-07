using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;

namespace VpnHood.Core.Toolkit.Extensions;

public static class AsyncDisposableExtensions
{
	public static async ValueTask SafeDisposeAsync(this IAsyncDisposable? disposable)
	{
		if (disposable == null)
		{
			return;
		}
		try
		{
			await disposable.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogDebug(exception, "Failed to dispose asynchronously.");
		}
		if (!(disposable is IDisposable disposable2))
		{
			return;
		}
		try
		{
			disposable2.Dispose();
		}
		catch (Exception exception2)
		{
			VhLogger.Instance.LogDebug(exception2, "Failed to dispose synchronously after async dispose failure.");
		}
	}
}
