using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Sockets;

namespace VpnHood.Core.Tunneling.Sockets;

public class SocketFactory(bool? keepAlive = null, bool? noDelay = null) : ISocketFactory
{
	private bool _hasKeepAliveError;

	public virtual TcpClient CreateTcpClient(IPEndPoint ipEndPoint)
	{
		IPEndPoint localEP = new IPEndPoint(ipEndPoint.IsV4() ? IPAddress.Any : IPAddress.IPv6Any, 0);
		TcpClient tcpClient = new TcpClient(ipEndPoint.AddressFamily);
		tcpClient.Client.Bind(localEP);
		if (keepAlive.HasValue)
		{
			SetKeepAlive(tcpClient.Client, keepAlive.Value);
		}
		if (noDelay.HasValue)
		{
			tcpClient.NoDelay = noDelay.Value;
		}
		return tcpClient;
	}

	public virtual UdpClient CreateUdpClient(AddressFamily addressFamily)
	{
		IPEndPoint localEP = new IPEndPoint(addressFamily.IsV4() ? IPAddress.Any : IPAddress.IPv6Any, 0);
		UdpClient udpClient = new UdpClient(addressFamily);
		udpClient.Client.Bind(localEP);
		return udpClient;
	}

	public virtual void SetKeepAlive(Socket socket, bool enable)
	{
		if (_hasKeepAliveError)
		{
			return;
		}
		try
		{
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, enable);
		}
		catch (Exception exception)
		{
			_hasKeepAliveError = true;
			VhLogger.Instance.LogWarning(exception, "KeepAlive is not supported! Consider upgrading your OS.");
		}
	}
}
