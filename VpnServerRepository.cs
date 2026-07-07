using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services.Interfaces;
using Ciphra.VPN.Common.Services.OnChain;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public class VpnServerRepository
{
	private readonly ApiService _apiService;

	private readonly VpnServersCache _vpnServersCache;

	private readonly IDeviceIdService _deviceIdService;

	private readonly Settings _settings;

	private readonly AuthProvider _authProvider;

	private readonly OnChainCredentialService? _onChainService;

	public VpnServerRepository(ApiService apiService, VpnServersCache vpnServersCache, IDeviceIdService deviceIdService, Settings settings, AuthProvider authProvider, OnChainCredentialService? onChainService = null)
	{
		_apiService = apiService ?? throw new ArgumentNullException("apiService");
		_vpnServersCache = vpnServersCache ?? throw new ArgumentNullException("vpnServersCache");
		_deviceIdService = deviceIdService ?? throw new ArgumentNullException("deviceIdService");
		_settings = settings ?? throw new ArgumentNullException("settings");
		_authProvider = authProvider ?? throw new ArgumentNullException("authProvider");
		_onChainService = onChainService;
	}

	public async Task<List<VpnServerDto>> GetCachedServersAsync(CancellationToken ct)
	{
		if (string.IsNullOrEmpty(_settings.UserToken) || _settings.UserToken.Contains("Not set"))
		{
			throw new InvalidOperationException("User token is not set. Please authenticate first.");
		}
		List<VpnServerDto> cachedServers = _vpnServersCache.GetAllServers();
		if (cachedServers != null && cachedServers.Count > 0)
		{
			Log.Information("Returning cached VPN servers");
			return cachedServers;
		}
		Log.Warning("No cached VPN servers found, fetching from API");
		return await GetServersAsync(ct);
	}

	public async Task<List<VpnServerDto>> GetServersAsync(CancellationToken ct)
	{
		if (string.IsNullOrEmpty(_settings.UserToken) || _settings.UserToken.Contains("Not set"))
		{
			throw new InvalidOperationException("User token is not set. Please authenticate first.");
		}
		try
		{
			List<VpnServerDto> freshServers = await FetchServersFromApiAsync(ct);
			if (freshServers.Count > 0)
			{
				return freshServers;
			}
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Error fetching VPN servers from API: {Message}", ex.Message);
		}
		List<VpnServerDto> cachedServers = _vpnServersCache.GetAllServers();
		if (cachedServers != null && cachedServers.Count > 0)
		{
			Log.Information("Returning cached VPN servers");
			return cachedServers;
		}
		Log.Warning("No VPN servers available in cache or from API");
		return new List<VpnServerDto>();
	}

	public async Task<VpnServerDto?> RefreshServerAccessKeyAsync(VpnServerDto currentServer, CancellationToken ct)
	{
		TokenValidationResult tokenResult = await _authProvider.ValidateToken(_settings.UserToken, ct);
		if (!tokenResult.IsValid && tokenResult.Exception == null)
		{
			Log.Information("Personal token expired, attempting to restore/create a new one...");
			string restored = await _authProvider.TryRestoreToken(ct);
			if (string.IsNullOrEmpty(restored))
			{
				restored = await _authProvider.CreateNewToken(ct);
			}
			if (string.IsNullOrEmpty(restored))
			{
				throw new InvalidOperationException("Failed to obtain a valid personal token.");
			}
			_settings.UserToken = restored;
			_settings.IsUserTokenValid = true;
			Log.Information("Personal token refreshed successfully.");
		}
		return (await FetchServersFromApiAsync(ct)).FirstOrDefault((VpnServerDto s) => s.Id == currentServer.Id);
	}

	private async Task<List<VpnServerDto>> FetchServersFromApiAsync(CancellationToken ct)
	{
		ServerKeysV2Response response;
		int num;
		if (OnChainCredentialService.ForceOnChainMode && _onChainService != null)
		{
			Log.Information("ForceOnChainMode: skipping API, using on-chain credentials for server keys.");
			response = OnChainCredentialService.ToServerKeysResponse(await _onChainService.GetCredentialsAsync(ct));
			if (response != null)
			{
				List<VpnServerDto> servers = response.Servers;
				if (servers != null)
				{
					num = ((servers.Count > 0) ? 1 : 0);
					goto IL_0131;
				}
			}
			num = 0;
			goto IL_0131;
		}
		try
		{
			return await InvokeKeysV3Async(ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (HttpRequestException ex2) when (IsTokenRejection(ex2))
		{
			Log.Warning<int>((Exception)ex2, "API keys/v3 rejected token (HTTP {StatusCode}). Re-registering user and retrying.", (int)ex2.StatusCode.Value);
			if (!(await TryReRegisterUserAsync(ct)))
			{
				throw;
			}
			try
			{
				return await InvokeKeysV3Async(ct);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex4) when (IsApiUnreachable(ex4))
			{
				Log.Warning<string>(ex4, "API keys/v3 unreachable after re-registration: {Message}. Attempting on-chain fallback.", ex4.Message);
				List<VpnServerDto> onChain = await TryOnChainFallbackAsync(ct);
				if (onChain != null)
				{
					return onChain;
				}
				if (!(ex4 is Exception source))
				{
					throw ex4;
				}
				ExceptionDispatchInfo.Capture(source).Throw();
			}
			catch (Exception ex5)
			{
				Log.Error<string>(ex5, "API keys/v3 still failing after re-registration: {Message}", ex5.Message);
				throw;
			}
		}
		catch (Exception ex6)
		{
			Exception ex7 = ex6;
			if (!IsApiUnreachable(ex7))
			{
				Log.Warning<string>(ex7, "API keys/v3 call failed: {Message}. Not falling back to on-chain.", ex7.Message);
				throw;
			}
			Log.Warning<string>(ex7, "API keys/v3 call failed (unreachable): {Message}. Attempting on-chain fallback.", ex7.Message);
			List<VpnServerDto> onChain2 = await TryOnChainFallbackAsync(ct);
			if (onChain2 != null)
			{
				return onChain2;
			}
			if (!(ex6 is Exception source2))
			{
				throw ex6;
			}
			ExceptionDispatchInfo.Capture(source2).Throw();
		}
		throw null;
		IL_0131:
		if (num != 0)
		{
			_vpnServersCache.CacheServers(response.Servers);
			return response.Servers;
		}
		return new List<VpnServerDto>();
	}

	private async Task<List<VpnServerDto>> InvokeKeysV3Async(CancellationToken ct)
	{
		string deviceId = await _deviceIdService.GetDeviceId();
		ServerKeysV2Response servers = await _apiService.GetServerKeysV3Async(_settings.UserToken, deviceId, ct);
		int num;
		if (servers != null && servers.Success)
		{
			List<VpnServerDto> servers2 = servers.Servers;
			if (servers2 != null)
			{
				num = ((servers2.Count > 0) ? 1 : 0);
				goto IL_0170;
			}
		}
		num = 0;
		goto IL_0170;
		IL_0170:
		if (num != 0)
		{
			_vpnServersCache.CacheServers(servers.Servers);
			return servers.Servers;
		}
		return new List<VpnServerDto>();
	}

	private async Task<bool> TryReRegisterUserAsync(CancellationToken ct)
	{
		try
		{
			string token = await _authProvider.TryRestoreToken(ct);
			if (string.IsNullOrEmpty(token))
			{
				Log.Information("No prior token to restore; auto-registering a new user token.");
				token = await _authProvider.CreateNewToken(ct);
			}
			if (string.IsNullOrEmpty(token))
			{
				Log.Warning("Re-registration failed: no token obtained from restore or create.");
				return false;
			}
			_settings.UserToken = token;
			_settings.IsUserTokenValid = true;
			Log.Information("User re-registered successfully after token rejection.");
			return true;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			Log.Error<string>(ex3, "Failed to re-register user: {Message}", ex3.Message);
			return false;
		}
	}

	private async Task<List<VpnServerDto>?> TryOnChainFallbackAsync(CancellationToken ct)
	{
		if (_onChainService == null)
		{
			return null;
		}
		try
		{
			ServerKeysV2Response response = OnChainCredentialService.ToServerKeysResponse(await _onChainService.GetCredentialsAsync(ct));
			int num;
			if (response != null)
			{
				List<VpnServerDto> servers = response.Servers;
				if (servers != null)
				{
					num = ((servers.Count > 0) ? 1 : 0);
					goto IL_00f1;
				}
			}
			num = 0;
			goto IL_00f1;
			IL_00f1:
			if (num != 0)
			{
				_vpnServersCache.CacheServers(response.Servers);
				Log.Information<int>("On-chain fallback provided {Count} servers.", response.Servers.Count);
				return response.Servers;
			}
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "On-chain credential fallback also failed: {Message}", ex.Message);
		}
		return null;
	}

	private static bool IsTokenRejection(HttpRequestException ex)
	{
		HttpStatusCode? statusCode = ex.StatusCode;
		int result;
		if (statusCode.HasValue)
		{
			HttpStatusCode valueOrDefault = statusCode.GetValueOrDefault();
			if (valueOrDefault >= HttpStatusCode.BadRequest)
			{
				result = ((valueOrDefault < HttpStatusCode.InternalServerError) ? 1 : 0);
				goto IL_002b;
			}
		}
		result = 0;
		goto IL_002b;
		IL_002b:
		return (byte)result != 0;
	}

	private static bool IsApiUnreachable(Exception ex)
	{
		if (1 == 0)
		{
		}
		bool result = ex is HttpRequestException { StatusCode: var statusCode } ex2 && (!statusCode.HasValue || ex2.StatusCode.Value >= HttpStatusCode.InternalServerError);
		if (1 == 0)
		{
		}
		return result;
	}
}
