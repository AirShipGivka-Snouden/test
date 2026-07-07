using System.Net;

namespace VpnHood.Core.Toolkit.Net;

public static class IPEndPointExtensions
{
	extension(IPEndPoint ipEndPoint)
	{
		public IpEndPointValue ToValue()
		{
			return new IpEndPointValue(ipEndPoint.Address, ipEndPoint.Port);
		}
	}
}
