using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services.Interfaces;
using Ciphra.VPN.Common.Services.OnChain;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public class AuthProvider
{
	private readonly ApiService _apiService;

	private readonly IDeviceIdService _deviceIdService;

	private readonly OnChainCredentialService? _onChainService;

	private readonly SemaphoreSlim _onChainGate = new SemaphoreSlim(1, 1);

	private Task<TokenValidationResult>? _onChainValidation;

	public AuthProvider(ApiService apiService, IDeviceIdService deviceIdService, OnChainCredentialService? onChainService = null)
	{
		_apiService = apiService ?? throw new ArgumentNullException("apiService");
		_deviceIdService = deviceIdService ?? throw new ArgumentNullException("deviceIdService");
		_onChainService = onChainService;
	}

	public async Task<TokenValidationResult> ValidateToken(string? token, CancellationToken ct)
	{
		if (string.IsNullOrEmpty(token) || token.Contains("Not set"))
		{
			return new TokenValidationResult(IsValid: false, null, null);
		}
		if (OnChainCredentialService.ForceOnChainMode && _onChainService != null)
		{
			Task<TokenValidationResult> done = _onChainValidation;
			if (done?.IsCompleted ?? false)
			{
				return await done;
			}
			await _onChainGate.WaitAsync(ct);
			try
			{
				if (_onChainValidation == null)
				{
					_onChainValidation = ValidateViaOnChainAsync(_onChainService, ct);
				}
			}
			finally
			{
				_onChainGate.Release();
			}
			return await _onChainValidation;
		}
		try
		{
			token = token.Trim();
			string deviceId = await _deviceIdService.GetDeviceId();
			TokenCheckResponse validationResponse = await _apiService.CheckTokenAsync(token, deviceId, ct);
			if (validationResponse == null)
			{
				return new TokenValidationResult(IsValid: false, null, null);
			}
			return new TokenValidationResult(validationResponse.Valid, validationResponse, null);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (HttpRequestException ex2)
		{
			Log.Warning<string>((Exception)ex2, "API token check failed (network): {Message}. Attempting on-chain fallback.", ex2.Message);
			if (_onChainService != null)
			{
				try
				{
					TokenCheckResponse response = OnChainCredentialService.ToTokenCheckResponse(await _onChainService.GetCredentialsAsync(ct));
					Log.Information("On-chain credential fallback succeeded for token validation.");
					return new TokenValidationResult(response.Valid, response, null);
				}
				catch (Exception ex3)
				{
					Exception onChainEx = ex3;
					Log.Error<string>(onChainEx, "On-chain credential fallback also failed: {Message}", onChainEx.Message);
				}
			}
			Exception wrapException = new Exception("An error occurred while validating user auth token.", ex2);
			Log.Error<string>((Exception)ex2, "Error validating token: {Message}", ex2.Message);
			return new TokenValidationResult(IsValid: false, null, wrapException);
		}
		catch (Exception ex4)
		{
			Exception wrapException2 = new Exception("An error occurred while validating user auth token.", ex4);
			Log.Error<string>(ex4, "Error validating token: {Message}", ex4.Message);
			return new TokenValidationResult(IsValid: false, null, wrapException2);
		}
	}

	private static async Task<TokenValidationResult> ValidateViaOnChainAsync(OnChainCredentialService onChainService, CancellationToken ct)
	{
		Log.Information("ForceOnChainMode: skipping API, using on-chain credentials for token validation.");
		TokenCheckResponse response = OnChainCredentialService.ToTokenCheckResponse(await onChainService.GetCredentialsAsync(ct));
		return new TokenValidationResult(response.Valid, response, null);
	}

	public async Task<string> CreateNewToken(CancellationToken ct)
	{
		try
		{
			string deviceId = await _deviceIdService.GetDeviceId();
			CreateTokenResponse response = await _apiService.CreateTokenAsync(deviceId, ct);
			if (response == null || string.IsNullOrEmpty(response.Token))
			{
				throw new InvalidOperationException("Failed to create a new token.");
			}
			return response.Token;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Error creating new token: " + ex2.Message);
			throw;
		}
	}

	public async Task<string?> TryRestoreToken(CancellationToken ct)
	{
		try
		{
			string deviceId = await _deviceIdService.GetDeviceId();
			Log.Information<string>("Attempting to restore token for device {DeviceId}", deviceId);
			TokenRestoreResponse response = await _apiService.RestoreTokenAsync(deviceId, ct);
			if (response != null && response.Success && response.Restored && !string.IsNullOrEmpty(response.Token))
			{
				Log.Information<string>("Token successfully restored for device {DeviceId}", deviceId);
				return response.Token;
			}
			Log.Information<string>("No token found to restore for device {DeviceId}", deviceId);
			return null;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			Log.Error<string>(ex3, "Error restoring token: {Message}", ex3.Message);
			return null;
		}
	}
}
