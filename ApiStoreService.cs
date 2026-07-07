using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services.Interfaces;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public class ApiStoreService : IStoreService
{
	private sealed class PurchaseCheckoutFailedException : Exception
	{
		public PurchaseCheckoutFailedException(string message)
			: base(message)
		{
		}
	}

	public static readonly IDictionary<SubscriptionType, string> PurchaseIds = new Dictionary<SubscriptionType, string>
	{
		{
			SubscriptionType.M1,
			"po_stripe_prod_monthly_v1"
		},
		{
			SubscriptionType.M3,
			"po_stripe_prod_quarterly_v1"
		},
		{
			SubscriptionType.M12,
			"po_stripe_prod_yearly_v1"
		}
	}.ToFrozenDictionary();

	private readonly ApiService _apiService;

	private readonly IDeviceIdService _deviceIdService;

	private readonly Settings _settings;

	private readonly IStripeUrlLauncher _urlLauncher;

	private readonly IAppAnalytics? _analytics;

	private readonly Dictionary<string, (string formattedPrice, DateTime cachedAt)> _priceCache = new Dictionary<string, (string, DateTime)>();

	private readonly TimeSpan _priceTtl = TimeSpan.FromMinutes(30L);

	public static SubscriptionType TryParseSubscriptionType(string purchaseId)
	{
		if (string.IsNullOrEmpty(purchaseId))
		{
			return SubscriptionType.Unknown;
		}
		foreach (KeyValuePair<SubscriptionType, string> purchaseId2 in PurchaseIds)
		{
			if (string.Equals(purchaseId2.Value, purchaseId, StringComparison.OrdinalIgnoreCase))
			{
				return purchaseId2.Key;
			}
		}
		return SubscriptionType.Unknown;
	}

	public ApiStoreService(ApiService apiService, IDeviceIdService deviceIdService, Settings settings, IStripeUrlLauncher urlLauncher, IAppAnalytics? analytics = null)
	{
		_apiService = apiService ?? throw new ArgumentNullException("apiService");
		_deviceIdService = deviceIdService ?? throw new ArgumentNullException("deviceIdService");
		_settings = settings ?? throw new ArgumentNullException("settings");
		_urlLauncher = urlLauncher ?? throw new ArgumentNullException("urlLauncher");
		_analytics = analytics;
	}

	public async Task<SubscriptionStatusResponse> GetSubscriptionStatusAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		Log.Information("ApiStoreService: Checking subscription status...");
		if (string.IsNullOrEmpty(_settings.UserToken) || _settings.UserToken.Contains("Not set"))
		{
			Log.Warning("ApiStoreService: User token not set");
			return new SubscriptionStatusResponse(isActive: false, 0, DateTimeOffset.MinValue);
		}
		try
		{
			string deviceId = await _deviceIdService.GetDeviceId();
			TokenCheckResponse tokenResponse = await _apiService.CheckTokenAsync(_settings.UserToken, deviceId, cancellationToken);
			if (tokenResponse == null || !tokenResponse.Valid || tokenResponse.User == null)
			{
				return new SubscriptionStatusResponse(isActive: false, 0, DateTimeOffset.MinValue);
			}
			int subLevel = tokenResponse.User.SubscriptionStatus;
			DateTime? expiry = tokenResponse.User.SubscriptionExpiryDay;
			if (subLevel > 0 && expiry.HasValue && expiry.Value > DateTimeOffset.UtcNow)
			{
				return new SubscriptionStatusResponse(isActive: true, 2, expiry.Value);
			}
			return new SubscriptionStatusResponse(isActive: false, 0, DateTimeOffset.MinValue);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "ApiStoreService: Failed to check subscription status");
			return new SubscriptionStatusResponse(isActive: false, 0, DateTimeOffset.MinValue);
		}
	}

	public async Task<string?> GetItemPrice(SubscriptionType subType, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!PurchaseIds.TryGetValue(subType, out string purchaseId))
		{
			return null;
		}
		if (_priceCache.TryGetValue(purchaseId, out var cached) && DateTime.UtcNow - cached.cachedAt < _priceTtl)
		{
			return cached.formattedPrice;
		}
		try
		{
			PurchasePricesResponse pricesResponse = await _apiService.GetPurchasePricesAsync(new string[1] { purchaseId }, cancellationToken);
			if (pricesResponse?.Purchases == null || !pricesResponse.Success)
			{
				Log.Warning<string, bool?>("ApiStoreService: Failed to fetch price for {PurchaseId}. Success={Success}", purchaseId, pricesResponse?.Success);
				return null;
			}
			PurchaseDto purchase = pricesResponse.Purchases.FirstOrDefault((PurchaseDto p) => p.Id == purchaseId);
			if (purchase == null)
			{
				Log.Warning<string>("ApiStoreService: Purchase id {PurchaseId} not found in response", purchaseId);
				return null;
			}
			string formatted = FormatPrice(purchase.AmountCents, purchase.Currency);
			_priceCache[purchaseId] = (formatted, DateTime.UtcNow);
			return formatted;
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "ApiStoreService: Error getting price for {PurchaseId}", purchaseId);
			return null;
		}
	}

	public async Task<string> PurchaseItemAsync(SubscriptionType subType, CancellationToken cancellationToken = default(CancellationToken))
	{
		string transactionId = Guid.NewGuid().ToString();
		if (string.IsNullOrEmpty(_settings.UserToken))
		{
			Log.Error("ApiStoreService: Cannot purchase, user token missing");
			return transactionId;
		}
		if (!PurchaseIds.TryGetValue(subType, out string purchaseId))
		{
			Log.Error<SubscriptionType>("ApiStoreService: Purchase id mapping missing for {SubType}", subType);
			return transactionId;
		}
		try
		{
			string deviceId = await _deviceIdService.GetDeviceId();
			string locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
			Log.Information<string, string>("ApiStoreService: Creating checkout for {PurchaseId} (device {DeviceId})", purchaseId, deviceId);
			PurchaseCheckoutResponse response = await _apiService.CreatePurchaseCheckoutAsync(_settings.UserToken, deviceId, purchaseId, locale, transactionId, cancellationToken);
			if (response.Success && !string.IsNullOrEmpty(response.CheckoutUrl))
			{
				_settings.StripeCustomerId = response.CustomerId;
				await _urlLauncher.LaunchStripeUrlAsync(response.CheckoutUrl, cancellationToken);
			}
			else
			{
				string errorText = (string.IsNullOrEmpty(response.Error) ? "(no error message)" : response.Error);
				Log.Error<string, string>("ApiStoreService: Checkout failed for {PurchaseId}. Error={Error}", purchaseId, errorText);
				_analytics?.SendException(new PurchaseCheckoutFailedException("Checkout failed for " + purchaseId + ": " + errorText), "ApiStoreService.PurchaseItemAsync");
			}
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			Log.Information<SubscriptionType>("ApiStoreService: Purchase cancelled for {SubType}", subType);
		}
		catch (Exception ex2)
		{
			Log.Error<SubscriptionType>(ex2, "ApiStoreService: Purchase failed for {SubType}", subType);
			_analytics?.SendException(ex2, "ApiStoreService.PurchaseItemAsync");
		}
		return transactionId;
	}

	private string FormatPrice(int amountCents, string? currency)
	{
		decimal value = (decimal)amountCents / 100m;
		string text = (currency ?? "usd").ToUpperInvariant();
		if (1 == 0)
		{
		}
		string result;
		if (!(text == "USD"))
		{
			if (text == "EUR")
			{
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider = invariantCulture;
				DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(1, 1, invariantCulture);
				handler.AppendLiteral("€");
				handler.AppendFormatted(value, "0.00");
				result = string.Create(provider, ref handler);
			}
			else
			{
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider2 = invariantCulture;
				DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(1, 2, invariantCulture);
				handler2.AppendFormatted(value, "0.00");
				handler2.AppendLiteral(" ");
				handler2.AppendFormatted(text);
				result = string.Create(provider2, ref handler2);
			}
		}
		else
		{
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider3 = invariantCulture;
			DefaultInterpolatedStringHandler handler3 = new DefaultInterpolatedStringHandler(1, 1, invariantCulture);
			handler3.AppendLiteral("$");
			handler3.AppendFormatted(value, "0.00");
			result = string.Create(provider3, ref handler3);
		}
		if (1 == 0)
		{
		}
		return result;
	}
}
