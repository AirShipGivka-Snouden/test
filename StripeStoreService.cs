using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services.Interfaces;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public class StripeStoreService : IStoreService
{
	public const string CheckoutSuccessParameter = "payment/success";

	public const string CheckoutCancelParameter = "payment/cancel";

	private const string successUrlFormat = "https://ciphra.vpn/payment/success?session_id={0}";

	private const string cancelUrlFormat = "https://ciphra.vpn/payment/cancel?session_id={0}&reason=user_aborted";

	private readonly ApiService _apiService;

	private readonly HttpClient _httpClient;

	private readonly IStripeUrlLauncher _stripeUrlLauncher;

	private readonly IDeviceIdService _deviceIdService;

	private readonly Settings _settings;

	private IDictionary<SubscriptionType, string> _itemPrices = new Dictionary<SubscriptionType, string>
	{
		{
			SubscriptionType.M1,
			"$4.99"
		},
		{
			SubscriptionType.M3,
			"$11.99"
		},
		{
			SubscriptionType.M12,
			"$39.99"
		}
	}.ToFrozenDictionary();

	private IDictionary<SubscriptionType, string> _itemPriceIds = new Dictionary<SubscriptionType, string>
	{
		{
			SubscriptionType.M1,
			"price_1RwycX2OmK7MSwaDNkDsa9K6"
		},
		{
			SubscriptionType.M3,
			"price_1RxhyR2OmK7MSwaDR3iVZoB7"
		},
		{
			SubscriptionType.M12,
			"price_1Rxi0k2OmK7MSwaDR4uB76Zm"
		}
	}.ToFrozenDictionary();

	public StripeStoreService(ApiService apiService, HttpClient httpClient, IStripeUrlLauncher stripeUrlLauncher, IDeviceIdService deviceIdService, Settings settings)
	{
		_apiService = apiService ?? throw new ArgumentNullException("apiService");
		_httpClient = httpClient ?? throw new ArgumentNullException("httpClient");
		_stripeUrlLauncher = stripeUrlLauncher ?? throw new ArgumentNullException("stripeUrlLauncher");
		_deviceIdService = deviceIdService ?? throw new ArgumentNullException("deviceIdService");
		_settings = settings ?? throw new ArgumentNullException("settings");
	}

	private DateTime GetDefaultSubscriptionDateTimeOffset()
	{
		return DateTime.UtcNow.AddMonths(1);
	}

	public async Task<SubscriptionStatusResponse> GetSubscriptionStatusAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		Log.Information("Checking subscription status...");
		if (string.IsNullOrEmpty(_settings.UserToken))
		{
			Log.Warning("GetSubscriptionStatusAsync: User token is not set.");
			return new SubscriptionStatusResponse(isActive: false, 0, DateTimeOffset.MinValue);
		}
		TokenCheckResponse tokenResponse = null;
		try
		{
			string deviceId = await _deviceIdService.GetDeviceId();
			tokenResponse = await _apiService.CheckTokenAsync(_settings.UserToken, deviceId, cancellationToken);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "GetSubscriptionStatusAsync: Error checking token.");
		}
		if (tokenResponse == null || !tokenResponse.Valid || tokenResponse.User == null)
		{
			Log.Warning("GetSubscriptionStatusAsync: Token is invalid or user data is missing.");
			return new SubscriptionStatusResponse(isActive: false, 0, DateTimeOffset.MinValue);
		}
		int subscriptionStatus = tokenResponse.User.SubscriptionStatus;
		DateTime? expiryDate = tokenResponse.User.SubscriptionExpiryDay;
		if (subscriptionStatus > 0 && expiryDate > DateTimeOffset.UtcNow)
		{
			Log.Information<int, DateTime?>("Subscription is active. Status: {Status}, Expiry: {Expiry}", subscriptionStatus, expiryDate);
			return new SubscriptionStatusResponse(isActive: true, 2, expiryDate.GetValueOrDefault(GetDefaultSubscriptionDateTimeOffset()));
		}
		Log.Information<int, DateTime?>("Subscription is inactive or expired. Status: {Status}, Expiry: {Expiry}", subscriptionStatus, expiryDate);
		return new SubscriptionStatusResponse(isActive: false, 0, DateTimeOffset.MinValue);
	}

	public Task<string?> GetItemPrice(SubscriptionType subType, CancellationToken cancellationToken = default(CancellationToken))
	{
		string value;
		return _itemPrices.TryGetValue(subType, out value) ? Task.FromResult(value) : Task.FromResult<string>(null);
	}

	public async Task<string> PurchaseItemAsync(SubscriptionType subType, CancellationToken cancellationToken = default(CancellationToken))
	{
		string checkoutSessionId = Guid.NewGuid().ToString();
		Log.Information<SubscriptionType, string>("PurchaseItemAsync: Starting purchase for subscription type {SubscriptionType} with session ID {SessionId}", subType, checkoutSessionId);
		string successUrl = $"https://ciphra.vpn/payment/success?session_id={checkoutSessionId}";
		string cancelUrl = $"https://ciphra.vpn/payment/cancel?session_id={checkoutSessionId}&reason=user_aborted";
		string deviceId = await _deviceIdService.GetDeviceId();
		string id;
		string priceId = (_itemPriceIds.TryGetValue(subType, out id) ? id : null);
		if (string.IsNullOrEmpty(priceId))
		{
			Log.Error<SubscriptionType>("PurchaseItemAsync: Price ID for subscription type {SubscriptionType} is not defined.", subType);
			return checkoutSessionId;
		}
		string userToken = _settings.UserToken;
		if (string.IsNullOrEmpty(userToken))
		{
			Log.Error("PurchaseItemAsync: User token is not set.");
			return checkoutSessionId;
		}
		StripeCheckoutResponse response = await _apiService.CreateStripeCheckoutAsync(userToken, deviceId, priceId, successUrl, cancelUrl, cancellationToken);
		Log.Information("PurchaseItemAsync: Received response from CreateStripeCheckoutAsync. Success: {Success}, CheckoutUrl: {CheckoutUrl}, CustomerId: {CustomerId}, Error: {Error}", new object[4] { response.Success, response.CheckoutUrl, response.CustomerId, response.Error });
		if (response.Success && !string.IsNullOrEmpty(response.CheckoutUrl))
		{
			_settings.StripeCustomerId = response.CustomerId;
			await _stripeUrlLauncher.LaunchStripeUrlAsync(response.CheckoutUrl, cancellationToken);
		}
		else
		{
			Log.Error<string>("PurchaseItemAsync: Failed to create Stripe checkout session. Error: {Error}", response.Error);
		}
		return checkoutSessionId;
	}
}
