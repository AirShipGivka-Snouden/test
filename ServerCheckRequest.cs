using VpnHood.Core.Common.Messaging;

namespace VpnHood.Core.Tunneling.Messaging;

public class ServerCheckRequest : ClientRequest
{
	public ServerCheckRequest()
		: base(30)
	{
	}
}
