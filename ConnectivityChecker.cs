using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public static class ConnectivityChecker
{
	private static readonly (string Url, HttpStatusCode Expected)[] ConnectivityEndpoints = new(string, HttpStatusCode)[2]
	{
		("http://clients3.google.com/generate_204", HttpStatusCode.NoContent),
		("http://www.msftconnecttest.com/connecttest.txt", HttpStatusCode.OK)
	};

	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5L);

	public static async Task<bool> IsInternetAvailableAsync(CancellationToken ct, HttpClient? httpClient = null)
	{
		bool disposeClient = httpClient == null;
		if (httpClient == null)
		{
			httpClient = new HttpClient();
		}
		try
		{
			(string Url, HttpStatusCode Expected)[] connectivityEndpoints = ConnectivityEndpoints;
			for (int i = 0; i < connectivityEndpoints.Length; i++)
			{
				var (url, expectedStatus) = connectivityEndpoints[i];
				try
				{
					using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
					cts.CancelAfter(Timeout);
					if ((await httpClient.GetAsync(url, cts.Token)).StatusCode == expectedStatus)
					{
						return true;
					}
				}
				catch (Exception ex) when (!(ex is OperationCanceledException) || !ct.IsCancellationRequested)
				{
					Log.Debug<string, string>(ex, "Connectivity check to {Url} failed: {Message}", url, ex.Message);
				}
			}
			return false;
		}
		finally
		{
			if (disposeClient)
			{
				httpClient.Dispose();
			}
		}
	}
}
