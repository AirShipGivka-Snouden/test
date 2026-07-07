using System.Net;
using VpnHood.Core.Toolkit.Logging;

namespace VpnHood.Core.Toolkit.Net;

public readonly record struct IPEndPointPair(IPEndPoint LocalEndPoint, IPEndPoint RemoteEndPoint)
{
	public override string ToString()
	{
		return VhLogger.Format(LocalEndPoint) + "->" + VhLogger.Format(RemoteEndPoint);
	}
}
