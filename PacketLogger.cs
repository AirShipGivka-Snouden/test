using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Tunneling.Exceptions;

namespace VpnHood.Core.Tunneling.Utils;

public static class PacketLogger
{
	public static void LogPackets(IReadOnlyList<IpPacket> ipPackets, string operation)
	{
		for (int i = 0; i < ipPackets.Count; i++)
		{
			LogPacket(ipPackets[i], operation);
		}
	}

	public static void LogPacket(IpPacket ipPacket, string message, LogLevel logLevel = LogLevel.Trace, Exception? exception = null, EventId? eventId = null)
	{
		try
		{
			if (VhLogger.MinLogLevel <= LogLevel.Trace)
			{
				EventId eventId2 = GeneralEventId.Packet;
				Memory<byte> memory = default(Memory<byte>);
				switch (ipPacket.Protocol)
				{
				case IpProtocol.IcmpV4:
					eventId2 = GeneralEventId.Ping;
					memory = ipPacket.ExtractIcmpV4().Payload;
					break;
				case IpProtocol.IcmpV6:
					eventId2 = GeneralEventId.Ping;
					memory = ipPacket.ExtractIcmpV6().Payload;
					break;
				case IpProtocol.Udp:
					eventId2 = GeneralEventId.Udp;
					memory = ipPacket.ExtractUdp().Payload;
					break;
				case IpProtocol.Tcp:
					eventId2 = GeneralEventId.Tcp;
					memory = ipPacket.ExtractTcp().Payload;
					break;
				}
				if (exception is NetFilterException)
				{
					eventId = GeneralEventId.NetFilter;
				}
				VhLogger.Instance.Log(logLevel, eventId ?? eventId2, exception, message + " Packet: {Packet}, PayloadLength: {PayloadLength}, Payload: {Payload}", Format(ipPacket), memory.Length, BitConverter.ToString(memory.ToArray(), 0, Math.Min(10, memory.Length)));
			}
		}
		catch (Exception exception2)
		{
			VhLogger.Instance.LogError(GeneralEventId.Packet, exception2, "Could not extract packet for log. Packet: {Packet}, Message: {Message}, Exception: {Exception}", Format(ipPacket), message, exception);
		}
	}

	public static string Format(IpPacket ipPacket)
	{
		return VhLogger.FormatIpPacket(ipPacket.ToString());
	}
}
