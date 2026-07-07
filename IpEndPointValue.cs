using System.Net;

namespace VpnHood.Core.Toolkit.Net;

public readonly record struct IpEndPointValue(IPAddress Address, int Port)
{
	public IPEndPoint ToIPEndPoint()
	{
		return new IPEndPoint(Address, Port);
	}
}
