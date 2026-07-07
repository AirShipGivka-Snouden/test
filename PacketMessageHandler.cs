using System;
using System.IO;
using System.Net;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Streams;

namespace VpnHood.Core.Tunneling.DatagramMessaging;

public static class PacketMessageHandler
{
	private static readonly IPEndPoint NoneEndPoint = new IPEndPoint(IPAddress.None, 0);

	private static PacketMessageCode GetMessageCode(IPacketMessage request)
	{
		if (request is ClosePacketMessage)
		{
			return PacketMessageCode.ClosePacketChannel;
		}
		throw new ArgumentException("Could not detect version code for this datagram message.");
	}

	public static IpPacket CreateMessage(IPacketMessage request)
	{
		using MemoryStream memoryStream = new MemoryStream();
		memoryStream.WriteByte(1);
		memoryStream.WriteByte((byte)GetMessageCode(request));
		StreamUtils.WriteObject(memoryStream, request);
		return PacketBuilder.BuildUdp(NoneEndPoint, NoneEndPoint, memoryStream.ToArray());
	}

	public static bool IsPacketMessage(IpPacket ipPacket)
	{
		if (ipPacket.Protocol == IpProtocol.Udp)
		{
			if (!IPAddress.None.SpanEquals(ipPacket.DestinationAddressSpan))
			{
				return IPAddress.Any.SpanEquals(ipPacket.DestinationAddressSpan);
			}
			return true;
		}
		return false;
	}

	public static IPacketMessage? ReadMessage(IpPacket ipPacket)
	{
		if (!IsPacketMessage(ipPacket))
		{
			return null;
		}
		UdpPacket udpPacket = ipPacket.ExtractUdp();
		Memory<byte> payload = udpPacket.Payload;
		if (payload.Length < 2)
		{
			throw new InvalidDataException("The packet message is too short to read version and message code.");
		}
		payload = udpPacket.Payload;
		byte b = payload.Span[0];
		if (b != 1)
		{
			throw new NotSupportedException($"The packet message version is not supported. Version: {b}. Packet: {ipPacket}");
		}
		payload = udpPacket.Payload;
		PacketMessageCode packetMessageCode = (PacketMessageCode)payload.Span[1];
		payload = udpPacket.Payload;
		using MemoryStream stream = new MemoryStream(payload.Slice(2).ToArray());
		if (packetMessageCode == PacketMessageCode.ClosePacketChannel)
		{
			return StreamUtils.ReadObject<ClosePacketMessage>(stream);
		}
		throw new NotSupportedException($"Unknown Datagram Message messageCode. MessageCode: {packetMessageCode}");
	}
}
