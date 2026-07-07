using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Hex.HexConvertors.Extensions;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public class BlockchainRelayResolver
{
	private static readonly string[] DefaultRpcEndpoints = new string[3] { "https://eth-sepolia.g.alchemy.com/v2/55BElrOL6VXIsDw9_NWJn", "https://rpc.sepolia.org", "https://sepolia.drpc.org" };

	private const string ContractAddress = "0x51cc67318635d8f34F617c91024Dc357901A81eD";

	private const string GetEndpointsSelector = "0x5d7c83f1";

	private static readonly TimeSpan RpcTimeout = TimeSpan.FromSeconds(10L);

	internal HttpClient? TestHttpClient { get; set; }

	public async Task<List<string>> GetRelayEndpointsAsync(CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		Exception lastException = null;
		for (int i = 0; i < DefaultRpcEndpoints.Length; i++)
		{
			ct.ThrowIfCancellationRequested();
			string rpcUrl = DefaultRpcEndpoints[i];
			try
			{
				Log.Information<int, string>("Attempting blockchain relay resolution via RPC {Index}: {Url}", i, rpcUrl);
				using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
				cts.CancelAfter(RpcTimeout);
				List<string> result = await CallGetEndpointsAsync(rpcUrl, cts.Token);
				Log.Information<int, int>("Blockchain relay resolution succeeded via RPC {Index}. Got {Count} endpoints.", i, result.Count);
				return result;
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex2)
			{
				lastException = ex2;
				Log.Warning<int, string, string>(ex2, "RPC node {Index} ({Url}) failed: {Message}", i, rpcUrl, ex2.Message);
			}
		}
		throw new HttpRequestException($"All {DefaultRpcEndpoints.Length} RPC nodes failed for blockchain relay resolution.", lastException);
	}

	private async Task<List<string>> CallGetEndpointsAsync(string rpcUrl, CancellationToken ct)
	{
		string requestBody = JsonSerializer.Serialize(new
		{
			jsonrpc = "2.0",
			method = "eth_call",
			@params = new object[2]
			{
				new
				{
					to = "0x51cc67318635d8f34F617c91024Dc357901A81eD",
					data = "0x5d7c83f1"
				},
				"latest"
			},
			id = 1
		});
		bool disposeClient = TestHttpClient == null;
		HttpClient httpClient = TestHttpClient ?? new HttpClient();
		StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
		try
		{
			HttpResponseMessage response = await httpClient.PostAsync(rpcUrl, content, ct);
			response.EnsureSuccessStatusCode();
			using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
			if (doc.RootElement.TryGetProperty("error", out var error))
			{
				throw new Exception($"RPC error: {error}");
			}
			string resultHex = doc.RootElement.GetProperty("result").GetString() ?? throw new Exception("No result in RPC response");
			return DecodeStringArray(resultHex);
		}
		finally
		{
			if (disposeClient)
			{
				httpClient.Dispose();
			}
		}
	}

	internal static List<string> DecodeStringArray(string hexData)
	{
		byte[] bytes = HexByteConvertorExtensions.HexToByteArray(hexData);
		if (bytes.Length < 32)
		{
			throw new FormatException($"ABI response too short ({bytes.Length} bytes). Expected at least 32.");
		}
		int num = ReadUint(0);
		int num2 = ReadUint(num);
		if (num2 < 0 || num2 > 10000)
		{
			throw new FormatException($"ABI array length {num2} is unreasonable.");
		}
		int num3 = num + 32;
		List<string> list = new List<string>(num2);
		for (int i = 0; i < num2; i++)
		{
			int num4 = ReadUint(num3 + i * 32);
			int num5 = num3 + num4;
			int num6 = ReadUint(num5);
			if (num6 < 0 || num5 + 32 + num6 > bytes.Length)
			{
				throw new FormatException($"ABI string at index {i} has invalid length {num6}.");
			}
			string item = Encoding.UTF8.GetString(bytes, num5 + 32, num6);
			list.Add(item);
		}
		return list;
		int ReadUint(int offset)
		{
			if (offset + 32 > bytes.Length)
			{
				throw new FormatException($"ABI read out of bounds at offset {offset} (data length: {bytes.Length}).");
			}
			byte[] array = new byte[32];
			Array.Copy(bytes, offset, array, 0, 32);
			return (array[28] << 24) | (array[29] << 16) | (array[30] << 8) | array[31];
		}
	}
}
