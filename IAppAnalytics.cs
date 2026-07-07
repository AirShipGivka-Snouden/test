using System;
using System.Threading.Tasks;
using Ciphra.VPN.Common.Models;

namespace Ciphra.VPN.Common.App;

public interface IAppAnalytics
{
	Task IdentifyAsync();

	void SendView(string viewName);

	void SendEvent(string eventName, params (string Key, string Value)[] properties);

	void SendException(Exception exception, string? source = null, bool fatal = false);

	void TrackSubscriptionPurchaseIntent(SubscriptionType subType, string transactionId);

	void TrackSubscriptionPurchaseResult(SubscriptionType subType, string transactionId, bool success);

	void TrackPaymentMethodSelected(SubscriptionType subType, string paymentMethod);

	void SendRelayFailureEvent(RelayFailureEntry entry);

	Task FlushAsync();
}
