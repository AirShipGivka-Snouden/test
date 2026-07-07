using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Hex.HexConvertors.Extensions;
using Serilog;

namespace Ciphra.VPN.Common.Services.OnChain;

public class OnChainBundlePoller : IOnChainBundlePoller
{
	private static readonly HttpClient SharedHttpClient = CreatePinnedHttpClient();

	internal HttpClient? TestHttpClient { get; set; }

	private static HttpClient CreatePinnedHttpClient()
	{
		HttpClientHandler handler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = delegate(HttpRequestMessage message, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors sslErrors)
			{
				if (sslErrors == SslPolicyErrors.None)
				{
					return true;
				}
				if (cert != null)
				{
					string item = Convert.ToHexString(cert.GetCertHash(HashAlgorithmName.SHA256));
					if (OnChainConfig.BundlerCertFingerprints.Contains(item))
					{
						return true;
					}
				}
				return false;
			}
		};
		return new HttpClient(handler);
	}

	public async Task<OnChainRpcResult<string>> TryGetEncryptedIpnsNameAsync(byte[] encryptedDeviceId, CancellationToken ct)
	{
		string data = "0x9d1606af" + HexByteConvertorExtensions.ToHex(encryptedDeviceId, false).PadRight(64, '0');
		string[] rpcEndpoints = OnChainConfig.RpcEndpoints;
		foreach (string rpcUrl in rpcEndpoints)
		{
			ct.ThrowIfCancellationRequested();
			try
			{
				string ipnsName = DecodeAbiString(await EthCallAsync(rpcUrl, "0x05a247D8345696cb194a9a4C4F3788b385B72E66", data, ct));
				return string.IsNullOrEmpty(ipnsName) ? OnChainRpcResult<string>.NotFound : OnChainRpcResult<string>.Of(ipnsName);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex2) when (ex2.Message.Contains("device not found"))
			{
				return OnChainRpcResult<string>.NotFound;
			}
			catch (Exception ex3)
			{
				Log.Warning<string>(ex3, "getIpnsName RPC call failed on {Url}", rpcUrl);
			}
		}
		return OnChainRpcResult<string>.Unavailable;
	}

	public async Task<string?> GetIpnsNameAsync(byte[] encryptedDeviceId, CancellationToken ct)
	{
		OnChainRpcResult<string> r = await TryGetEncryptedIpnsNameAsync(encryptedDeviceId, ct);
		return r.IsFound ? r.Value : null;
	}

	public async Task<string?> GetDecryptedIpnsNameAsync(byte[] encryptedDeviceId, byte[] x25519PrivateKey, CancellationToken ct)
	{
		string encryptedIpns = await GetIpnsNameAsync(encryptedDeviceId, ct);
		if (encryptedIpns == null)
		{
			return null;
		}
		try
		{
			return BundleDecryptor.DecryptIpnsName(encryptedIpns, x25519PrivateKey);
		}
		catch (CryptographicException ex)
		{
			Log.Warning((Exception)ex, "Failed to decrypt IPNS name — may be encrypted with old key");
			return null;
		}
	}

	public async Task<OnChainRpcResult<byte[]>> TryGetDevicePubKeyAsync(byte[] encryptedDeviceId, CancellationToken ct)
	{
		string data = "0xa33279c1" + HexByteConvertorExtensions.ToHex(encryptedDeviceId, false).PadRight(64, '0');
		string[] rpcEndpoints = OnChainConfig.RpcEndpoints;
		foreach (string rpcUrl in rpcEndpoints)
		{
			ct.ThrowIfCancellationRequested();
			try
			{
				string result = await EthCallAsync(rpcUrl, "0x05a247D8345696cb194a9a4C4F3788b385B72E66", data, ct);
				string hex = (result.StartsWith("0x") ? result.Substring(2) : result);
				if (hex.Length < 64)
				{
					Log.Warning<int, string>("getDevice returned short hex ({Len}) on {Url}; trying next RPC.", hex.Length, rpcUrl);
					continue;
				}
				string pubKeyHex = hex.Substring(0, 64);
				if (pubKeyHex == new string('0', 64))
				{
					return OnChainRpcResult<byte[]>.NotFound;
				}
				return OnChainRpcResult<byte[]>.Of(HexByteConvertorExtensions.HexToByteArray(pubKeyHex));
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex2) when (ex2.Message.Contains("device not found"))
			{
				return OnChainRpcResult<byte[]>.NotFound;
			}
			catch (Exception ex3)
			{
				Log.Warning<string>(ex3, "getDevice RPC call failed on {Url}", rpcUrl);
			}
		}
		return OnChainRpcResult<byte[]>.Unavailable;
	}

	public async Task<byte[]?> GetDevicePubKeyAsync(byte[] encryptedDeviceId, CancellationToken ct)
	{
		OnChainRpcResult<byte[]> r = await TryGetDevicePubKeyAsync(encryptedDeviceId, ct);
		return r.IsFound ? r.Value : null;
	}

	public async Task<OnChainRpcResult<bool>> TryIsRevokedAsync(byte[] encryptedDeviceId, CancellationToken ct)
	{
		string data = "0xe28404b4" + HexByteConvertorExtensions.ToHex(encryptedDeviceId, false).PadRight(64, '0');
		string[] rpcEndpoints = OnChainConfig.RpcEndpoints;
		foreach (string rpcUrl in rpcEndpoints)
		{
			ct.ThrowIfCancellationRequested();
			try
			{
				return OnChainRpcResult<bool>.Of(DecodeBool(await EthCallAsync(rpcUrl, "0x05a247D8345696cb194a9a4C4F3788b385B72E66", data, ct)));
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex2)
			{
				Log.Warning<string>(ex2, "isRevoked RPC call failed on {Url}", rpcUrl);
			}
		}
		return OnChainRpcResult<bool>.Unavailable;
	}

	public async Task<bool> IsRevokedAsync(byte[] encryptedDeviceId, CancellationToken ct)
	{
		OnChainRpcResult<bool> r = await TryIsRevokedAsync(encryptedDeviceId, ct);
		return r.IsFound && r.Value;
	}

	public async Task<byte[]?> FetchBundleAsync(string ipnsName, CancellationToken ct)
	{
		string[] ipfsGateways = OnChainConfig.IpfsGateways;
		foreach (string gateway in ipfsGateways)
		{
			ct.ThrowIfCancellationRequested();
			try
			{
				string url = gateway.TrimEnd('/') + "/ipns/" + ipnsName;
				Log.Information<string>("Fetching bundle from IPFS: {Url}", url);
				using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
				cts.CancelAfter(OnChainConfig.IpfsTimeout);
				HttpClient httpClient = TestHttpClient ?? SharedHttpClient;
				HttpResponseMessage response = await httpClient.GetAsync(url, cts.Token);
				response.EnsureSuccessStatusCode();
				byte[] bytes = await response.Content.ReadAsByteArrayAsync(cts.Token);
				if (bytes.Length < 60)
				{
					Log.Warning<string, int>("Bundle from {Gateway} too small ({Size} bytes), skipping.", gateway, bytes.Length);
					continue;
				}
				Log.Information<string, int>("Bundle fetched successfully from {Gateway} ({Size} bytes).", gateway, bytes.Length);
				return bytes;
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex2)
			{
				Log.Warning<string, string>(ex2, "IPFS gateway {Gateway} failed: {Message}", gateway, ex2.Message);
			}
		}
		return null;
	}

	public async Task<(byte[] EncryptedBundle, string IpnsName)> WaitForBundleAsync(byte[] encryptedDeviceId, byte[] x25519PrivateKey, CancellationToken ct)
	{
		string previousEncryptedIpns = null;
		for (int attempt = 0; attempt < 60; attempt++)
		{
			ct.ThrowIfCancellationRequested();
			string encryptedIpns = await GetIpnsNameAsync(encryptedDeviceId, ct);
			if (encryptedIpns != null && encryptedIpns != previousEncryptedIpns)
			{
				previousEncryptedIpns = encryptedIpns;
				try
				{
					string ipnsName = BundleDecryptor.DecryptIpnsName(encryptedIpns, x25519PrivateKey);
					byte[] bundle = await FetchBundleAsync(ipnsName, ct);
					if (bundle != null)
					{
						return (EncryptedBundle: bundle, IpnsName: ipnsName);
					}
				}
				catch (CryptographicException ex)
				{
					CryptographicException ex2 = ex;
					Log.Debug((Exception)ex2, "IPNS name decrypt failed, server may not have re-encrypted yet");
				}
			}
			Log.Information<int, int, double>("Bundle not ready yet (attempt {Attempt}/{Max}). Waiting {Seconds}s...", attempt + 1, 60, OnChainConfig.BundlePollInterval.TotalSeconds);
			await Task.Delay(OnChainConfig.BundlePollInterval, ct);
		}
		throw new TimeoutException($"On-chain bundle not available after {60} attempts.");
	}

	internal async Task<string> EthCallAsync(string rpcUrl, string contractAddress, string data, CancellationToken ct)
	{
		string requestBody = JsonSerializer.Serialize(new
		{
			jsonrpc = "2.0",
			method = "eth_call",
			@params = new object[2]
			{
				new
				{
					to = contractAddress,
					data = (data.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? data : ("0x" + data))
				},
				"latest"
			},
			id = 1
		});
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(OnChainConfig.RpcTimeout);
		HttpClient httpClient = TestHttpClient ?? SharedHttpClient;
		StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
		content.Headers.ContentType.CharSet = null;
		HttpResponseMessage response = await httpClient.PostAsync(rpcUrl, content, cts.Token);
		response.EnsureSuccessStatusCode();
		using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cts.Token));
		if (doc.RootElement.TryGetProperty("error", out var error))
		{
			throw new Exception($"RPC error: {error}");
		}
		return doc.RootElement.GetProperty("result").GetString() ?? throw new Exception("No result in RPC response");
	}

	internal static string DecodeAbiString(string hexData)
	{
		string text = (hexData.StartsWith("0x") ? hexData.Substring(2) : hexData);
		if (text.Length < 128)
		{
			return "";
		}
		byte[] bytes = HexByteConvertorExtensions.HexToByteArray(text);
		int num = ReadUint(0);
		int num2 = ReadUint(num);
		if (num2 == 0 || num + 32 + num2 > bytes.Length)
		{
			return "";
		}
		return Encoding.UTF8.GetString(bytes, num + 32, num2);
		int ReadUint(int offset)
		{
			if (offset + 32 > bytes.Length)
			{
				return 0;
			}
			Span<byte> span = bytes.AsSpan(offset + 28, 4);
			return (span[0] << 24) | (span[1] << 16) | (span[2] << 8) | span[3];
		}
	}

	internal static bool DecodeBool(string hexData)
	{
		string text = (hexData.StartsWith("0x") ? hexData.Substring(2) : hexData);
		if (text.Length < 64)
		{
			return false;
		}
		byte[] array = HexByteConvertorExtensions.HexToByteArray(text);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != 0)
			{
				return true;
			}
		}
		return false;
	}
}
