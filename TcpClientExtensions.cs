using System;
using System.Net;
using System.Net.Sockets;

namespace VpnHood.Core.Toolkit.Net;

public static class TcpClientExtensions
{
	extension(TcpClient tcpClient)
	{
		public IPEndPoint? TryGetLocalEndPoint()
		{
			try
			{
				return tcpClient.Client?.LocalEndPoint as IPEndPoint;
			}
			catch
			{
				return null;
			}
		}

		public IPEndPoint? TryGetRemoteEndPoint()
		{
			try
			{
				return tcpClient.Client?.RemoteEndPoint as IPEndPoint;
			}
			catch
			{
				return null;
			}
		}

		public IPEndPoint GetLocalEndPoint()
		{
			ArgumentNullException.ThrowIfNull(tcpClient.Client, "Client");
			return tcpClient.Client.GetLocalEndPoint();
		}

		public IPEndPoint GetRemoteEndPoint()
		{
			ArgumentNullException.ThrowIfNull(tcpClient.Client, "Client");
			return tcpClient.Client.GetRemoteEndPoint();
		}
	}
}
