using System.Net;
using System.Net.Sockets;

namespace VpnHood.Core.Toolkit.Sockets;

public interface ISocketFactory
{
	TcpClient CreateTcpClient(IPEndPoint ipEndPoint);

	UdpClient CreateUdpClient(AddressFamily addressFamily);

	void SetKeepAlive(Socket socket, bool enable);
}
