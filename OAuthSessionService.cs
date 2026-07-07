using System;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services.Interfaces;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public class OAuthSessionService
{
	private readonly OAuthApiClient? _api;

	private readonly IOAuthSecureStorage? _storage;

	private readonly object _refreshLock = new object();

	private string? _accessToken;

	private string? _sessionId;

	private Task<OAuthSessionOperationResult>? _inFlightRefresh;

	public IAppAnalytics? Analytics { get; set; }

	public OAuthSessionService(OAuthApiClient? api, IOAuthSecureStorage? storage)
	{
		_api = api;
		_storage = storage;
	}

	public async Task<OAuthSessionOperationResult> ExchangeAndLoadAccountAsync(string code, string verifier, CancellationToken ct = default(CancellationToken))
	{
		OAuthSessionOperationResult result = await ExchangeAndLoadAccountCoreAsync(code, verifier, ct).ConfigureAwait(continueOnCapturedContext: false);
		TrackEvent("auth_signin_result", ("outcome", OutcomeName(result.Status)));
		return result;
	}

	private async Task<OAuthSessionOperationResult> ExchangeAndLoadAccountCoreAsync(string code, string verifier, CancellationToken ct)
	{
		if (_api == null || _storage == null)
		{
			Log.Warning("OAuth API client / storage not registered; cannot complete exchange");
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.Unavailable, null, "Sign-in is not available on this build");
		}
		try
		{
			OAuthTokenResponse tokens = await _api.ExchangeAsync(code, verifier, ct).ConfigureAwait(continueOnCapturedContext: false);
			await _storage.SaveAsync(new OAuthStoredSession(tokens.RefreshToken, tokens.SessionId), ct).ConfigureAwait(continueOnCapturedContext: false);
			_accessToken = tokens.AccessToken;
			_sessionId = tokens.SessionId;
			return await FetchMeWithRefreshRetryAsync(tokens.AccessToken, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OAuthInvalidGrantException ex)
		{
			Log.Warning((Exception)ex, "OAuth exchange rejected; clearing local state");
			await ClearStoredSessionAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.SessionExpired, null, "Sign-in failed. Please try again.");
		}
		catch (Exception ex2)
		{
			Log.Error(ex2, "OAuth exchange failed");
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.Failed, null, "Sign-in failed: " + ex2.Message);
		}
	}

	public async Task<OAuthSessionOperationResult> RestoreSessionAsync(CancellationToken ct = default(CancellationToken))
	{
		OAuthSessionOperationResult result = await RestoreSessionCoreAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		OAuthSessionOperationStatus status = result.Status;
		if ((status == OAuthSessionOperationStatus.Success || (uint)(status - 3) <= 2u) ? true : false)
		{
			TrackEvent("auth_restore_result", ("outcome", OutcomeName(result.Status)));
		}
		return result;
	}

	private async Task<OAuthSessionOperationResult> RestoreSessionCoreAsync(CancellationToken ct)
	{
		if (_api == null || _storage == null)
		{
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.Unavailable);
		}
		OAuthStoredSession stored;
		try
		{
			stored = await _storage.LoadAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to read OAuth secure storage on startup");
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.RefreshFailed);
		}
		if (stored == null)
		{
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.NoStoredSession);
		}
		Log.Information("Found stored OAuth session; refreshing on startup");
		_sessionId = stored.SessionId;
		OAuthSessionOperationResult refreshed = await EnsureRefreshedAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		if (refreshed.Status != OAuthSessionOperationStatus.Success || _accessToken == null)
		{
			return refreshed;
		}
		return await FetchMeWithRefreshRetryAsync(_accessToken, ct).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<OAuthSessionOperationResult> RefreshAccountAsync(CancellationToken ct = default(CancellationToken))
	{
		OAuthSessionOperationResult result = await RefreshAccountCoreAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		if (result.Status != OAuthSessionOperationStatus.Unavailable)
		{
			TrackEvent("auth_refresh_result", ("outcome", OutcomeName(result.Status)));
		}
		return result;
	}

	private async Task<OAuthSessionOperationResult> RefreshAccountCoreAsync(CancellationToken ct)
	{
		if (_api == null)
		{
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.Unavailable);
		}
		if (_accessToken == null)
		{
			OAuthSessionOperationResult refreshed = await EnsureRefreshedAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			if (refreshed.Status != OAuthSessionOperationStatus.Success || _accessToken == null)
			{
				return refreshed;
			}
		}
		return await FetchMeWithRefreshRetryAsync(_accessToken, ct).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task SignOutAsync(CancellationToken ct = default(CancellationToken))
	{
		string refreshToBeRevoked = null;
		if (_storage != null)
		{
			try
			{
				refreshToBeRevoked = (await _storage.LoadAsync(ct).ConfigureAwait(continueOnCapturedContext: false))?.RefreshToken;
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log.Warning(ex2, "Failed to read stored refresh during sign-out");
			}
		}
		await ClearStoredSessionAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		if (_api != null && !string.IsNullOrEmpty(refreshToBeRevoked))
		{
			_api.LogoutAsync(refreshToBeRevoked, ct);
		}
		TrackEvent("auth_signout");
	}

	public async Task ClearStoredSessionAsync(CancellationToken ct = default(CancellationToken))
	{
		_accessToken = null;
		_sessionId = null;
		if (_storage == null)
		{
			return;
		}
		try
		{
			await _storage.ClearAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to clear OAuth secure storage");
		}
	}

	private Task<OAuthSessionOperationResult> EnsureRefreshedAsync(CancellationToken ct)
	{
		lock (_refreshLock)
		{
			if (_inFlightRefresh != null && !_inFlightRefresh.IsCompleted)
			{
				return _inFlightRefresh;
			}
			_inFlightRefresh = RefreshOnceAsync(ct);
			return _inFlightRefresh;
		}
	}

	private async Task<OAuthSessionOperationResult> RefreshOnceAsync(CancellationToken ct)
	{
		if (_api == null || _storage == null)
		{
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.Unavailable);
		}
		OAuthStoredSession stored;
		try
		{
			stored = await _storage.LoadAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to read OAuth secure storage before refresh");
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.RefreshFailed);
		}
		if (stored == null)
		{
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.NoStoredSession);
		}
		try
		{
			OAuthTokenResponse tokens = await _api.RefreshAsync(stored.RefreshToken, ct).ConfigureAwait(continueOnCapturedContext: false);
			OAuthStoredSession newSession = new OAuthStoredSession(tokens.RefreshToken, stored.SessionId);
			await _storage.SaveAsync(newSession, ct).ConfigureAwait(continueOnCapturedContext: false);
			_accessToken = tokens.AccessToken;
			_sessionId = stored.SessionId;
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.Success);
		}
		catch (OAuthInvalidGrantException ex2)
		{
			Log.Warning((Exception)ex2, "Refresh rejected (invalid_grant); wiping local state and forcing re-login");
			await ClearStoredSessionAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.SessionExpired, null, "Session expired. Please sign in again.");
		}
		catch (Exception ex3)
		{
			Log.Warning(ex3, "Refresh failed (transient); keeping stored refresh for retry");
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.RefreshFailed);
		}
	}

	private async Task<OAuthSessionOperationResult> FetchMeWithRefreshRetryAsync(string accessToken, CancellationToken ct)
	{
		if (_api == null)
		{
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.Unavailable);
		}
		try
		{
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.Success, await _api.GetMeAsync(accessToken, ct).ConfigureAwait(continueOnCapturedContext: false));
		}
		catch (OAuthInvalidTokenException)
		{
			Log.Information("/me returned invalid_token; refreshing and retrying");
			OAuthSessionOperationResult refreshed = await EnsureRefreshedAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			if (refreshed.Status != OAuthSessionOperationStatus.Success || _accessToken == null)
			{
				return refreshed;
			}
			try
			{
				return new OAuthSessionOperationResult(OAuthSessionOperationStatus.Success, await _api.GetMeAsync(_accessToken, ct).ConfigureAwait(continueOnCapturedContext: false));
			}
			catch (Exception ex2)
			{
				Log.Error(ex2, "/me failed after refresh-retry");
				return new OAuthSessionOperationResult(OAuthSessionOperationStatus.Failed, null, "Could not load account info");
			}
		}
		catch (Exception ex3)
		{
			Log.Error(ex3, "/me request failed");
			return new OAuthSessionOperationResult(OAuthSessionOperationStatus.Failed, null, "Could not load account info");
		}
	}

	private static string OutcomeName(OAuthSessionOperationStatus status)
	{
		if (1 == 0)
		{
		}
		string result = status switch
		{
			OAuthSessionOperationStatus.Success => "success", 
			OAuthSessionOperationStatus.SessionExpired => "session_expired", 
			OAuthSessionOperationStatus.RefreshFailed => "refresh_failed", 
			OAuthSessionOperationStatus.Failed => "failed", 
			OAuthSessionOperationStatus.Unavailable => "unavailable", 
			OAuthSessionOperationStatus.NoStoredSession => "no_stored_session", 
			_ => "unknown", 
		};
		if (1 == 0)
		{
		}
		return result;
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
