using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Ciphra.VPN.Common.Services;

internal static class HttpRetryPolicy
{
	private static bool IsTransient(Exception ex)
	{
		return ex is HttpRequestException { StatusCode: var statusCode } ex2 && (!statusCode.HasValue || ex2.StatusCode.Value >= HttpStatusCode.InternalServerError);
	}

	public static async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct, int maxRetries = 3, double baseDelaySeconds = 2.0, string operationName = "HTTP request")
	{
		Exception lastException = null;
		for (int attempt = 0; attempt <= maxRetries; attempt++)
		{
			if (attempt > 0)
			{
				TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(baseDelaySeconds, attempt));
				Log.Warning("Retrying {OperationName} (attempt {Attempt}/{MaxRetries}) after {Delay}s...", new object[4] { operationName, attempt, maxRetries, delay.TotalSeconds });
				await Task.Delay(delay, ct);
			}
			try
			{
				return await operation(ct);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex2) when (IsTransient(ex2))
			{
				lastException = ex2;
				Log.Warning(ex2, "{OperationName} attempt {Attempt} of {Total} failed: {Message}", new object[4]
				{
					operationName,
					attempt + 1,
					maxRetries + 1,
					ex2.Message
				});
			}
		}
		throw new HttpRequestException($"{operationName} failed after {maxRetries} retries.", lastException);
	}
}
