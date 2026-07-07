using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.Models;
using Newtonsoft.Json;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public class OAuthApiClient
{
	private const string DefaultApiBaseUrl = "https://api.ciphravpn.com";

	private const string ExchangeEndpoint = "/auth/exchange";

	private const string RefreshEndpoint = "/auth/refresh";

	private const string LogoutEndpoint = "/auth/logout";

	private const string MeEndpoint = "/me";

	private readonly HttpClient _httpClient;

	public OAuthApiClient(HttpClient httpClient)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException("httpClient");
	}

	public async Task<OAuthTokenResponse> ExchangeAsync(string code, string codeVerifier, CancellationToken ct = default(CancellationToken))
	{
		OAuthExchangeRequest request = new OAuthExchangeRequest
		{
			ClientId = "ciphra-app",
			Code = code,
			CodeVerifier = codeVerifier,
			RedirectUri = "ciphra://auth/callback"
		};
		return await PostJsonAsync<OAuthExchangeRequest, OAuthTokenResponse>("/auth/exchange", request, null, ct).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<OAuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken ct = default(CancellationToken))
	{
		OAuthRefreshRequest request = new OAuthRefreshRequest
		{
			RefreshToken = refreshToken
		};
		return await PostJsonAsync<OAuthRefreshRequest, OAuthTokenResponse>("/auth/refresh", request, null, ct).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task LogoutAsync(string refreshToken, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			OAuthLogoutRequest request = new OAuthLogoutRequest
			{
				RefreshToken = refreshToken
			};
			using HttpRequestMessage httpRequest = new HttpRequestMessage(requestUri: BuildUri("/auth/logout"), method: HttpMethod.Post)
			{
				Content = new StringContent(JsonConvert.SerializeObject((object)request), Encoding.UTF8, "application/json")
			};
			using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(continueOnCapturedContext: false);
			Log.Debug<int>("OAuth logout returned {Status}", (int)response.StatusCode);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Warning(ex2, "OAuth logout failed (will still clear local state)");
		}
	}

	public async Task<OAuthMeResponse> GetMeAsync(string accessToken, CancellationToken ct = default(CancellationToken))
	{
		using HttpRequestMessage httpRequest = new HttpRequestMessage(requestUri: BuildUri("/me"), method: HttpMethod.Get);
		httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(continueOnCapturedContext: false);
		string body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			throw new OAuthInvalidTokenException(TryDeserializeError(body));
		}
		if (!response.IsSuccessStatusCode)
		{
			throw new OAuthApiException((int)response.StatusCode, TryDeserializeError(body));
		}
		OAuthMeResponse me = JsonConvert.DeserializeObject<OAuthMeResponse>(body);
		if (me == null)
		{
			throw new OAuthApiException((int)response.StatusCode, new OAuthErrorResponse
			{
				Error = "invalid_response"
			});
		}
		return me;
	}

	private async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string endpoint, TRequest body, string? accessToken, CancellationToken ct) where TResponse : class
	{
		using HttpRequestMessage httpRequest = new HttpRequestMessage(requestUri: BuildUri(endpoint), method: HttpMethod.Post)
		{
			Content = new StringContent(JsonConvert.SerializeObject((object)body), Encoding.UTF8, "application/json")
		};
		if (!string.IsNullOrEmpty(accessToken))
		{
			httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		}
		using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(continueOnCapturedContext: false);
		string responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		if (response.StatusCode == HttpStatusCode.BadRequest)
		{
			OAuthErrorResponse err = TryDeserializeError(responseBody);
			if (string.Equals(err.Error, "invalid_grant", StringComparison.OrdinalIgnoreCase))
			{
				throw new OAuthInvalidGrantException(err);
			}
			throw new OAuthApiException((int)response.StatusCode, err);
		}
		if (!response.IsSuccessStatusCode)
		{
			throw new OAuthApiException((int)response.StatusCode, TryDeserializeError(responseBody));
		}
		TResponse parsed = JsonConvert.DeserializeObject<TResponse>(responseBody);
		if (parsed == null)
		{
			throw new OAuthApiException((int)response.StatusCode, new OAuthErrorResponse
			{
				Error = "invalid_response"
			});
		}
		return parsed;
	}

	private Uri BuildUri(string endpoint)
	{
		string uriString = ApiService.BaseUrlOverride ?? "https://api.ciphravpn.com";
		return new Uri(new Uri(uriString), endpoint);
	}

	private static OAuthErrorResponse TryDeserializeError(string body)
	{
		try
		{
			return JsonConvert.DeserializeObject<OAuthErrorResponse>(body) ?? new OAuthErrorResponse
			{
				Error = "unknown_error"
			};
		}
		catch
		{
			return new OAuthErrorResponse
			{
				Error = "unparseable_error_response"
			};
		}
	}
}
