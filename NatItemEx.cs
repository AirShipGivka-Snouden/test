using System;
using System.Net;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Tunneling;

public class NatItemEx : NatItem
{
	public IPAddress DestinationAddress { get; }

	public ushort DestinationPort { get; }

	public NatItemEx(IpPacket ipPacket)
		: base(ipPacket)
	{
		if (ipPacket == null)
		{
			throw new ArgumentNullException("ipPacket");
		}
		DestinationAddress = ipPacket.DestinationAddress;
		switch (ipPacket.Protocol)
		{
		case IpProtocol.Tcp:
		{
			TcpPacket tcpPacket = ipPacket.ExtractTcp();
			DestinationPort = tcpPacket.DestinationPort;
			break;
		}
		case IpProtocol.Udp:
		{
			UdpPacket udpPacket = ipPacket.ExtractUdp();
			DestinationPort = udpPacket.DestinationPort;
			break;
		}
		default:
			throw new NotSupportedException($"{ipPacket.Protocol} is not yet supported by this NAT!");
		}
	}

	public override bool Equals(object? obj)
	{
		if (obj is NatItemEx natItemEx && base.Equals((object?)natItemEx) && object.Equals(DestinationAddress, natItemEx.DestinationAddress))
		{
			return object.Equals(DestinationPort, natItemEx.DestinationPort);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(base.GetHashCode(), DestinationAddress, DestinationPort);
	}

	public override string ToString()
	{
		return $"{base.Protocol}:{base.NatId}, LocalEp: {VhLogger.Format(base.SourceAddress)}:{base.SourcePort}, RemoteEp: {VhLogger.Format(DestinationAddress)}:{DestinationPort}";
	}
}
