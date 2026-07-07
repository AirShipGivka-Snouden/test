using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Tunneling.Proxies;

public interface IPacketProxyCallbacks
{
	void OnConnectionRequested(IpProtocol protocolType, IpEndPointValue remoteEndPoint);

	void OnConnectionEstablished(IpProtocol protocolType, IpEndPointValue localEndPoint, IpEndPointValue remoteEndPoint, bool isNewLocalEndPoint, bool isNewRemoteEndPoint);
}
