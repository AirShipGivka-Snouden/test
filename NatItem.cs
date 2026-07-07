using System;
using System.Net;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Tunneling;

public class NatItem
{
	public ushort NatId { get; internal set; }

	public object? CustomData { get; set; }

	public IpVersion IpVersion { get; }

	public IpProtocol Protocol { get; }

	public IPAddress SourceAddress { get; }

	public ushort SourcePort { get; }

	public ushort IcmpId { get; }

	public DateTime AccessTime { get; internal set; }

	public NatItem(IpPacket ipPacket)
	{
		if (ipPacket == null)
		{
			throw new ArgumentNullException("ipPacket");
		}
		IpVersion = ipPacket.Version;
		Protocol = ipPacket.Protocol;
		SourceAddress = ipPacket.SourceAddress;
		AccessTime = FastDateTime.Now;
		switch (ipPacket.Protocol)
		{
		case IpProtocol.Tcp:
		{
			TcpPacket tcpPacket = ipPacket.ExtractTcp();
			SourcePort = tcpPacket.SourcePort;
			break;
		}
		case IpProtocol.Udp:
		{
			UdpPacket udpPacket = ipPacket.ExtractUdp();
			SourcePort = udpPacket.SourcePort;
			break;
		}
		case IpProtocol.IcmpV4:
			IcmpId = GetIcmpV4Id(ipPacket);
			break;
		case IpProtocol.IcmpV6:
			IcmpId = GetIcmpV6Id(ipPacket);
			break;
		default:
			throw new NotSupportedException($"{ipPacket.Protocol} is not yet supported by this NAT!");
		}
	}

	private static ushort GetIcmpV4Id(IpPacket ipPacket)
	{
		IcmpV4Packet icmpV4Packet = ipPacket.ExtractIcmpV4();
		IcmpV4Type type = icmpV4Packet.Type;
		if ((type == IcmpV4Type.EchoReply || type == IcmpV4Type.EchoRequest) ? true : false)
		{
			return icmpV4Packet.Identifier;
		}
		throw new Exception($"Unsupported IcmpV4 Type for NAT. Type: {icmpV4Packet.Type}");
	}

	private static ushort GetIcmpV6Id(IpPacket ipPacket)
	{
		IcmpV6Packet icmpV6Packet = ipPacket.ExtractIcmpV6();
		IcmpV6Type type = icmpV6Packet.Type;
		if (type - 128 <= IcmpV6Type.DestinationUnreachable)
		{
			return icmpV6Packet.Identifier;
		}
		throw new Exception($"Unsupported IcmpV6 Type for NAT. Type: {icmpV6Packet.Type}");
	}

	public override bool Equals(object? obj)
	{
		if (obj is NatItem natItem && object.Equals(IpVersion, natItem.IpVersion) && object.Equals(Protocol, natItem.Protocol) && object.Equals(SourceAddress, natItem.SourceAddress) && object.Equals(SourcePort, natItem.SourcePort))
		{
			return object.Equals(IcmpId, natItem.IcmpId);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(IpVersion, Protocol, SourceAddress, SourcePort, IcmpId);
	}

	public override string ToString()
	{
		return $"{Protocol}:{NatId}, LocalEp: {VhLogger.Format(SourceAddress)}:{SourcePort}";
	}
}
