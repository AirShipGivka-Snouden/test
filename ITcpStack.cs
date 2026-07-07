using System;
using System.Net;
using VpnHood.Core.Packets;

namespace VpnHood.Core.TcpStack.Abstractions;

public interface ITcpStack : IDisposable
{
	Action<IpPacket>? OnPacketSend { get; set; }

	void ProcessIncoming(IpPacket ipPacket);

	ITcpListener ListenAny();

	ITcpListener Listen(IPEndPoint localEndPoint);

	void DropAllConnections();
}
