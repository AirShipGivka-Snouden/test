using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.Models;
using Newtonsoft.Json;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public class ApiService
{
	private const string CreateTokenEndpoint = "token/create";

	private const string CheckTokenEndpoint = "token/check";

	private const string GetServerKeysEndpoint = "keys";

	private const string GetServerKeysV2Endpoint = "keys/v2";

	private const string GetServerKeysV3Endpoint = "keys/v3";

	private const string UpdateSubscriptionEndpoint = "subscription/update";

	private const string StripeCheckoutEndpoint = "stripe/checkout";

	private const string StripePortalEndpoint = "stripe/portal";

	private const string PurchasePricesEndpoint = "purchase/prices";

	private const string PurchaseCheckoutEndpoint = "purchase/checkout";

	private const string CryptoCheckoutEndpoint = "coinbase/checkout";

	private const string TrialCheckEndpoint = "trial/check";

	private const string TokenRestoreEndpoint = "token/restore";

	private const string DefaultBaseUrl = "https://api.ciphravpn.com";

	private readonly HttpClient _httpClient;

	public static string? BaseUrlOverride { get; set; }

	public ApiService(HttpClient httpClient)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException("httpClient");
		string uriString = BaseUrlOverride ?? "https://api.ciphravpn.com";
		if (_httpClient.BaseAddress == null)
		{
			_httpClient.BaseAddress = new Uri(uriString);
		}
		Log.Information<Uri>("ApiService initialized with base URL: {BaseUrl}", _httpClient.BaseAddress);
	}

	private static void ValidateNotNullOrEmpty(string value, string parameterName)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException(parameterName + " cannot be empty", parameterName);
		}
	}

	private static StringContent CreateJsonContent(object obj)
	{
		string content = JsonConvert.SerializeObject(obj);
		return new StringContent(content, Encoding.UTF8, "application/json");
	}

	private async Task<T> SendRequestAsync<T>(HttpMethod method, string endpoint, object? requestBody = null, Dictionary<string, string>? queryParams = null, CancellationToken cancellationToken = default(CancellationToken), int maxRetries = 3) where T : class
	{
		Uri requestUri = await BuildRequestUriAsync(endpoint, queryParams, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		using HttpRequestMessage request = new HttpRequestMessage(method, requestUri);
		if (requestBody != null)
		{
			request.Content = CreateJsonContent(requestBody);
		}
		Log.Information<HttpMethod, Uri, string>("Sending {Method} request to {Endpoint} with body: {Body}", method, requestUri, (requestBody != null) ? JsonConvert.SerializeObject(requestBody) : "none");
		using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
		string responseString = await response.Content.ReadAsStringAsync(cancellationToken);
		Log.Information<string, HttpStatusCode, string>("Request to {Endpoint} completed with status code {StatusCode}: {Response}", endpoint, response.StatusCode, (responseString.Length > 1000) ? (responseString.Substring(0, 1000) + "...") : responseString);
		response.EnsureSuccessStatusCode();
		T result = JsonConvert.DeserializeObject<T>(responseString);
		if (result == null)
		{
			throw new JsonSerializationException("Failed to deserialize " + typeof(T).Name + " response");
		}
		return result;
	}

	private async Task<Uri> BuildRequestUriAsync(string endpoint, Dictionary<string, string>? queryParams, CancellationToken cancellationToken)
	{
		UriBuilder builder = new UriBuilder(new Uri(_httpClient.BaseAddress, endpoint));
		if (queryParams?.Any() ?? false)
		{
			using FormUrlEncodedContent content = new FormUrlEncodedContent(queryParams);
			UriBuilder uriBuilder = builder;
			uriBuilder.Query = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		return builder.Uri;
	}

	private Task<T> GetAsync<T>(string endpoint, Dictionary<string, string>? queryParams = null, CancellationToken cancellationToken = default(CancellationToken), int maxRetries = 3) where T : class
	{
		return SendRequestAsync<T>(HttpMethod.Get, endpoint, null, queryParams, cancellationToken, maxRetries);
	}

	private Task<T> PostAsync<T>(string endpoint, object? requestBody = null, CancellationToken cancellationToken = default(CancellationToken), int maxRetries = 3) where T : class
	{
		return SendRequestAsync<T>(HttpMethod.Post, endpoint, requestBody, null, cancellationToken, maxRetries);
	}

	public async Task<CreateTokenResponse> CreateTokenAsync(string deviceId, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateNotNullOrEmpty(deviceId, "deviceId");
		Log.Information<string>("Creating new user token for device {DeviceId}", deviceId);
		CreateTokenRequest request = new CreateTokenRequest
		{
			DeviceId = deviceId
		};
		return await PostAsync<CreateTokenResponse>("token/create", request, cancellationToken);
	}

	public async Task<TokenCheckResponse> CheckTokenAsync(string token, string deviceId, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateNotNullOrEmpty(token, "token");
		ValidateNotNullOrEmpty(deviceId, "deviceId");
		Log.Information<string>("Checking token validity for device {DeviceId}", deviceId);
		TokenCheckRequest request = new TokenCheckRequest
		{
			Token = token,
			DeviceId = deviceId
		};
		return await PostAsync<TokenCheckResponse>("token/check", request, cancellationToken);
	}

	public async Task<ServerKeysResponse> GetServerKeysAsync(string token, string deviceId, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateNotNullOrEmpty(token, "token");
		ValidateNotNullOrEmpty(deviceId, "deviceId");
		Log.Information<string>("Getting server keys for device {DeviceId}", deviceId);
		Dictionary<string, string> queryParams = new Dictionary<string, string>
		{
			["token"] = token,
			["device_id"] = deviceId
		};
		return await GetAsync<ServerKeysResponse>("keys", queryParams, cancellationToken);
	}

	public async Task<ServerKeysV2Response> GetServerKeysV2Async(string token, string deviceId, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateNotNullOrEmpty(token, "token");
		ValidateNotNullOrEmpty(deviceId, "deviceId");
		Log.Information<string>("Getting server keys v2 for device {DeviceId}", deviceId);
		Dictionary<string, string> queryParams = new Dictionary<string, string>
		{
			["token"] = token,
			["device_id"] = deviceId
		};
		return await GetAsync<ServerKeysV2Response>("keys/v2", queryParams, cancellationToken);
	}

	public async Task<ServerKeysV2Response> GetServerKeysV3Async(string token, string deviceId, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateNotNullOrEmpty(token, "token");
		ValidateNotNullOrEmpty(deviceId, "deviceId");
		Log.Information<string>("Getting server keys v3 for device {DeviceId}", deviceId);
		Dictionary<string, string> queryParams = new Dictionary<string, string>
		{
			["token"] = token,
			["device_id"] = deviceId
		};
		return await GetAsync<ServerKeysV2Response>("keys/v3", queryParams, cancellationToken);
	}

	public async Task<UpdateSubscriptionResponse> UpdateSubscriptionAsync(string platform, string token, int subscriptionStatus, DateTimeOffset? subscriptionExpirationDate, string deviceId, string? purchaseToken = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateNotNullOrEmpty(platform, "platform");
		ValidateNotNullOrEmpty(token, "token");
		ValidateNotNullOrEmpty(deviceId, "deviceId");
		Log.Information<string, int>("Updating subscription for device {DeviceId} to level {SubscriptionStatus}", deviceId, subscriptionStatus);
		UpdateSubscriptionRequest request = new UpdateSubscriptionRequest
		{
			Platform = platform,
			Token = token,
			SubscriptionStatus = subscriptionStatus,
			SubscriptionExpiryDay = subscriptionExpirationDate?.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
			DeviceId = deviceId,
			PurchaseToken = purchaseToken
		};
		return await PostAsync<UpdateSubscriptionResponse>("subscription/update", request, cancellationToken);
	}

	public async Task<StripeCheckoutResponse> CreateStripeCheckoutAsync(string token, string deviceId, string priceId, string successUrl, string cancelUrl, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateNotNullOrEmpty(token, "token");
		ValidateNotNullOrEmpty(deviceId, "deviceId");
		ValidateNotNullOrEmpty(priceId, "priceId");
		ValidateNotNullOrEmpty(successUrl, "successUrl");
		ValidateNotNullOrEmpty(cancelUrl, "cancelUrl");
		Log.Information<string, string>("Creating Stripe checkout session for device {DeviceId} with price {PriceId}", deviceId, priceId);
		StripeCheckoutRequest request = new StripeCheckoutRequest
		{
			Token = token,
			DeviceId = deviceId,
			PriceId = priceId,
			SuccessUrl = successUrl,
			CancelUrl = cancelUrl
		};
		return await PostAsync<StripeCheckoutResponse>("stripe/checkout", request, cancellationToken);
	}

	public async Task<StripePortalResponse> GetStripePortalAsync(string customerId, string returnUrl, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateNotNullOrEmpty(customerId, "customerId");
		ValidateNotNullOrEmpty(returnUrl, "returnUrl");
		Log.Information<string>("Getting Stripe portal URL for customer {CustomerId}", customerId);
		StripePortalRequest request = new StripePortalRequest
		{
			CustomerId = customerId,
			ReturnUrl = returnUrl
		};
		return await PostAsync<StripePortalResponse>("stripe/portal", request, cancellationToken);
	}

	public async Task<PurchasePricesResponse> GetPurchasePricesAsync(IEnumerable<string>? purchaseIds = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		Dictionary<string, string> queryParams = new Dictionary<string, string>();
		if (purchaseIds != null)
		{
			string[] ids = purchaseIds.Where((string id) => !string.IsNullOrWhiteSpace(id)).ToArray();
			if (ids.Length != 0)
			{
				queryParams["ids"] = string.Join(",", ids);
			}
		}
		Log.Information<string>("Getting purchase prices. IDs provided: {Ids}", queryParams.ContainsKey("ids") ? queryParams["ids"] : "none");
		return await GetAsync<PurchasePricesResponse>("purchase/prices", (queryParams.Count > 0) ? queryParams : null, cancellationToken);
	}

	public async Task<PurchaseCheckoutResponse> CreatePurchaseCheckoutAsync(string token, string deviceId, string purchaseId, string localeIso, string transactionId, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateNotNullOrEmpty(token, "token");
		ValidateNotNullOrEmpty(deviceId, "deviceId");
		ValidateNotNullOrEmpty(purchaseId, "purchaseId");
		ValidateNotNullOrEmpty(localeIso, "localeIso");
		Log.Information<string, string>("Creating purchase checkout for device {DeviceId} purchase {PurchaseId}", deviceId, purchaseId);
		PurchaseCheckoutRequest request = new PurchaseCheckoutRequest
		{
			Token = token,
			DeviceId = deviceId,
			PurchaseId = purchaseId,
			LocaleIso = localeIso,
			TransactionId = transactionId
		};
		for (int attempt = 1; attempt <= 4; attempt++)
		{
			PurchaseCheckoutResponse response = await PostAsync<PurchaseCheckoutResponse>("purchase/checkout", request, cancellationToken);
			if (response.Success || !IsStripeUpstreamTransientError(response.Error))
			{
				return response;
			}
			if (attempt == 4)
			{
				Log.Warning<int, string>("Purchase checkout exhausted {Attempts} attempts with Stripe upstream transient error: {Error}", 4, response.Error);
				return response;
			}
			int delayMs = attempt * 500;
			Log.Warning("Purchase checkout hit Stripe upstream transient error (attempt {Attempt}/{Max}): {Error}. Retrying in {DelayMs}ms.", new object[4] { attempt, 4, response.Error, delayMs });
			await Task.Delay(delayMs, cancellationToken);
		}
		throw new UnreachableException("Purchase checkout retry loop completed without returning.");
	}

	private static bool IsStripeUpstreamTransientError(string? error)
	{
		if (string.IsNullOrEmpty(error))
		{
			return false;
		}
		return error.Contains("Cloudflare 1006", StringComparison.OrdinalIgnoreCase) || error.Contains("Network error reaching Stripe", StringComparison.OrdinalIgnoreCase);
	}

	public async Task<CryptoCheckoutResponse> CreateCryptoCheckoutAsync(string productType, string userToken, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateNotNullOrEmpty(productType, "productType");
		ValidateNotNullOrEmpty(userToken, "userToken");
		Log.Information<string>("Creating crypto checkout for product type {ProductType}", productType);
		CryptoCheckoutRequest request = new CryptoCheckoutRequest
		{
			ProductType = productType,
			UserToken = userToken
		};
		return await PostAsync<CryptoCheckoutResponse>("coinbase/checkout", request, cancellationToken);
	}

	public async Task<TrialCheckResponse> CheckTrialAsync(string deviceId, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateNotNullOrEmpty(deviceId, "deviceId");
		Log.Information<string>("Checking trial status for device {DeviceId}", deviceId);
		TrialCheckRequest request = new TrialCheckRequest
		{
			DeviceId = deviceId
		};
		return await PostAsync<TrialCheckResponse>("trial/check", request, cancellationToken);
	}

	public async Task<TokenRestoreResponse> RestoreTokenAsync(string deviceId, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateNotNullOrEmpty(deviceId, "deviceId");
		Log.Information<string>("Attempting to restore token for device {DeviceId}", deviceId);
		TokenRestoreRequest request = new TokenRestoreRequest
		{
			DeviceId = deviceId
		};
		return await PostAsync<TokenRestoreResponse>("token/restore", request, cancellationToken);
	}
}
