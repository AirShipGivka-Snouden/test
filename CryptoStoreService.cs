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

public class CryptoStoreService : IStoreService
{
	public static readonly IDictionary<SubscriptionType, string> ProductTypes = new Dictionary<SubscriptionType, string>
	{
		{
			SubscriptionType.M1,
			"1month"
		},
		{
			SubscriptionType.M3,
			"3months"
		},
		{
			SubscriptionType.M12,
			"1year"
		}
	}.ToFrozenDictionary();

	private readonly ApiService _apiService;

	private readonly IDeviceIdService _deviceIdService;

	private readonly Settings _settings;

	private readonly IStripeUrlLauncher _urlLauncher;

	private readonly Dictionary<string, (string formattedPrice, DateTime cachedAt)> _priceCache = new Dictionary<string, (string, DateTime)>();

	private readonly TimeSpan _priceTtl = TimeSpan.FromMinutes(30L);

	public static SubscriptionType TryParseSubscriptionType(string productType)
	{
		if (string.IsNullOrEmpty(productType))
		{
			return SubscriptionType.Unknown;
		}
		foreach (KeyValuePair<SubscriptionType, string> productType2 in ProductTypes)
		{
			if (string.Equals(productType2.Value, productType, StringComparison.OrdinalIgnoreCase))
			{
				return productType2.Key;
			}
		}
		return SubscriptionType.Unknown;
	}

	public CryptoStoreService(ApiService apiService, IDeviceIdService deviceIdService, Settings settings, IStripeUrlLauncher urlLauncher)
	{
		_apiService = apiService ?? throw new ArgumentNullException("apiService");
		_deviceIdService = deviceIdService ?? throw new ArgumentNullException("deviceIdService");
		_settings = settings ?? throw new ArgumentNullException("settings");
		_urlLauncher = urlLauncher ?? throw new ArgumentNullException("urlLauncher");
	}

	public async Task<SubscriptionStatusResponse> GetSubscriptionStatusAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		Log.Information("CryptoStoreService: Checking subscription status...");
		if (string.IsNullOrEmpty(_settings.UserToken))
		{
			Log.Warning("CryptoStoreService: User token not set");
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
			Log.Error(ex, "CryptoStoreService: Failed to check subscription status");
			return new SubscriptionStatusResponse(isActive: false, 0, DateTimeOffset.MinValue);
		}
	}

	public async Task<string?> GetItemPrice(SubscriptionType subType, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!ProductTypes.TryGetValue(subType, out string productType))
		{
			return null;
		}
		if (_priceCache.TryGetValue(productType, out var cached) && DateTime.UtcNow - cached.cachedAt < _priceTtl)
		{
			return cached.formattedPrice;
		}
		try
		{
			PurchasePricesResponse pricesResponse = await _apiService.GetPurchasePricesAsync(new string[1] { ApiStoreService.PurchaseIds[subType] }, cancellationToken);
			if (pricesResponse?.Purchases == null || !pricesResponse.Success)
			{
				Log.Warning<string, bool?>("CryptoStoreService: Failed to fetch price for {ProductType}. Success={Success}", productType, pricesResponse?.Success);
				return null;
			}
			PurchaseDto purchase = pricesResponse.Purchases.FirstOrDefault((PurchaseDto p) => p.Id == ApiStoreService.PurchaseIds[subType]);
			if (purchase == null)
			{
				Log.Warning<string>("CryptoStoreService: Product type {ProductType} not found in response", productType);
				return null;
			}
			string formatted = FormatPrice(purchase.AmountCents, purchase.Currency);
			_priceCache[productType] = (formatted, DateTime.UtcNow);
			return formatted;
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "CryptoStoreService: Error getting price for {ProductType}", productType);
			return null;
		}
	}

	public async Task<string> PurchaseItemAsync(SubscriptionType subType, CancellationToken cancellationToken = default(CancellationToken))
	{
		string transactionId = Guid.NewGuid().ToString();
		if (string.IsNullOrEmpty(_settings.UserToken))
		{
			Log.Error("CryptoStoreService: Cannot purchase, user token missing");
			return transactionId;
		}
		if (!ProductTypes.TryGetValue(subType, out string productType))
		{
			Log.Error<SubscriptionType>("CryptoStoreService: Product type mapping missing for {SubType}", subType);
			return transactionId;
		}
		try
		{
			Log.Information<string>("CryptoStoreService: Creating crypto checkout for {ProductType}", productType);
			CryptoCheckoutResponse response = await _apiService.CreateCryptoCheckoutAsync(productType, _settings.UserToken, cancellationToken);
			if (response.Success && !string.IsNullOrEmpty(response.HostedUrl))
			{
				Log.Information<string, string>("CryptoStoreService: Launching crypto checkout URL. ChargeId={ChargeId}, TransactionCode={TransactionCode}", response.ChargeId, response.TransactionCode);
				await _urlLauncher.LaunchStripeUrlAsync(response.HostedUrl, cancellationToken);
			}
			else
			{
				Log.Error<string, string>("CryptoStoreService: Checkout failed for {ProductType}. Error={Error}", productType, response.Error);
			}
		}
		catch (Exception ex)
		{
			Log.Error<SubscriptionType>(ex, "CryptoStoreService: Purchase failed for {SubType}", subType);
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
