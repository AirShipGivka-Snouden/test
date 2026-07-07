using System.Text.Json.Serialization;

namespace VpnHood.Core.Common.Messaging;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SessionErrorCode
{
	Ok,
	AccessError,
	PlanRejected,
	GeneralError,
	SessionClosed,
	SessionSuppressedBy,
	SessionError,
	SessionExpired,
	AccessExpired,
	AccessCodeRejected,
	AccessLocked,
	AccessTrafficOverflow,
	DailyLimitExceeded,
	NoServerAvailable,
	PremiumLocation,
	AdError,
	RewardedAdRejected,
	Maintenance,
	RedirectHost,
	UnsupportedClient,
	UnsupportedServer
}
