using System;

namespace Ciphra.VPN.Common.Models;

public class SubscriptionStatusResponse
{
	public bool IsActive { get; }

	public int SubscriptionLevel { get; }

	public DateTimeOffset ExpiryDate { get; }

	public string? PurchaseToken { get; set; }

	public SubscriptionStatusResponse(bool isActive, int subscriptionLevel, DateTimeOffset expiryDate, string? purchaseToken)
	{
		IsActive = isActive;
		SubscriptionLevel = subscriptionLevel;
		ExpiryDate = expiryDate;
		PurchaseToken = purchaseToken;
	}

	public SubscriptionStatusResponse(bool isActive, int subscriptionLevel, DateTimeOffset expiryDate)
	{
		IsActive = isActive;
		SubscriptionLevel = subscriptionLevel;
		ExpiryDate = expiryDate;
	}
}
