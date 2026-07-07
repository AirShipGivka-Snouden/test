using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Security;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Util;
using Serilog;

namespace Ciphra.VPN.Common.Services.OnChain;

public class OnChainRegistrar
{
	internal class UserOperation
	{
		public string Sender { get; set; } = "";

		public string Nonce { get; set; } = "0x0";

		public string? Factory { get; set; }

		public string? FactoryData { get; set; }

		public string CallData { get; set; } = "0x";

		public string CallGasLimit { get; set; } = "0x0";

		public string VerificationGasLimit { get; set; } = "0x0";

		public string PreVerificationGas { get; set; } = "0x0";

		public string MaxFeePerGas { get; set; } = "0x0";

		public string MaxPriorityFeePerGas { get; set; } = "0x0";

		public string? Paymaster { get; set; }

		public string? PaymasterData { get; set; }

		public string PaymasterVerificationGasLimit { get; set; } = "0x0";

		public string PaymasterPostOpGasLimit { get; set; } = "0x0";

		public string Signature { get; set; } = "0x";

		public Dictionary<string, object> ToDict()
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>
			{
				["sender"] = Sender,
				["nonce"] = Nonce,
				["callData"] = CallData,
				["callGasLimit"] = CallGasLimit,
				["verificationGasLimit"] = VerificationGasLimit,
				["preVerificationGas"] = PreVerificationGas,
				["maxFeePerGas"] = MaxFeePerGas,
				["maxPriorityFeePerGas"] = MaxPriorityFeePerGas,
				["signature"] = Signature
			};
			if (Factory != null)
			{
				dictionary["factory"] = Factory;
			}
			if (FactoryData != null)
			{
				dictionary["factoryData"] = FactoryData;
			}
			if (Paymaster != null)
			{
				dictionary["paymaster"] = Paymaster;
			}
			if (PaymasterData != null)
			{
				dictionary["paymasterData"] = PaymasterData;
			}
			if (Paymaster != null)
			{
				dictionary["paymasterVerificationGasLimit"] = PaymasterVerificationGasLimit;
			}
			if (Paymaster != null)
			{
				dictionary["paymasterPostOpGasLimit"] = PaymasterPostOpGasLimit;
			}
			return dictionary;
		}

		public PackedUserOp PackForEntryPoint()
		{
			BigInteger bigInteger = HexToBigInt(VerificationGasLimit);
			BigInteger bigInteger2 = HexToBigInt(CallGasLimit);
			string accountGasLimits = ((bigInteger << 128) | bigInteger2).ToString("x").PadLeft(64, '0');
			BigInteger bigInteger3 = HexToBigInt(MaxPriorityFeePerGas);
			BigInteger bigInteger4 = HexToBigInt(MaxFeePerGas);
			string gasFees = ((bigInteger3 << 128) | bigInteger4).ToString("x").PadLeft(64, '0');
			string initCode = "";
			if (Factory != null)
			{
				string obj = (Factory.StartsWith("0x") ? Factory.Substring(2) : Factory);
				string? factoryData = FactoryData;
				initCode = obj + ((factoryData != null && factoryData.StartsWith("0x")) ? FactoryData.Substring(2) : (FactoryData ?? ""));
			}
			string paymasterAndData = "";
			if (Paymaster != null)
			{
				string text = (Paymaster.StartsWith("0x") ? Paymaster.Substring(2) : Paymaster);
				string text2 = HexToBigInt(PaymasterVerificationGasLimit).ToString("x").PadLeft(32, '0');
				string text3 = HexToBigInt(PaymasterPostOpGasLimit).ToString("x").PadLeft(32, '0');
				string? paymasterData = PaymasterData;
				string text4 = ((paymasterData != null && paymasterData.StartsWith("0x")) ? PaymasterData.Substring(2) : (PaymasterData ?? ""));
				paymasterAndData = text + text2 + text3 + text4;
			}
			string signature = (Signature.StartsWith("0x") ? Signature.Substring(2) : Signature);
			return new PackedUserOp
			{
				Sender = Sender,
				Nonce = Nonce,
				InitCode = initCode,
				CallData = (CallData.StartsWith("0x") ? CallData.Substring(2) : CallData),
				AccountGasLimits = accountGasLimits,
				PreVerificationGas = PreVerificationGas,
				GasFees = gasFees,
				PaymasterAndData = paymasterAndData,
				Signature = signature
			};
		}
	}

	internal class PackedUserOp
	{
		public string Sender { get; set; } = "";

		public string Nonce { get; set; } = "0x0";

		public string InitCode { get; set; } = "";

		public string CallData { get; set; } = "";

		public string AccountGasLimits { get; set; } = "";

		public string PreVerificationGas { get; set; } = "0x0";

		public string GasFees { get; set; } = "";

		public string PaymasterAndData { get; set; } = "";

		public string Signature { get; set; } = "";
	}

	private static readonly HttpClient SharedHttpClient = CreatePinnedHttpClient();

	private readonly OnChainBundlePoller _poller;

	internal HttpClient? TestHttpClient { get; set; }

	public OnChainRegistrar(OnChainBundlePoller poller)
	{
		_poller = poller;
	}

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

	public async Task<string> RegisterDeviceAsync(byte[] encryptedDeviceId, byte[] x25519PublicKey, byte[] secp256k1PrivateKey, CancellationToken ct)
	{
		EthECKey ecKey = new EthECKey(secp256k1PrivateKey, true);
		string eoaAddress = ecKey.GetPublicAddress();
		string smartAccount = await GetSmartAccountAddressAsync(eoaAddress, ct);
		Log.Information<string>("Smart Account address: {Address}", smartAccount);
		string executeCallData = BuildExecuteCallData(innerCallDataHex: BuildRegisterDeviceCallData(encryptedDeviceId, x25519PublicKey), target: "0x05a247D8345696cb194a9a4C4F3788b385B72E66", value: BigInteger.Zero);
		var (bundlerUrl, paymasterUrl) = await FindWorkingBundlerAsync(ct);
		Log.Information<string, string>("Using bundler: {Bundler}, paymaster: {Paymaster}", bundlerUrl, paymasterUrl);
		bool isDeployed = await IsAccountDeployedAsync(smartAccount, ct);
		string nonce = "0x0";
		string factory = null;
		string factoryDataHex = null;
		if (isDeployed)
		{
			Log.Information("Smart Account already deployed. Getting nonce...");
			nonce = await GetAccountNonceAsync(smartAccount, ct);
		}
		else
		{
			factory = "0x91E60e0613810449d098b0b5Ec8b51A0FE8c8985";
			factoryDataHex = "0x" + BuildCreateAccountData(eoaAddress);
		}
		UserOperation userOp = new UserOperation
		{
			Sender = smartAccount,
			Nonce = nonce,
			Factory = factory,
			FactoryData = factoryDataHex,
			CallData = "0x" + executeCallData,
			CallGasLimit = "0x0",
			VerificationGasLimit = "0x0",
			PreVerificationGas = "0x0",
			MaxFeePerGas = "0x0",
			MaxPriorityFeePerGas = "0x0",
			PaymasterVerificationGasLimit = "0x0",
			PaymasterPostOpGasLimit = "0x0",
			Signature = "0x" + CreateDummySignature(ecKey)
		};
		string opHash = await ConfigureSignAndSubmitAsync(userOp, ecKey, bundlerUrl, paymasterUrl, ct, async delegate
		{
			byte[] pub = await _poller.GetDevicePubKeyAsync(encryptedDeviceId, ct);
			return pub != null && pub.SequenceEqual(x25519PublicKey);
		});
		Log.Information("On-chain device registration confirmed.");
		return opHash;
	}

	public async Task<string> UpdateDeviceKeyAsync(byte[] encryptedDeviceId, byte[] x25519PublicKey, byte[] secp256k1PrivateKey, CancellationToken ct)
	{
		EthECKey ecKey = new EthECKey(secp256k1PrivateKey, true);
		string eoaAddress = ecKey.GetPublicAddress();
		string smartAccount = await GetSmartAccountAddressAsync(eoaAddress, ct);
		Log.Information<string>("UpdateDeviceKey: Smart Account {Address}", smartAccount);
		string executeCallData = BuildExecuteCallData(innerCallDataHex: BuildUpdateDeviceKeyCallData(encryptedDeviceId, x25519PublicKey), target: "0x05a247D8345696cb194a9a4C4F3788b385B72E66", value: BigInteger.Zero);
		(string BundlerUrl, string PaymasterUrl) tuple = await FindWorkingBundlerAsync(ct);
		string bundlerUrl = tuple.BundlerUrl;
		string paymasterUrl = tuple.PaymasterUrl;
		bool isDeployed = await IsAccountDeployedAsync(smartAccount, ct);
		string nonce = "0x0";
		string factory = null;
		string factoryDataHex = null;
		if (isDeployed)
		{
			nonce = await GetAccountNonceAsync(smartAccount, ct);
		}
		else
		{
			factory = "0x91E60e0613810449d098b0b5Ec8b51A0FE8c8985";
			factoryDataHex = "0x" + BuildCreateAccountData(eoaAddress);
		}
		UserOperation userOp = new UserOperation
		{
			Sender = smartAccount,
			Nonce = nonce,
			Factory = factory,
			FactoryData = factoryDataHex,
			CallData = "0x" + executeCallData,
			CallGasLimit = "0x0",
			VerificationGasLimit = "0x0",
			PreVerificationGas = "0x0",
			MaxFeePerGas = "0x0",
			MaxPriorityFeePerGas = "0x0",
			PaymasterVerificationGasLimit = "0x0",
			PaymasterPostOpGasLimit = "0x0",
			Signature = "0x" + CreateDummySignature(ecKey)
		};
		string opHash = await ConfigureSignAndSubmitAsync(userOp, ecKey, bundlerUrl, paymasterUrl, ct, async delegate
		{
			byte[] pub = await _poller.GetDevicePubKeyAsync(encryptedDeviceId, ct);
			return pub != null && pub.SequenceEqual(x25519PublicKey);
		});
		Log.Information("On-chain device key update confirmed.");
		return opHash;
	}

	private static string BuildUpdateDeviceKeyCallData(byte[] encDeviceId, byte[] x25519PubKey)
	{
		string text = HexByteConvertorExtensions.ToHex(encDeviceId, false).PadRight(64, '0');
		string text2 = HexByteConvertorExtensions.ToHex(x25519PubKey, false).PadLeft(64, '0');
		return "0x1a935c68".Substring(2) + text + text2;
	}

	private async Task<bool> IsAccountDeployedAsync(string accountAddress, CancellationToken ct)
	{
		string[] rpcEndpoints = OnChainConfig.RpcEndpoints;
		JsonElement el = default(JsonElement);
		foreach (string rpcUrl in rpcEndpoints)
		{
			try
			{
				JsonElement? result = await JsonRpcAsync(rpcUrl, "eth_getCode", new object[2] { accountAddress, "latest" }, ct);
				int num;
				if (result.HasValue)
				{
					el = result.GetValueOrDefault();
					num = 1;
				}
				else
				{
					num = 0;
				}
				if (num != 0)
				{
					string code = el.GetString() ?? "0x";
					return code != "0x" && code != "0x0" && code.Length > 2;
				}
				el = default(JsonElement);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch
			{
			}
		}
		return false;
	}

	private async Task<string> GetAccountNonceAsync(string accountAddress, CancellationToken ct)
	{
		string data = "0x35567e1a" + accountAddress.Substring(2).ToLower().PadLeft(64, '0') + new string('0', 64);
		string[] rpcEndpoints = OnChainConfig.RpcEndpoints;
		foreach (string rpcUrl in rpcEndpoints)
		{
			try
			{
				string result = await _poller.EthCallAsync(rpcUrl, "0x0000000071727De22E5E9d8BAf0edAc6f37da032", data, ct);
				string hex = (result.StartsWith("0x") ? result : ("0x" + result));
				Log.Information<string>("Smart Account nonce: {Nonce}", hex);
				return hex;
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex2)
			{
				Log.Warning<string>(ex2, "getNonce failed on {Url}", rpcUrl);
			}
		}
		return "0x0";
	}

	private async Task<string> GetSmartAccountAddressAsync(string eoaAddress, CancellationToken ct)
	{
		string data = "0x8cb84e18" + eoaAddress.Substring(2).ToLower().PadLeft(64, '0') + new string('0', 64);
		string[] rpcEndpoints = OnChainConfig.RpcEndpoints;
		foreach (string rpcUrl in rpcEndpoints)
		{
			try
			{
				string result = await _poller.EthCallAsync(rpcUrl, "0x91E60e0613810449d098b0b5Ec8b51A0FE8c8985", data, ct);
				string hex = (result.StartsWith("0x") ? result.Substring(2) : result);
				return "0x" + hex.Substring(24, 40);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex2)
			{
				Log.Warning<string>(ex2, "getAddress failed on {Url}", rpcUrl);
			}
		}
		throw new Exception("Failed to compute Smart Account address from all RPC nodes.");
	}

	private static string BuildRegisterDeviceCallData(byte[] encDeviceId, byte[] x25519PubKey)
	{
		string text = HexByteConvertorExtensions.ToHex(encDeviceId, false).PadRight(64, '0');
		string text2 = HexByteConvertorExtensions.ToHex(x25519PubKey, false).PadLeft(64, '0');
		return "0xd0675bfb".Substring(2) + text + text2;
	}

	private static string BuildExecuteCallData(string target, BigInteger value, string innerCallDataHex)
	{
		string text = target.Substring(2).ToLower().PadLeft(64, '0');
		string text2 = value.ToString("x").PadLeft(64, '0');
		byte[] array = HexByteConvertorExtensions.HexToByteArray(innerCallDataHex);
		string text3 = "0000000000000000000000000000000000000000000000000000000000000060";
		string text4 = array.Length.ToString("x").PadLeft(64, '0');
		string text5 = innerCallDataHex + new string('0', (32 - array.Length % 32) % 32 * 2);
		return "0xb61d27f6".Substring(2) + text + text2 + text3 + text4 + text5;
	}

	private static string BuildCreateAccountData(string eoaAddress)
	{
		string text = eoaAddress.Substring(2).ToLower().PadLeft(64, '0');
		return "0x5fbfb9cf".Substring(2) + text + new string('0', 64);
	}

	private static string CreateDummySignature(EthECKey ecKey)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		byte[] array = new byte[32];
		RandomNumberGenerator.Fill(array);
		EthereumMessageSigner val = new EthereumMessageSigner();
		string text = ((MessageSigner)val).Sign(array, ecKey);
		return text.StartsWith("0x") ? text.Substring(2) : text;
	}

	private static string SignUserOpHash(string userOpHash, EthECKey ecKey)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		byte[] array = HexByteConvertorExtensions.HexToByteArray(userOpHash.StartsWith("0x") ? userOpHash.Substring(2) : userOpHash);
		EthereumMessageSigner val = new EthereumMessageSigner();
		string text = ((MessageSigner)val).Sign(array, ecKey);
		return text.StartsWith("0x") ? text : ("0x" + text);
	}

	private async Task<string> ConfigureSignAndSubmitAsync(UserOperation userOp, EthECKey ecKey, string bundlerUrl, string paymasterUrl, CancellationToken ct, Func<Task<bool>>? onChainVerify = null)
	{
		await SetGasPriceAsync(userOp, bundlerUrl, ct);
		await GetPaymasterStubDataAsync(userOp, paymasterUrl, ct);
		string stubPmVgl = userOp.PaymasterVerificationGasLimit;
		Log.Debug<Dictionary<string, object>>("UserOp before estimation: {@UserOp}", userOp.ToDict());
		try
		{
			await EstimateGasAsync(userOp, bundlerUrl, ct);
		}
		catch (Exception ex) when (!ct.IsCancellationRequested)
		{
			Log.Warning<string>(ex, "Gas estimation failed on {Bundler}, trying Pimlico fallback.", bundlerUrl);
			string pimlicoUrl = OnChainConfig.Bundlers[^1];
			if (!(pimlicoUrl != bundlerUrl))
			{
				throw;
			}
			await EstimateGasAsync(userOp, pimlicoUrl, ct);
		}
		userOp.PaymasterVerificationGasLimit = HexMax(HexMax(userOp.PaymasterVerificationGasLimit, stubPmVgl), "0x30000");
		await GetPaymasterDataAsync(userOp, paymasterUrl, ct);
		string userOpHash = ComputeUserOpHash(userOp);
		Log.Information<string>("Computed UserOp hash: {Hash}", userOpHash);
		userOp.Signature = SignUserOpHash(userOpHash, ecKey);
		string opHash = await SendUserOperationAsync(userOp, bundlerUrl, ct);
		Log.Information<string, string>("UserOperation submitted to {Bundler}. Hash: {Hash}", bundlerUrl, opHash);
		await WaitForReceiptAsync(opHash, bundlerUrl, ct, onChainVerify);
		return opHash;
	}

	private async Task<(string BundlerUrl, string PaymasterUrl)> FindWorkingBundlerAsync(CancellationToken ct)
	{
		JsonElement arr = default(JsonElement);
		for (int i = 0; i < OnChainConfig.Bundlers.Length; i++)
		{
			ct.ThrowIfCancellationRequested();
			try
			{
				string bundlerUrl = OnChainConfig.Bundlers[i];
				JsonElement? response = await JsonRpcAsync(bundlerUrl, "eth_supportedEntryPoints", new object[0], ct);
				int num;
				if (response.HasValue)
				{
					arr = response.GetValueOrDefault();
					num = ((arr.ValueKind != JsonValueKind.Array) ? 1 : 0);
				}
				else
				{
					num = 1;
				}
				if (num != 0)
				{
					continue;
				}
				bool supported = false;
				foreach (JsonElement item in arr.EnumerateArray())
				{
					if (string.Equals(item.GetString(), "0x0000000071727De22E5E9d8BAf0edAc6f37da032", StringComparison.OrdinalIgnoreCase))
					{
						supported = true;
						break;
					}
				}
				if (!supported)
				{
					continue;
				}
				try
				{
					await JsonRpcAsync(bundlerUrl, "eth_estimateUserOperationGas", new object[2]
					{
						new Dictionary<string, string>
						{
							["sender"] = "0x0000000000000000000000000000000000000001",
							["nonce"] = "0x0",
							["callData"] = "0x",
							["callGasLimit"] = "0x0",
							["verificationGasLimit"] = "0x0",
							["preVerificationGas"] = "0x0",
							["maxFeePerGas"] = "0x5f5e100",
							["maxPriorityFeePerGas"] = "0x5f5e100",
							["signature"] = "0x00"
						},
						"0x0000000071727De22E5E9d8BAf0edAc6f37da032"
					}, ct);
				}
				catch (Exception ex) when (ex.Message.Contains("rpc provider", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("internal error", StringComparison.OrdinalIgnoreCase))
				{
					Log.Warning<int, string>("Bundler {Index} ({Url}) RPC backend unreachable, skipping.", i, bundlerUrl);
					goto end_IL_003c;
				}
				catch
				{
				}
				Log.Information<int, string>("Selected bundler {Index}: {Url}", i, bundlerUrl);
				return (BundlerUrl: bundlerUrl, PaymasterUrl: OnChainConfig.Paymasters[i]);
				end_IL_003c:;
			}
			catch (Exception ex2)
			{
				Log.Warning<int, string>(ex2, "Bundler {Index} ({Url}) unreachable.", i, OnChainConfig.Bundlers[i]);
			}
		}
		throw new Exception("No working ERC-4337 bundler found.");
	}

	private async Task SetGasPriceAsync(UserOperation userOp, string bundlerUrl, CancellationToken ct)
	{
		JsonElement standard = default(JsonElement);
		if (bundlerUrl.Contains("pimlico", StringComparison.OrdinalIgnoreCase) && ((await JsonRpcAsync(bundlerUrl, "pimlico_getUserOperationGasPrice", new object[0], ct))?.TryGetProperty("standard", out standard) ?? false))
		{
			userOp.MaxFeePerGas = standard.GetProperty("maxFeePerGas").GetString();
			userOp.MaxPriorityFeePerGas = standard.GetProperty("maxPriorityFeePerGas").GetString();
			return;
		}
		string[] rpcEndpoints = OnChainConfig.RpcEndpoints;
		JsonElement el = default(JsonElement);
		foreach (string rpcUrl in rpcEndpoints)
		{
			try
			{
				JsonElement? result = await JsonRpcAsync(rpcUrl, "eth_gasPrice", new object[0], ct);
				int num;
				if (result.HasValue)
				{
					el = result.GetValueOrDefault();
					num = 1;
				}
				else
				{
					num = 0;
				}
				if (num != 0)
				{
					string gasPriceHex = el.GetString() ?? "0x0";
					BigInteger gasPrice = HexToBigInt(gasPriceHex);
					BigInteger minPriorityFee = BigInteger.Parse("100000000");
					BigInteger priorityFee = BigInteger.Max(gasPrice, minPriorityFee);
					userOp.MaxFeePerGas = "0x" + (priorityFee * 2).ToString("x");
					userOp.MaxPriorityFeePerGas = "0x" + priorityFee.ToString("x");
					return;
				}
				el = default(JsonElement);
			}
			catch
			{
			}
		}
		throw new Exception("Failed to get gas price from any source.");
	}

	private async Task GetPaymasterStubDataAsync(UserOperation userOp, string paymasterUrl, CancellationToken ct)
	{
		JsonElement? result = await JsonRpcAsync(paymasterUrl, "pm_getPaymasterStubData", new object[4]
		{
			userOp.ToDict(),
			"0x0000000071727De22E5E9d8BAf0edAc6f37da032",
			"0xaa36a7",
			new { }
		}, ct);
		JsonElement obj = default(JsonElement);
		int num;
		if (result.HasValue)
		{
			obj = result.GetValueOrDefault();
			num = 1;
		}
		else
		{
			num = 0;
		}
		if (num != 0)
		{
			userOp.Paymaster = obj.GetProperty("paymaster").GetString();
			userOp.PaymasterData = obj.GetProperty("paymasterData").GetString();
			if (obj.TryGetProperty("paymasterVerificationGasLimit", out var pvgl))
			{
				userOp.PaymasterVerificationGasLimit = pvgl.GetString() ?? "0x0";
			}
			if (obj.TryGetProperty("paymasterPostOpGasLimit", out var ppgl))
			{
				userOp.PaymasterPostOpGasLimit = ppgl.GetString() ?? "0x0";
			}
		}
	}

	private async Task EstimateGasAsync(UserOperation userOp, string bundlerUrl, CancellationToken ct)
	{
		JsonElement? result = await JsonRpcAsync(bundlerUrl, "eth_estimateUserOperationGas", new object[3]
		{
			userOp.ToDict(),
			"0x0000000071727De22E5E9d8BAf0edAc6f37da032",
			new { }
		}, ct);
		JsonElement obj = default(JsonElement);
		int num;
		if (result.HasValue)
		{
			obj = result.GetValueOrDefault();
			num = 1;
		}
		else
		{
			num = 0;
		}
		if (num != 0)
		{
			if (obj.TryGetProperty("callGasLimit", out var cgl))
			{
				userOp.CallGasLimit = cgl.GetString() ?? userOp.CallGasLimit;
			}
			if (obj.TryGetProperty("verificationGasLimit", out var vgl))
			{
				userOp.VerificationGasLimit = vgl.GetString() ?? userOp.VerificationGasLimit;
			}
			if (obj.TryGetProperty("preVerificationGas", out var pvg))
			{
				userOp.PreVerificationGas = pvg.GetString() ?? userOp.PreVerificationGas;
			}
			if (obj.TryGetProperty("paymasterVerificationGasLimit", out var pmvgl))
			{
				userOp.PaymasterVerificationGasLimit = pmvgl.GetString() ?? userOp.PaymasterVerificationGasLimit;
			}
			if (obj.TryGetProperty("paymasterPostOpGasLimit", out var pmpogl))
			{
				userOp.PaymasterPostOpGasLimit = pmpogl.GetString() ?? userOp.PaymasterPostOpGasLimit;
			}
		}
	}

	private async Task GetPaymasterDataAsync(UserOperation userOp, string paymasterUrl, CancellationToken ct)
	{
		JsonElement? result = await JsonRpcAsync(paymasterUrl, "pm_getPaymasterData", new object[4]
		{
			userOp.ToDict(),
			"0x0000000071727De22E5E9d8BAf0edAc6f37da032",
			"0xaa36a7",
			new { }
		}, ct);
		JsonElement obj = default(JsonElement);
		int num;
		if (result.HasValue)
		{
			obj = result.GetValueOrDefault();
			num = 1;
		}
		else
		{
			num = 0;
		}
		if (num != 0 && obj.TryGetProperty("paymasterData", out var pd))
		{
			userOp.PaymasterData = pd.GetString() ?? userOp.PaymasterData;
		}
	}

	private static string ComputeUserOpHash(UserOperation userOp)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Expected O, but got Unknown
		PackedUserOp packedUserOp = userOp.PackForEntryPoint();
		Sha3Keccack val = new Sha3Keccack();
		byte[] array = HexToBytes(packedUserOp.InitCode);
		byte[] array2 = HexToBytes(packedUserOp.CallData);
		byte[] array3 = HexToBytes(packedUserOp.PaymasterAndData);
		byte[] array4 = val.CalculateHash(array);
		byte[] array5 = val.CalculateHash(array2);
		byte[] array6 = val.CalculateHash(array3);
		string text = packedUserOp.Sender.Substring(2).ToLower().PadLeft(64, '0') + FormatUint256(packedUserOp.Nonce) + HexByteConvertorExtensions.ToHex(array4, false) + HexByteConvertorExtensions.ToHex(array5, false) + packedUserOp.AccountGasLimits + FormatUint256(packedUserOp.PreVerificationGas) + packedUserOp.GasFees + HexByteConvertorExtensions.ToHex(array6, false);
		byte[] array7 = val.CalculateHash(HexByteConvertorExtensions.HexToByteArray(text));
		string text2 = "0x0000000071727De22E5E9d8BAf0edAc6f37da032".Substring(2).ToLower().PadLeft(64, '0');
		string text3 = 11155111.ToString("x").PadLeft(64, '0');
		string text4 = HexByteConvertorExtensions.ToHex(array7, false) + text2 + text3;
		byte[] array8 = val.CalculateHash(HexByteConvertorExtensions.HexToByteArray(text4));
		return "0x" + HexByteConvertorExtensions.ToHex(array8, false);
	}

	private async Task<string> SendUserOperationAsync(UserOperation userOp, string bundlerUrl, CancellationToken ct)
	{
		JsonElement? result = await JsonRpcAsync(bundlerUrl, "eth_sendUserOperation", new object[2]
		{
			userOp.ToDict(),
			"0x0000000071727De22E5E9d8BAf0edAc6f37da032"
		}, ct, TimeSpan.FromSeconds(30L));
		JsonElement el = default(JsonElement);
		int num;
		if (result.HasValue)
		{
			el = result.GetValueOrDefault();
			num = 1;
		}
		else
		{
			num = 0;
		}
		if (num != 0)
		{
			return el.GetString() ?? throw new Exception("No hash returned from sendUserOperation.");
		}
		throw new Exception("Unexpected response from sendUserOperation.");
	}

	private async Task WaitForReceiptAsync(string opHash, string bundlerUrl, CancellationToken ct, Func<Task<bool>>? onChainVerify = null)
	{
		JsonElement obj = default(JsonElement);
		for (int i = 0; i < 60; i++)
		{
			ct.ThrowIfCancellationRequested();
			await Task.Delay(OnChainConfig.ReceiptPollInterval, ct);
			try
			{
				JsonElement? result = await JsonRpcAsync(bundlerUrl, "eth_getUserOperationReceipt", new object[1] { opHash }, ct);
				int num;
				if (result.HasValue)
				{
					obj = result.GetValueOrDefault();
					num = ((obj.ValueKind != JsonValueKind.Null) ? 1 : 0);
				}
				else
				{
					num = 0;
				}
				if (num != 0)
				{
					if (obj.TryGetProperty("success", out var success) && success.GetBoolean())
					{
						return;
					}
					if (obj.TryGetProperty("success", out var fail) && !fail.GetBoolean())
					{
						throw new Exception("UserOperation execution reverted on-chain.");
					}
					success = default(JsonElement);
					fail = default(JsonElement);
				}
				obj = default(JsonElement);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex2) when (ex2.Message.Contains("reverted"))
			{
				throw;
			}
			catch
			{
			}
			if (onChainVerify == null || (i + 1) % 10 != 0)
			{
				continue;
			}
			try
			{
				if (await onChainVerify())
				{
					Log.Information<int>("Receipt not found but on-chain state verified after {Attempts} polls.", i + 1);
					return;
				}
			}
			catch (Exception ex3)
			{
				Exception ex4 = ex3;
				Log.Debug(ex4, "On-chain verification check failed.");
			}
		}
		if (onChainVerify != null)
		{
			try
			{
				if (await onChainVerify())
				{
					Log.Information("Receipt timed out but on-chain state verified.");
					return;
				}
			}
			catch (Exception ex3)
			{
				Exception ex5 = ex3;
				Log.Debug(ex5, "Final on-chain verification failed.");
			}
		}
		throw new TimeoutException($"UserOperation receipt not received after {60} attempts.");
	}

	private async Task<JsonElement?> JsonRpcAsync(string url, string method, object[] parameters, CancellationToken ct, TimeSpan? timeout = null)
	{
		string requestBody = JsonSerializer.Serialize(new
		{
			jsonrpc = "2.0",
			method = method,
			@params = parameters,
			id = 1
		});
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(timeout ?? OnChainConfig.RpcTimeout);
		HttpClient httpClient = TestHttpClient ?? SharedHttpClient;
		StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
		content.Headers.ContentType.CharSet = null;
		HttpResponseMessage response = await httpClient.PostAsync(url, content, cts.Token);
		response.EnsureSuccessStatusCode();
		using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cts.Token));
		if (doc.RootElement.TryGetProperty("error", out var error))
		{
			throw new Exception($"JSON-RPC error ({method}): {error}");
		}
		if (doc.RootElement.TryGetProperty("result", out var result))
		{
			return result.Clone();
		}
		return null;
	}

	private static string HexMax(string a, string b)
	{
		BigInteger left = HexToBigInt(a);
		BigInteger right = HexToBigInt(b);
		return "0x" + BigInteger.Max(left, right).ToString("x");
	}

	private static BigInteger HexToBigInt(string hex)
	{
		hex = (hex.StartsWith("0x") ? hex.Substring(2) : hex);
		if (string.IsNullOrEmpty(hex))
		{
			return BigInteger.Zero;
		}
		return BigInteger.Parse("0" + hex, NumberStyles.HexNumber);
	}

	private static string FormatUint256(string hexValue)
	{
		string text = (hexValue.StartsWith("0x") ? hexValue.Substring(2) : hexValue);
		return text.PadLeft(64, '0');
	}

	private static byte[] HexToBytes(string hex)
	{
		hex = (hex.StartsWith("0x") ? hex.Substring(2) : hex);
		if (string.IsNullOrEmpty(hex))
		{
			return Array.Empty<byte>();
		}
		return HexByteConvertorExtensions.HexToByteArray(hex);
	}

	private static string EncodeBytes(byte[] data)
	{
		string text = data.Length.ToString("x").PadLeft(64, '0');
		string text2 = HexByteConvertorExtensions.ToHex(data, false);
		string text3 = text2 + new string('0', (32 - data.Length % 32) % 32 * 2);
		return text + text3;
	}

	private static int PadTo32(int length)
	{
		return (length != 0) ? ((length + 31) / 32 * 32) : 0;
	}
}
