namespace VpnHood.Core.Tunneling.Messaging;

public class SessionStatusRequest : RequestBase
{
	public SessionStatusRequest()
		: base(VpnHood.Core.Tunneling.Messaging.RequestCode.SessionStatus)
	{
	}
}
