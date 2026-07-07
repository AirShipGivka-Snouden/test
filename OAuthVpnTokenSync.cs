using System;
using Ciphra.VPN.Common.App;
using Serilog;

namespace Ciphra.VPN.Common.ViewModels;

public class OAuthVpnTokenSync
{
	private readonly Settings? _settings;

	private readonly Func<string?>? _getUserToken;

	private readonly Action<string?>? _setUserToken;

	private readonly Action<bool>? _setUserTokenValid;

	public OAuthVpnTokenSync(Settings? settings, AccountInfoViewModel? accountInfo)
		: this(settings, (accountInfo == null) ? null : ((Func<string>)(() => accountInfo.UserToken)), (accountInfo == null) ? null : ((Action<string>)delegate(string? token)
		{
			accountInfo.UserToken = token;
		}), (accountInfo == null) ? null : ((Action<bool>)delegate(bool valid)
		{
			accountInfo.IsUserTokenValid = valid;
		}))
	{
	}

	public OAuthVpnTokenSync(Settings? settings, Func<string?>? getUserToken, Action<string?>? setUserToken, Action<bool>? setUserTokenValid)
	{
		_settings = settings;
		_getUserToken = getUserToken;
		_setUserToken = setUserToken;
		_setUserTokenValid = setUserTokenValid;
	}

	public void ApplyUserBoundToken(string? userBoundToken)
	{
		if (_settings == null || _getUserToken == null || _setUserToken == null || _setUserTokenValid == null || string.IsNullOrWhiteSpace(userBoundToken))
		{
			return;
		}
		if (string.IsNullOrEmpty(_settings.AnonymousUserToken))
		{
			string userToken = _settings.UserToken;
			if (!string.IsNullOrWhiteSpace(userToken) && userToken != "Not set" && !string.Equals(userToken, userBoundToken, StringComparison.Ordinal))
			{
				_settings.AnonymousUserToken = userToken;
				Log.Information("Backed up anonymous token to AnonymousUserToken slot");
			}
		}
		if (!string.Equals(_getUserToken(), userBoundToken, StringComparison.Ordinal))
		{
			_setUserToken(userBoundToken);
			_setUserTokenValid(obj: true);
		}
	}

	public void RestoreAnonymousToken()
	{
		if (_settings != null && _getUserToken != null && _setUserToken != null)
		{
			string anonymousUserToken = _settings.AnonymousUserToken;
			if (string.IsNullOrWhiteSpace(anonymousUserToken))
			{
				Log.Debug("No anonymous token backup; nothing to restore");
			}
			else if (!string.Equals(_getUserToken(), anonymousUserToken, StringComparison.Ordinal))
			{
				_setUserToken(anonymousUserToken);
				Log.Information("Restored anonymous token from AnonymousUserToken slot");
			}
		}
	}
}
