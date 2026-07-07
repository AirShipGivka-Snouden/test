using System;
using System.Net;
using System.Net.Sockets;
using VpnHood.Core.Toolkit.Collections;

namespace VpnHood.Core.Tunneling.Proxies;

internal class UdpProxyEx(UdpClient udpClient, TimeSpan udpTimeout, int queueCapacity, bool autoDisposePackets) : UdpProxy(udpClient, null, queueCapacity, autoDisposePackets)
{
	public TimeoutDictionary<IPEndPoint, TimeoutItem<IPEndPoint>> DestinationEndPointMap { get; } = new TimeoutDictionary<IPEndPoint, TimeoutItem<IPEndPoint>>(udpTimeout);

	protected override IPEndPoint? GetSourceEndPoint(IPEndPoint remoteEndPoint)
	{
		if (!DestinationEndPointMap.TryGetValue(remoteEndPoint, out TimeoutItem<IPEndPoint> value))
		{
			return null;
		}
		return value.Value;
	}

	protected override void DisposeManaged()
	{
		DestinationEndPointMap.Dispose();
		base.DisposeManaged();
	}
}
