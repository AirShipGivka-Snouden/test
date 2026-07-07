using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services.Interfaces;
using Newtonsoft.Json;
using Serilog;

namespace Ciphra.VPN.Common.Services.OnChain;

public class OnChainCredentialService
{
	private readonly OnChainKeyStore _keyStore;

	private readonly OnChainRegistrar _registrar;

	private readonly IOnChainBundlePoller _poller;

	private readonly BundleCache _bundleCache;

	private readonly IDeviceIdService _deviceIdService;

	private readonly Settings _settings;

	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

	private Task<OnChainBundle>? _inflightTask;

	private CancellationTokenSource? _inflightCts;

	public static bool ForceOnChainMode { get; set; }

	public IAppAnalytics? Analytics { get; set; }

	public static event Action<string?>? StatusChanged;

	private static void ReportStatus(string? message)
	{
		try
		{
			OnChainCredentialService.StatusChanged?.Invoke(message);
		}
		catch
		{
		}
	}

	public OnChainCredentialService(OnChainKeyStore keyStore, OnChainRegistrar registrar, IOnChainBundlePoller poller, BundleCache bundleCache, IDeviceIdService deviceIdService, Settings settings)
	{
		_keyStore = keyStore;
		_registrar = registrar;
		_poller = poller;
		_bundleCache = bundleCache;
		_deviceIdService = deviceIdService;
		_settings = settings;
	}

	public async Task<OnChainBundle> GetCredentialsAsync(CancellationToken ct)
	{
		await _gate.WaitAsync(ct);
		Task<OnChainBundle> task;
		try
		{
			Task<OnChainBundle> inflightTask = _inflightTask;
			if (inflightTask != null && !inflightTask.IsCompleted)
			{
				Log.Information("On-chain credentials request coalesced with in-flight operation.");
				task = _inflightTask;
			}
			else
			{
				_inflightCts?.Dispose();
				_inflightCts = new CancellationTokenSource();
				_inflightTask = GetCredentialsCoreAsync(_inflightCts.Token);
				_inflightTask.ContinueWith(delegate(Task<OnChainBundle> t)
				{
					_ = t.Exception;
				}, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
				task = _inflightTask;
			}
		}
		finally
		{
			_gate.Release();
		}
		return await task.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task<OnChainBundle> GetCredentialsCoreAsync(CancellationToken ct)
	{
		Stopwatch sw = Stopwatch.StartNew();
		TrackEvent("onchain_fallback_started");
		try
		{
			(byte[], byte[]) orCreateX25519Keypair = _keyStore.GetOrCreateX25519Keypair();
			byte[] x25519Priv = orCreateX25519Keypair.Item1;
			byte[] x25519Pub = orCreateX25519Keypair.Item2;
			byte[] secp256k1Priv = _keyStore.GetOrCreateSecp256k1Keypair().PrivateKey;
			byte[] encDeviceId = DeviceIdCrypto.EncryptDeviceId(await _deviceIdService.GetDeviceId());
			OnChainRpcResult<bool> revoked = await _poller.TryIsRevokedAsync(encDeviceId, ct);
			if (revoked.IsFound && revoked.Value)
			{
				Log.Warning("Device is revoked on-chain. Clearing cached bundle and IPNS cache.");
				_bundleCache.Clear();
				_keyStore.InvalidateIpnsCache();
				_settings.IsUserTokenValid = false;
				TrackEvent("onchain_device_revoked");
				throw new UnauthorizedAccessException("Device has been revoked on-chain.");
			}
			OnChainBundle cached = _bundleCache.Load();
			if (cached != null)
			{
				Log.Information("Using cached on-chain bundle (TTL valid).");
				TrackEvent("onchain_fallback_result", ("outcome", "cache_hit"), ("servers", cached.Servers.Count.ToString()), ("subscription_status", cached.SubscriptionStatus.ToString()), ("duration_ms", sw.ElapsedMilliseconds.ToString()));
				return cached;
			}
			string cachedIpns = _keyStore.GetCachedIpnsName(encDeviceId, x25519Pub);
			if (cachedIpns != null)
			{
				OnChainBundle viaCache = await TryFetchBundleViaIpnsAsync(cachedIpns, x25519Priv, ct);
				if (viaCache != null)
				{
					PersistToken(viaCache);
					TrackEvent("onchain_fallback_result", ("outcome", "cache_hit_ipns"), ("servers", viaCache.Servers.Count.ToString()), ("subscription_status", viaCache.SubscriptionStatus.ToString()), ("duration_ms", sw.ElapsedMilliseconds.ToString()));
					return viaCache;
				}
			}
			OnChainRpcResult<byte[]> pubKeyResult = await _poller.TryGetDevicePubKeyAsync(encDeviceId, ct);
			if (pubKeyResult.IsUnavailable)
			{
				return ReturnStaleOrThrow(sw, "rpc_unavailable_pubkey");
			}
			bool needsBundleWait = false;
			string outcomeTag = "bundle_fetched";
			if (pubKeyResult.Outcome == OnChainRpcOutcome.NotFound)
			{
				Log.Information("Device not registered on-chain. Starting ERC-4337 registration...");
				TrackEvent("onchain_registration_started");
				ReportStatus("Registering device on blockchain...\nThis may take up to 2 minutes");
				try
				{
					await _registrar.RegisterDeviceAsync(encDeviceId, x25519Pub, secp256k1Priv, ct);
					TrackEvent("onchain_registration_confirmed");
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					TrackEvent("onchain_registration_failed", ("error", ex2.Message));
					throw;
				}
				needsBundleWait = true;
				outcomeTag = "registered_new";
			}
			else if (!pubKeyResult.Value.SequenceEqual(x25519Pub))
			{
				Log.Information("On-chain pub key mismatch. Updating device key...");
				TrackEvent("onchain_key_update_started");
				ReportStatus("Updating device key on blockchain...\nThis may take up to 2 minutes");
				try
				{
					await _registrar.UpdateDeviceKeyAsync(encDeviceId, x25519Pub, secp256k1Priv, ct);
					TrackEvent("onchain_key_update_confirmed");
				}
				catch (Exception ex)
				{
					Exception ex3 = ex;
					TrackEvent("onchain_key_update_failed", ("error", ex3.Message));
					throw;
				}
				_keyStore.InvalidateIpnsCache();
				needsBundleWait = true;
				outcomeTag = "key_updated";
			}
			if (needsBundleWait)
			{
				Log.Information("Waiting for server to publish bundle...");
				ReportStatus("Waiting for credentials from server...\nPlease wait");
				(byte[] EncryptedBundle, string IpnsName) tuple = await _poller.WaitForBundleAsync(encDeviceId, x25519Priv, ct);
				byte[] encryptedBundle = tuple.EncryptedBundle;
				string ipnsName = tuple.IpnsName;
				OnChainBundle bundle = DecryptAndCache(encryptedBundle, x25519Priv);
				_keyStore.CacheIpnsName(ipnsName, encDeviceId, x25519Pub);
				PersistToken(bundle);
				TrackEvent("onchain_fallback_result", ("outcome", outcomeTag), ("servers", bundle.Servers.Count.ToString()), ("subscription_status", bundle.SubscriptionStatus.ToString()), ("duration_ms", sw.ElapsedMilliseconds.ToString()));
				return bundle;
			}
			OnChainRpcResult<string> ipnsResult = await _poller.TryGetEncryptedIpnsNameAsync(encDeviceId, ct);
			if (ipnsResult.IsUnavailable)
			{
				return ReturnStaleOrThrow(sw, "rpc_unavailable_ipns");
			}
			if (ipnsResult.IsFound)
			{
				try
				{
					string ipnsName2 = BundleDecryptor.DecryptIpnsName(ipnsResult.Value, x25519Priv);
					byte[] encryptedBundle2 = await _poller.FetchBundleAsync(ipnsName2, ct);
					if (encryptedBundle2 != null)
					{
						OnChainBundle fetched = DecryptAndCache(encryptedBundle2, x25519Priv);
						_keyStore.CacheIpnsName(ipnsName2, encDeviceId, x25519Pub);
						PersistToken(fetched);
						TrackEvent("onchain_fallback_result", ("outcome", "bundle_fetched"), ("servers", fetched.Servers.Count.ToString()), ("subscription_status", fetched.SubscriptionStatus.ToString()), ("duration_ms", sw.ElapsedMilliseconds.ToString()));
						return fetched;
					}
				}
				catch (CryptographicException ex4)
				{
					CryptographicException ex5 = ex4;
					Log.Warning((Exception)ex5, "IPNS name decrypt failed — server may not have re-encrypted yet");
				}
			}
			Log.Information("IPNS bundle not available yet. Waiting...");
			ReportStatus("Waiting for credentials from server...\nPlease wait");
			(byte[] EncryptedBundle, string IpnsName) tuple2 = await _poller.WaitForBundleAsync(encDeviceId, x25519Priv, ct);
			byte[] encBundle = tuple2.EncryptedBundle;
			string polledIpns = tuple2.IpnsName;
			OnChainBundle polled = DecryptAndCache(encBundle, x25519Priv);
			_keyStore.CacheIpnsName(polledIpns, encDeviceId, x25519Pub);
			PersistToken(polled);
			TrackEvent("onchain_fallback_result", ("outcome", "bundle_waited"), ("servers", polled.Servers.Count.ToString()), ("subscription_status", polled.SubscriptionStatus.ToString()), ("duration_ms", sw.ElapsedMilliseconds.ToString()));
			return polled;
		}
		catch (Exception ex6) when (!(ex6 is OperationCanceledException) && !(ex6 is UnauthorizedAccessException))
		{
			TrackEvent("onchain_fallback_result", ("outcome", "failed"), ("error", ex6.GetType().Name + ": " + ex6.Message), ("error_site", ex6.TargetSite?.DeclaringType?.Name + "." + ex6.TargetSite?.Name), ("error_stack", ex6.StackTrace ?? ""), ("duration_ms", sw.ElapsedMilliseconds.ToString()));
			throw;
		}
		finally
		{
			ReportStatus(null);
		}
	}

	private async Task<OnChainBundle?> TryFetchBundleViaIpnsAsync(string ipnsName, byte[] x25519Priv, CancellationToken ct)
	{
		try
		{
			Log.Information<string>("Fetching bundle via cached IPNS: {IpnsName}", ipnsName);
			byte[] encryptedBundle = await _poller.FetchBundleAsync(ipnsName, ct);
			if (encryptedBundle == null)
			{
				return null;
			}
			return DecryptAndCache(encryptedBundle, x25519Priv);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			Log.Warning(ex3, "Cached-IPNS fast path failed — falling through to RPC");
			return null;
		}
	}

	private OnChainBundle ReturnStaleOrThrow(Stopwatch sw, string reason)
	{
		OnChainBundle onChainBundle = _bundleCache.LoadEvenIfExpired();
		if (onChainBundle != null)
		{
			Log.Warning<string>("RPC unavailable ({Reason}); serving stale cached bundle.", reason);
			TrackEvent("onchain_fallback_result", ("outcome", "stale_cache"), ("reason", reason), ("servers", onChainBundle.Servers.Count.ToString()), ("subscription_status", onChainBundle.SubscriptionStatus.ToString()), ("duration_ms", sw.ElapsedMilliseconds.ToString()));
			return onChainBundle;
		}
		TrackEvent("onchain_fallback_result", ("outcome", "rpc_unavailable_no_cache"), ("reason", reason), ("duration_ms", sw.ElapsedMilliseconds.ToString()));
		throw new InvalidOperationException("On-chain RPC unavailable (" + reason + ") and no cached bundle available.");
	}

	private void PersistToken(OnChainBundle bundle)
	{
		if (!string.IsNullOrEmpty(bundle.Token))
		{
			StoreToken(bundle.Token);
		}
	}

	public static TokenCheckResponse ToTokenCheckResponse(OnChainBundle bundle)
	{
		DateTime result;
		return new TokenCheckResponse
		{
			Success = true,
			Valid = true,
			ExpiresAt = DateTime.UtcNow.AddDays(30.0),
			User = new UserDto
			{
				UserId = 0,
				SubscriptionStatus = bundle.SubscriptionStatus,
				SubscriptionOverdue = bundle.SubscriptionOverdue,
				SubscriptionExpiryDay = (string.IsNullOrEmpty(bundle.SubscriptionExpiryDay) ? ((DateTime?)null) : (DateTime.TryParse(bundle.SubscriptionExpiryDay, out result) ? new DateTime?(result) : ((DateTime?)null)))
			}
		};
	}

	public static ServerKeysV2Response ToServerKeysResponse(OnChainBundle bundle)
	{
		return new ServerKeysV2Response
		{
			Success = true,
			Servers = bundle.Servers.Select((OnChainServer s) => s.ToVpnServerDto()).ToList(),
			SubscriptionStatus = bundle.SubscriptionStatus,
			SubscriptionOverdue = bundle.SubscriptionOverdue,
			SubscriptionSource = bundle.SubscriptionSource
		};
	}

	private OnChainBundle DecryptAndCache(byte[] encryptedBundle, byte[] x25519PrivateKey)
	{
		string text = BundleDecryptor.Decrypt(encryptedBundle, x25519PrivateKey);
		OnChainBundle onChainBundle = JsonConvert.DeserializeObject<OnChainBundle>(text) ?? throw new Exception("Failed to deserialize on-chain bundle.");
		_bundleCache.Save(onChainBundle);
		Log.Information<int, int>("On-chain bundle decrypted and cached. Servers: {Count}, Sub: {Status}", onChainBundle.Servers.Count, onChainBundle.SubscriptionStatus);
		return onChainBundle;
	}

	private void StoreToken(string token)
	{
		_settings.UserToken = token;
		_settings.IsUserTokenValid = true;
		Log.Information("Stored on-chain token for future API use.");
	}

	private void TrackEvent(string eventName, params (string Key, string Value)[] properties)
	{
		try
		{
			Analytics?.SendEvent(eventName, properties);
		}
		catch (Exception ex)
		{
			Log.Warning<string>(ex, "Failed to send analytics event: {Event}", eventName);
		}
	}
}
