using System.Net.Sockets;

namespace VpnHood.Core.Proxies;

public class ProxyClientException : SocketException
{
	public ProxyClientException(SocketError socketError, string? message = null)
		: base((int)socketError, message)
	{
	}
}
