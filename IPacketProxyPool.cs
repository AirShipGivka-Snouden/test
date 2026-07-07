using System;
using VpnHood.Core.PacketTransports;

namespace VpnHood.Core.Tunneling.Proxies;

public interface IPacketProxyPool : IPacketTransport, IDisposable
{
	int ClientCount { get; }

	int RemoteEndPointCount { get; }
}
