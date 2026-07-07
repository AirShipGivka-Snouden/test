using System.Net;

namespace VpnHood.Core.Tunneling.Messaging;

public class UdpPacketRequest : RequestBase
{
	public required byte[][] PacketBuffers { get; init; }

	public required IPAddress DestinationAddress { get; init; }

	public UdpPacketRequest()
		: base(VpnHood.Core.Tunneling.Messaging.RequestCode.TcpPacketChannel)
	{
	}
}
