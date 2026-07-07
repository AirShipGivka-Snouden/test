namespace VpnHood.Core.Tunneling.Messaging;

public class ByeRequest : RequestBase
{
	public ByeRequest()
		: base(VpnHood.Core.Tunneling.Messaging.RequestCode.Bye)
	{
	}
}
