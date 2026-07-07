using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Services.Interfaces;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public class OAuthSignInCoordinator
{
	public static readonly TimeSpan PendingAuthTtl = TimeSpan.FromMinutes(5L);

	private readonly IOAuthLoginLauncher? _loginLauncher;

	private readonly TimeProvider _timeProvider;

	private readonly Func<OAuthPkce.PendingAuth> _pendingFactory;

	private readonly object _pendingLock = new object();

	private OAuthPkce.PendingAuth? _pending;

	public IAppAnalytics? Analytics { get; set; }

	public bool HasPendingAttempt
	{
		get
		{
			lock (_pendingLock)
			{
				return _pending != null;
			}
		}
	}

	public OAuthSignInCoordinator(IOAuthLoginLauncher? loginLauncher, TimeProvider? timeProvider = null, Func<OAuthPkce.PendingAuth>? pendingFactory = null)
	{
		_loginLauncher = loginLauncher;
		_timeProvider = timeProvider ?? TimeProvider.System;
		_pendingFactory = pendingFactory ?? ((Func<OAuthPkce.PendingAuth>)(() => OAuthPkce.Create(_timeProvider)));
	}

	public async Task<OAuthSignInStartResult> StartAsync(CancellationToken ct = default(CancellationToken))
	{
		TrackEvent("auth_signin_started");
		if (_loginLauncher == null)
		{
			Log.Warning("OAuth sign-in invoked with no IOAuthLoginLauncher registered");
			TrackEvent("auth_signin_result", ("outcome", "unavailable"));
			return new OAuthSignInStartResult(OAuthSignInStartStatus.Unavailable);
		}
		OAuthPkce.PendingAuth attempt;
		lock (_pendingLock)
		{
			attempt = ((_pending != null && _timeProvider.GetUtcNow() - _pending.CreatedAt <= PendingAuthTtl) ? _pending : (_pending = _pendingFactory()));
		}
		string authorizeUrl = BuildAuthorizeUrl(attempt.Challenge, attempt.State);
		Log.Information<string>("OAuth sign-in opening login URL (launcher={Launcher})", _loginLauncher.GetType().Name);
		try
		{
			await _loginLauncher.LaunchLoginAsync(authorizeUrl, ct).ConfigureAwait(continueOnCapturedContext: false);
			return new OAuthSignInStartResult(OAuthSignInStartStatus.Started);
		}
		catch (OperationCanceledException)
		{
			ClearIfCurrent(attempt);
			TrackEvent("auth_signin_result", ("outcome", "cancelled"));
			return new OAuthSignInStartResult(OAuthSignInStartStatus.Cancelled);
		}
		catch (Exception ex2)
		{
			ClearIfCurrent(attempt);
			Log.Error(ex2, "Failed to launch OAuth login page");
			TrackEvent("auth_signin_result", ("outcome", "launch_failed"));
			return new OAuthSignInStartResult(OAuthSignInStartStatus.LaunchFailed, ex2);
		}
	}

	public OAuthCallbackValidationResult HandleCallback(string callbackUri)
	{
		OAuthCallbackParseResult oAuthCallbackParseResult = OAuthCallbackParser.Parse(callbackUri);
		if (oAuthCallbackParseResult.Status == OAuthCallbackParseStatus.Empty)
		{
			return new OAuthCallbackValidationResult(OAuthCallbackStatus.Ignored);
		}
		if (oAuthCallbackParseResult.Status == OAuthCallbackParseStatus.InvalidUri)
		{
			Log.Warning("OAuth callback URI is not a valid absolute URI");
			return new OAuthCallbackValidationResult(OAuthCallbackStatus.Ignored);
		}
		if (oAuthCallbackParseResult.Status == OAuthCallbackParseStatus.UnexpectedShape)
		{
			Log.Warning<string, string, string>("OAuth callback URI shape unexpected: {Scheme}://{Host}{Path}", oAuthCallbackParseResult.Scheme, oAuthCallbackParseResult.Host, oAuthCallbackParseResult.Path);
			return new OAuthCallbackValidationResult(OAuthCallbackStatus.Ignored);
		}
		OAuthCallback callback = oAuthCallbackParseResult.Callback;
		OAuthPkce.PendingAuth pending;
		lock (_pendingLock)
		{
			if (_pending == null)
			{
				Log.Information("OAuth callback received with no pending attempt; ignoring");
				return new OAuthCallbackValidationResult(OAuthCallbackStatus.Ignored);
			}
			pending = _pending;
			if (_timeProvider.GetUtcNow() - pending.CreatedAt > PendingAuthTtl)
			{
				Log.Warning("OAuth callback received past 5-minute TTL; clearing pending");
				_pending = null;
				TrackEvent("auth_signin_result", ("outcome", "expired"));
				return new OAuthCallbackValidationResult(OAuthCallbackStatus.Expired, null, null, "Sign-in session expired. Please try again.");
			}
			if (!string.IsNullOrEmpty(callback.Error))
			{
				Log.Warning<string, string>("OAuth callback error: {Error} {Description}", callback.Error, callback.ErrorDescription);
				_pending = null;
				TrackEvent("auth_signin_result", ("outcome", "authorization_error"), ("error", callback.Error));
				return new OAuthCallbackValidationResult(OAuthCallbackStatus.AuthorizationError, null, null, callback.ErrorDescription ?? callback.Error);
			}
			if (!string.Equals(callback.State, pending.State, StringComparison.Ordinal))
			{
				Log.Warning("OAuth callback state mismatch; rejecting");
				_pending = null;
				TrackEvent("auth_signin_result", ("outcome", "state_mismatch"));
				return new OAuthCallbackValidationResult(OAuthCallbackStatus.StateMismatch, null, null, "Sign-in failed. Please try again.");
			}
			if (string.IsNullOrEmpty(callback.Code))
			{
				Log.Warning("OAuth callback missing code");
				_pending = null;
				TrackEvent("auth_signin_result", ("outcome", "missing_code"));
				return new OAuthCallbackValidationResult(OAuthCallbackStatus.MissingCode, null, null, "Sign-in failed. Please try again.");
			}
			_pending = null;
		}
		Log.Debug<int>("OAuth callback validated. Code length={CodeLength}", callback.Code.Length);
		return new OAuthCallbackValidationResult(OAuthCallbackStatus.Accepted, callback.Code, pending.Verifier);
	}

	public void Cancel()
	{
		lock (_pendingLock)
		{
			_pending = null;
		}
	}

	public static string BuildAuthorizeUrl(string codeChallenge, string state)
	{
		Dictionary<string, string> nameValueCollection = new Dictionary<string, string>
		{
			["client_id"] = "ciphra-app",
			["response_type"] = "code",
			["redirect_uri"] = "ciphra://auth/callback",
			["state"] = state,
			["code_challenge"] = codeChallenge,
			["code_challenge_method"] = "S256"
		};
		using FormUrlEncodedContent formUrlEncodedContent = new FormUrlEncodedContent(nameValueCollection);
		UriBuilder uriBuilder = new UriBuilder("https://www.ciphravpn.com/auth/authorize")
		{
			Query = formUrlEncodedContent.ReadAsStringAsync().GetAwaiter().GetResult()
		};
		return uriBuilder.Uri.AbsoluteUri;
	}

	private void ClearIfCurrent(OAuthPkce.PendingAuth attempt)
	{
		lock (_pendingLock)
		{
			if ((object)_pending == attempt)
			{
				_pending = null;
			}
		}
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
