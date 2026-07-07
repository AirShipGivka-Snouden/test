namespace VpnHood.Core.Tunneling.Messaging;

public class RewardedAdRequest : RequestBase
{
	public required string AdData { get; init; }

	public RewardedAdRequest()
		: base(VpnHood.Core.Tunneling.Messaging.RequestCode.RewardedAd)
	{
	}
}
