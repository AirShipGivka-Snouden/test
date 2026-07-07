using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services.Interfaces;
using Microsoft.UI.Xaml;
using Serilog;
using WinRT.Interop;
using Windows.Services.Store;

namespace Ciphra.VPN.WinUI.Services;

public class WindowsStoreService : IStoreService
{
	private StoreContext _storeContext;

	private bool _isInitialized;

	private readonly IDictionary<SubscriptionType, string> _subscriptionIds = new Dictionary<SubscriptionType, string>
	{
		{
			SubscriptionType.M1,
			"9MTWZ5X0MRSF"
		},
		{
			SubscriptionType.M3,
			"9N9SPF2SRTS5"
		},
		{
			SubscriptionType.M12,
			"9NFDZ1QKQ3G2"
		}
	}.ToFrozenDictionary();

	public void Initialize(Window window)
	{
		if (!_isInitialized)
		{
			_storeContext = StoreContext.GetDefault();
			nint windowHandle = WindowNative.GetWindowHandle((object)window);
			InitializeWithWindow.Initialize((object)_storeContext, (IntPtr)windowHandle);
			_isInitialized = true;
		}
	}

	public async Task<SubscriptionStatusResponse> GetSubscriptionStatusAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		try
		{
			await Task.Delay(1000, cancellationToken);
			TaskAwaiter<StoreAppLicense> taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter<StoreAppLicense>(_storeContext.GetAppLicenseAsync());
			if (!taskAwaiter.IsCompleted)
			{
				await taskAwaiter;
				TaskAwaiter<StoreAppLicense> taskAwaiter2 = default(TaskAwaiter<StoreAppLicense>);
				taskAwaiter = taskAwaiter2;
			}
			StoreAppLicense result = taskAwaiter.GetResult();
			StoreAppLicense appLicense = result;
			DateTimeOffset? latestExpiryDate = null;
			bool hasActiveLicense = false;
			foreach (KeyValuePair<string, StoreLicense> kvp in appLicense.AddOnLicenses)
			{
				string storeId = kvp.Key;
				StoreLicense license = kvp.Value;
				if (license.IsActive && _subscriptionIds.Values.Contains(storeId))
				{
					hasActiveLicense = true;
					DateTimeOffset expiryDate = ((license.ExpirationDate != default(DateTimeOffset)) ? license.ExpirationDate : DateTimeOffset.Now.AddMonths(1));
					int num;
					if (latestExpiryDate.HasValue)
					{
						DateTimeOffset value = expiryDate;
						DateTimeOffset? dateTimeOffset = latestExpiryDate;
						num = ((value > dateTimeOffset) ? 1 : 0);
					}
					else
					{
						num = 1;
					}
					if (num != 0)
					{
						latestExpiryDate = expiryDate;
					}
				}
			}
			if (hasActiveLicense && latestExpiryDate.HasValue)
			{
				return new SubscriptionStatusResponse(isActive: true, 2, latestExpiryDate.Value);
			}
			TaskAwaiter<StoreProductQueryResult> taskAwaiter3 = WindowsRuntimeSystemExtensions.GetAwaiter<StoreProductQueryResult>(_storeContext.GetUserCollectionAsync((IEnumerable<string>)new string[2] { "Durable", "Subscription" }));
			if (!taskAwaiter3.IsCompleted)
			{
				await taskAwaiter3;
				TaskAwaiter<StoreProductQueryResult> taskAwaiter4 = default(TaskAwaiter<StoreProductQueryResult>);
				taskAwaiter3 = taskAwaiter4;
			}
			StoreProductQueryResult result2 = taskAwaiter3.GetResult();
			StoreProductQueryResult result3 = result2;
			if (result3.ExtendedError != null)
			{
				string errorMessage = GetStoreErrorMessage(result3.ExtendedError);
				Log.Error<string>("GetUserCollectionAsync failed: {ErrorMessage}", errorMessage);
			}
			if (result3.Products == null || result3.Products.Count == 0)
			{
				return new SubscriptionStatusResponse(isActive: false, 0, DateTimeOffset.Now);
			}
			foreach (string productKey in result3.Products.Keys)
			{
				result3.Products.TryGetValue(productKey, out var product);
				if (!(product == (StoreProduct)null))
				{
					string storeId2 = product.StoreId;
					if (_subscriptionIds.Values.Contains(storeId2) && product.IsInUserCollection)
					{
						DateTimeOffset expiryDate2 = DateTimeOffset.Now.AddMonths(1);
						return new SubscriptionStatusResponse(isActive: true, 2, expiryDate2);
					}
					product = null;
				}
			}
			return new SubscriptionStatusResponse(isActive: false, 0, DateTimeOffset.Now);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "GetSubscriptionStatusAsync failed");
			return new SubscriptionStatusResponse(isActive: false, 0, DateTimeOffset.Now);
		}
	}

	public async Task<string?> GetItemPrice(SubscriptionType subType, CancellationToken cancellationToken = default(CancellationToken))
	{
		string itemId = _subscriptionIds[subType];
		try
		{
			await Task.Delay(1000, cancellationToken);
			List<string> queryIds = new List<string> { itemId };
			TaskAwaiter<StoreProductQueryResult> taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter<StoreProductQueryResult>(_storeContext.GetStoreProductsAsync((IEnumerable<string>)new string[2] { "Durable", "Subscription" }, (IEnumerable<string>)queryIds));
			if (!taskAwaiter.IsCompleted)
			{
				await taskAwaiter;
				TaskAwaiter<StoreProductQueryResult> taskAwaiter2 = default(TaskAwaiter<StoreProductQueryResult>);
				taskAwaiter = taskAwaiter2;
			}
			StoreProductQueryResult result = taskAwaiter.GetResult();
			StoreProductQueryResult result2 = result;
			if (result2.ExtendedError != null)
			{
				string errorMessage = GetStoreErrorMessage(result2.ExtendedError);
				Log.Error<string>("GetStoreProductsAsync failed: {ErrorMessage}", errorMessage);
			}
			if (result2.Products == null || result2.Products.Count == 0)
			{
				Log.Error<string>("GetStoreProductsAsync returned no products for ID: {Id}", itemId);
				return null;
			}
			if (result2.Products.TryGetValue(itemId, out var product))
			{
				return product.Price.FormattedPrice;
			}
			return null;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error<string>(ex2, "GetItemPrice failed for itemId {ItemId}", itemId);
			return null;
		}
	}

	public async Task<string> PurchaseItemAsync(SubscriptionType subType, CancellationToken cancellationToken = default(CancellationToken))
	{
		string transactionId = Guid.NewGuid().ToString();
		string itemId = _subscriptionIds[subType];
		try
		{
			Log.Information<string>("Requesting purchase for storeId {StoreId}", itemId);
			TaskAwaiter<StorePurchaseResult> taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter<StorePurchaseResult>(_storeContext.RequestPurchaseAsync(itemId));
			if (!taskAwaiter.IsCompleted)
			{
				await taskAwaiter;
				TaskAwaiter<StorePurchaseResult> taskAwaiter2 = default(TaskAwaiter<StorePurchaseResult>);
				taskAwaiter = taskAwaiter2;
			}
			StorePurchaseResult result = taskAwaiter.GetResult();
			StorePurchaseResult result2 = result;
			Log.Information<string, StorePurchaseStatus>("Purchase result for storeId {StoreId}: {Status}", itemId, result2.Status);
			if ((int)result2.Status == 0)
			{
				return transactionId;
			}
			if (result2.ExtendedError != null)
			{
				string errorMessage = GetStoreErrorMessage(result2.ExtendedError);
				Log.Error<string, string>("Purchase failed for storeId {StoreId}: {ErrorMessage}", itemId, errorMessage);
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error<string>(ex2, "PurchaseItemAsync failed for storeId {StoreId}", itemId);
		}
		return transactionId;
	}

	private string GetStoreErrorMessage(Exception error)
	{
		if (error == null)
		{
			return "Unknown error: No exception provided";
		}
		int hResult = error.HResult;
		if (1 == 0)
		{
		}
		string result = hResult switch
		{
			-2147024891 => "Access denied: App may not be properly associated with Microsoft Store or lacks permissions", 
			-2147023673 => "Operation canceled: User canceled the purchase or operation", 
			-2147023579 => "User not signed in: Please sign in to the Microsoft Store", 
			-2147012889 => "Network error: Unable to connect to Microsoft Store servers", 
			-2147009287 => "Store error: Product not found or unavailable in the Microsoft Store", 
			-2143330048 => "Network connectivity issue", 
			-2143330046 => "Purchase canceled by user", 
			-2143330045 => "Product not found in store", 
			-2143330044 => "Product not available for purchase", 
			-2143330043 => "Server error occurred", 
			-2143330041 => "Store not available or user not signed in", 
			-2143330040 => "License check failed", 
			_ => $"Unknown Store error - HResult: 0x{error.HResult:X8}, Message: {error.Message}", 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
