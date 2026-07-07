using System;
using System.Net;
using System.Net.Sockets;

namespace VpnHood.Core.Toolkit.Net;

public static class SocketExtensions
{
	extension(Socket socket)
	{
		public IPEndPoint GetLocalEndPoint()
		{
			return (IPEndPoint)(socket.LocalEndPoint ?? throw new InvalidOperationException("Socket does not have a local endpoint."));
		}

		public IPEndPoint GetRemoteEndPoint()
		{
			return (IPEndPoint)(socket.RemoteEndPoint ?? throw new InvalidOperationException("Socket does not have a remote endpoint."));
		}
	}
}
