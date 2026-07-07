using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public class ExternalIpService
{
	private const string CheckIpUrl = "https://checkip.amazonaws.com/";

	public async Task<string> GetExternalIp(CancellationToken ct = default(CancellationToken))
	{
		try
		{
			return await HttpRetryPolicy.ExecuteAsync(async delegate(CancellationToken innerCt)
			{
				using HttpClient httpClient = new HttpClient();
				return (await httpClient.GetStringAsync("https://checkip.amazonaws.com/", innerCt)).Trim();
			}, ct, 3, 2.0, "External IP check");
		}
		catch (OperationCanceledException)
		{
			return "Unknown";
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			Log.Debug(ex3, "Failed to get external IP after retries");
			return "Unknown";
		}
	}
}
