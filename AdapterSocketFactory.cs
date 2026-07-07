using System;
using System.Net;
using System.Net.Sockets;
using VpnHood.Core.Toolkit.Sockets;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.VpnAdapters.Abstractions;

namespace VpnHood.Core.Client;

public class AdapterSocketFactory(IVpnAdapter vpnAdapter, ISocketFactory socketFactory) : ISocketFactory
{
	public TcpClient CreateTcpClient(IPEndPoint ipEndPoint)
	{
		TcpClient tcpClient = new TcpClient(ipEndPoint.AddressFamily);
		if (vpnAdapter.CanProtectSocket)
		{
			vpnAdapter.ProtectSocket(tcpClient.Client, ipEndPoint.Address);
		}
		socketFactory.SetKeepAlive(tcpClient.Client, enable: true);
		VhUtils.ConfigTcpClient(tcpClient);
		return tcpClient;
	}

	public UdpClient CreateUdpClient(AddressFamily addressFamily)
	{
		UdpClient udpClient = new UdpClient(addressFamily);
		if (udpClient.Client == null)
		{
			throw new Exception("UdpClient socket is null.");
		}
		if (vpnAdapter.CanProtectSocket)
		{
			vpnAdapter.ProtectSocket(udpClient.Client);
		}
		return udpClient;
	}

	public void SetKeepAlive(Socket socket, bool enable)
	{
		socketFactory.SetKeepAlive(socket, enable);
	}
}
