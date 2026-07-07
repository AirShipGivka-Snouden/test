using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Packets;

public static class PacketBuilder
{
	public static IpPacket Parse(ReadOnlySpan<byte> buffer, MemoryPool<byte>? memoryPool = null)
	{
		buffer = buffer[..PacketUtil.ReadPacketLength(buffer)];
		if (memoryPool == null)
		{
			memoryPool = MemoryPool<byte>.Shared;
		}
		IMemoryOwner<byte> memoryOwner = memoryPool.Rent(buffer.Length);
		buffer.CopyTo(memoryOwner.Memory.Span);
		return Attach(memoryOwner);
	}

	public static IpPacket Attach(Memory<byte> buffer)
	{
		IpVersion packetVersion = IpPacket.GetPacketVersion(buffer.Span);
		return packetVersion switch
		{
			IpVersion.IPv4 => new IpV4Packet(buffer), 
			IpVersion.IPv6 => new IpV6Packet(buffer), 
			_ => throw new NotSupportedException($"IP version {packetVersion} not supported."), 
		};
	}

	public static IpPacket Attach(IMemoryOwner<byte> memoryOwner)
	{
		IpVersion packetVersion = IpPacket.GetPacketVersion(memoryOwner.Memory.Span);
		return packetVersion switch
		{
			IpVersion.IPv4 => new IpV4Packet(memoryOwner), 
			IpVersion.IPv6 => new IpV6Packet(memoryOwner), 
			_ => throw new NotSupportedException($"IP version {packetVersion} not supported."), 
		};
	}

	public static IpPacket BuildIp(IPAddress sourceAddress, IPAddress destinationAddress, IpProtocol protocol, int payloadLength, MemoryPool<byte>? memoryPool = null)
	{
		if (sourceAddress.AddressFamily != destinationAddress.AddressFamily)
		{
			throw new ArgumentException("SourceAddress and DestinationAddress must have a same ip version.");
		}
		if (memoryPool == null)
		{
			memoryPool = MemoryPool<byte>.Shared;
		}
		object obj = sourceAddress.AddressFamily switch
		{
			AddressFamily.InterNetwork => new IpV4Packet(memoryPool.Rent(20 + payloadLength), 20 + payloadLength, protocol, 0), 
			AddressFamily.InterNetworkV6 => new IpV6Packet(memoryPool.Rent(40 + payloadLength), 40 + payloadLength, protocol), 
			_ => throw new NotSupportedException($"{sourceAddress.AddressFamily} not supported."), 
		};
		((IpPacket)obj).SourceAddress = sourceAddress;
		((IpPacket)obj).DestinationAddress = destinationAddress;
		((IpPacket)obj).TimeToLive = 64;
		return (IpPacket)obj;
	}

	public static IpPacket BuildIp(ReadOnlySpan<byte> sourceAddress, ReadOnlySpan<byte> destinationAddress, IpProtocol protocol, int payloadLength, MemoryPool<byte>? memoryPool = null)
	{
		if (sourceAddress.Length != destinationAddress.Length)
		{
			throw new ArgumentException("SourceAddress and DestinationAddress must have a same ip version.");
		}
		if (memoryPool == null)
		{
			memoryPool = MemoryPool<byte>.Shared;
		}
		object obj = sourceAddress.Length switch
		{
			4 => new IpV4Packet(memoryPool.Rent(20 + payloadLength), 20 + payloadLength, protocol, 0), 
			16 => new IpV6Packet(memoryPool.Rent(40 + payloadLength), 40 + payloadLength, protocol), 
			_ => throw new NotSupportedException($"IP version {sourceAddress.Length} not supported."), 
		};
		((IpPacket)obj).SourceAddressSpan = sourceAddress;
		((IpPacket)obj).DestinationAddressSpan = destinationAddress;
		((IpPacket)obj).TimeToLive = 64;
		return (IpPacket)obj;
	}

	public static IpPacket BuildUdp(IPEndPoint sourceEndPoint, IPEndPoint destinationEndPoint, ReadOnlySpan<byte> payload)
	{
		return BuildUdp(sourceEndPoint.Address, destinationEndPoint.Address, sourceEndPoint.Port, destinationEndPoint.Port, payload);
	}

	public static IpPacket BuildUdp(IPAddress sourceAddress, IPAddress destinationAddress, int sourcePort, int destinationPort, ReadOnlySpan<byte> payload)
	{
		IpPacket ipPacket = BuildIp(sourceAddress, destinationAddress, IpProtocol.Udp, 8 + payload.Length);
		UdpPacket udpPacket = ipPacket.BuildUdp();
		udpPacket.SourcePort = (ushort)sourcePort;
		udpPacket.DestinationPort = (ushort)destinationPort;
		payload.CopyTo(udpPacket.Payload.Span);
		return ipPacket;
	}

	public static IpPacket BuildTcp(IpEndPointValue sourceEndPoint, IpEndPointValue destinationEndPoint, ReadOnlySpan<byte> options, ReadOnlySpan<byte> payload)
	{
		return BuildTcp(sourceEndPoint.Address, destinationEndPoint.Address, sourceEndPoint.Port, destinationEndPoint.Port, options, payload);
	}

	public static IpPacket BuildTcp(IPEndPoint sourceEndPoint, IPEndPoint destinationEndPoint, ReadOnlySpan<byte> options, ReadOnlySpan<byte> payload)
	{
		return BuildTcp(sourceEndPoint.Address, destinationEndPoint.Address, sourceEndPoint.Port, destinationEndPoint.Port, options, payload);
	}

	public static IpPacket BuildTcp(IPAddress sourceAddress, IPAddress destinationAddress, int sourcePort, int destinationPort, ReadOnlySpan<byte> options, ReadOnlySpan<byte> payload)
	{
		IpPacket ipPacket = BuildIp(sourceAddress, destinationAddress, IpProtocol.Tcp, 20 + options.Length + payload.Length);
		TcpPacket tcpPacket = ipPacket.BuildTcp(options.Length);
		tcpPacket.SourcePort = (ushort)sourcePort;
		tcpPacket.DestinationPort = (ushort)destinationPort;
		payload.CopyTo(tcpPacket.Payload.Span);
		options.CopyTo(tcpPacket.Options.Span);
		return ipPacket;
	}

	public static IpPacket BuildIcmpV4(IPAddress sourceAddress, IPAddress destinationAddress, ReadOnlySpan<byte> payload)
	{
		IpPacket ipPacket = BuildIp(sourceAddress, destinationAddress, IpProtocol.IcmpV4, 8 + payload.Length);
		payload.CopyTo(ipPacket.BuildIcmpV4().Payload.Span);
		return ipPacket;
	}

	public static IpPacket BuildIcmpV6(IPAddress sourceAddress, IPAddress destinationAddress, ReadOnlySpan<byte> payload)
	{
		IpPacket ipPacket = BuildIp(sourceAddress, destinationAddress, IpProtocol.IcmpV6, 8 + payload.Length);
		payload.CopyTo(ipPacket.BuildIcmpV6().Payload.Span);
		return ipPacket;
	}

	public static IpPacket BuildIcmpEchoRequest(IPAddress sourceAddress, IPAddress destinationAddress, ReadOnlySpan<byte> payload, ushort identifier = 0, ushort sequenceNumber = 0, bool updateChecksum = true)
	{
		if (sourceAddress.AddressFamily != destinationAddress.AddressFamily)
		{
			throw new ArgumentException("SourceAddress and DestinationAddress must have a same address family.");
		}
		return sourceAddress.AddressFamily switch
		{
			AddressFamily.InterNetwork => BuildIcmpV4EchoRequest(sourceAddress, destinationAddress, payload, identifier, sequenceNumber, updateChecksum), 
			AddressFamily.InterNetworkV6 => BuildIcmpV6EchoRequest(sourceAddress, destinationAddress, payload, identifier, sequenceNumber, updateChecksum), 
			_ => throw new NotSupportedException($"{sourceAddress.AddressFamily} not supported."), 
		};
	}

	public static IpPacket BuildIcmpV4EchoRequest(IPAddress sourceAddress, IPAddress destinationAddress, ReadOnlySpan<byte> payload, ushort identifier = 0, ushort sequenceNumber = 0, bool updateChecksum = true)
	{
		if (!sourceAddress.IsV4() || !destinationAddress.IsV4())
		{
			throw new ArgumentException("SourceAddress and DestinationAddress must be IPv4 addresses.");
		}
		IpPacket ipPacket = BuildIcmpV4(sourceAddress, destinationAddress, payload);
		IcmpV4Packet icmpV4Packet = ipPacket.ExtractIcmpV4();
		icmpV4Packet.Type = IcmpV4Type.EchoRequest;
		icmpV4Packet.Code = 0;
		icmpV4Packet.SequenceNumber = sequenceNumber;
		icmpV4Packet.Identifier = identifier;
		if (updateChecksum)
		{
			ipPacket.UpdateAllChecksums();
		}
		return ipPacket;
	}

	public static IpPacket BuildIcmpV6EchoRequest(IPAddress sourceAddress, IPAddress destinationAddress, ReadOnlySpan<byte> payload, ushort identifier = 0, ushort sequenceNumber = 0, bool updateChecksum = true)
	{
		if (!sourceAddress.IsV6() || !destinationAddress.IsV6())
		{
			throw new ArgumentException("SourceAddress and DestinationAddress must be IPv6 addresses.");
		}
		IpPacket ipPacket = BuildIcmpV6(sourceAddress, destinationAddress, payload);
		IcmpV6Packet icmpV6Packet = ipPacket.ExtractIcmpV6();
		icmpV6Packet.Type = IcmpV6Type.EchoRequest;
		icmpV6Packet.Code = 0;
		icmpV6Packet.SequenceNumber = sequenceNumber;
		icmpV6Packet.Identifier = identifier;
		if (updateChecksum)
		{
			ipPacket.UpdateAllChecksums();
		}
		return ipPacket;
	}

	public static IpPacket BuildIcmpEchoReply(IPAddress sourceAddress, IPAddress destinationAddress, ReadOnlySpan<byte> payload, ushort identifier = 0, ushort sequenceNumber = 0, bool updateChecksum = true)
	{
		if (sourceAddress.AddressFamily != destinationAddress.AddressFamily)
		{
			throw new ArgumentException("SourceAddress and DestinationAddress must have a same ip version.");
		}
		return sourceAddress.AddressFamily switch
		{
			AddressFamily.InterNetwork => BuildIcmpV4EchoReply(sourceAddress, destinationAddress, payload, identifier, sequenceNumber, updateChecksum), 
			AddressFamily.InterNetworkV6 => BuildIcmpV6EchoReply(sourceAddress, destinationAddress, payload, identifier, sequenceNumber, updateChecksum), 
			_ => throw new NotSupportedException($"{sourceAddress.AddressFamily} not supported."), 
		};
	}

	public static IpPacket BuildIcmpV4EchoReply(IPAddress sourceAddress, IPAddress destinationAddress, ReadOnlySpan<byte> payload, ushort identifier = 0, ushort sequenceNumber = 0, bool updateChecksum = true)
	{
		if (!sourceAddress.IsV4() || !destinationAddress.IsV4())
		{
			throw new ArgumentException("SourceAddress and DestinationAddress must be IPv4 addresses.");
		}
		IpPacket ipPacket = BuildIcmpV4(sourceAddress, destinationAddress, payload);
		IcmpV4Packet icmpV4Packet = ipPacket.ExtractIcmpV4();
		icmpV4Packet.Type = IcmpV4Type.EchoReply;
		icmpV4Packet.Code = 0;
		icmpV4Packet.SequenceNumber = sequenceNumber;
		icmpV4Packet.Identifier = identifier;
		if (updateChecksum)
		{
			ipPacket.UpdateAllChecksums();
		}
		return ipPacket;
	}

	public static IpPacket BuildIcmpV6EchoReply(IPAddress sourceAddress, IPAddress destinationAddress, ReadOnlySpan<byte> payload, ushort identifier = 0, ushort sequenceNumber = 0, bool updateChecksum = true)
	{
		if (!sourceAddress.IsV6() || !destinationAddress.IsV6())
		{
			throw new ArgumentException("SourceAddress and DestinationAddress must be IPv6 addresses.");
		}
		IpPacket ipPacket = BuildIcmpV6(sourceAddress, destinationAddress, payload);
		IcmpV6Packet icmpV6Packet = ipPacket.ExtractIcmpV6();
		icmpV6Packet.Type = IcmpV6Type.EchoReply;
		icmpV6Packet.Code = 0;
		icmpV6Packet.SequenceNumber = sequenceNumber;
		icmpV6Packet.Identifier = identifier;
		if (updateChecksum)
		{
			ipPacket.UpdateAllChecksums();
		}
		return ipPacket;
	}

	public static IpPacket BuildTcpResetReply(IpPacket ipPacket, bool updateChecksum = true)
	{
		if (ipPacket == null)
		{
			throw new ArgumentNullException("ipPacket");
		}
		if (ipPacket.Protocol != IpProtocol.Tcp)
		{
			throw new ArgumentException("packet is not TCP!", "ipPacket");
		}
		TcpPacket tcpPacket = ipPacket.ExtractTcp();
		IpPacket ipPacket2 = BuildTcp(ipPacket.DestinationAddress, ipPacket.SourceAddress, tcpPacket.DestinationPort, tcpPacket.SourcePort, null, null);
		TcpPacket tcpPacket2 = ipPacket2.ExtractTcp();
		tcpPacket2.Reset = true;
		tcpPacket2.WindowSize = 0;
		if (tcpPacket != null && tcpPacket.Synchronize && !tcpPacket.Acknowledgment)
		{
			tcpPacket2.Acknowledgment = true;
			tcpPacket2.SequenceNumber = 0u;
			tcpPacket2.AcknowledgmentNumber = tcpPacket.SequenceNumber + 1;
		}
		else
		{
			tcpPacket2.Acknowledgment = false;
			tcpPacket2.AcknowledgmentNumber = tcpPacket.AcknowledgmentNumber;
			tcpPacket2.SequenceNumber = tcpPacket.AcknowledgmentNumber;
		}
		if (updateChecksum)
		{
			ipPacket2.UpdateAllChecksums();
		}
		return ipPacket2;
	}

	public static IpPacket BuildDns(IPAddress sourceAddress, IPAddress destinationAddress, int sourcePort, int destinationPort, string host, ushort? queryId = null)
	{
		ushort valueOrDefault = queryId.GetValueOrDefault();
		if (!queryId.HasValue)
		{
			valueOrDefault = (ushort)new Random().Next(65535);
			queryId = valueOrDefault;
		}
		byte[] array = DnsResolver.BuildDnsQuery(queryId.Value, host);
		return BuildUdp(sourceAddress, destinationAddress, sourcePort, destinationPort, array);
	}

	public static IpPacket BuildDns(IPEndPoint sourceEndPoint, IPEndPoint destinationEndPoint, string host, ushort? queryId = null)
	{
		return BuildDns(sourceEndPoint.Address, destinationEndPoint.Address, sourceEndPoint.Port, destinationEndPoint.Port, host, queryId);
	}

	public static IpPacket BuildIcmpUnreachableReply(IpPacket ipPacket, bool updateChecksum = true)
	{
		if (ipPacket.Version != IpVersion.IPv6)
		{
			return BuildIcmpV4Error(ipPacket, IcmpV4Type.DestinationUnreachable, IcmpV4Code.HostUnreachable, 0u, updateChecksum);
		}
		return BuildIcmpV6Error(ipPacket, IcmpV6Type.DestinationUnreachable, IcmpV6Code.AddressUnreachable, 0u, updateChecksum);
	}

	public static IpPacket BuildIcmpUnreachablePortReply(IpPacket ipPacket, bool updateChecksum = true)
	{
		if (ipPacket.Version != IpVersion.IPv6)
		{
			return BuildIcmpV4Error(ipPacket, IcmpV4Type.DestinationUnreachable, IcmpV4Code.PortUnreachable, 0u, updateChecksum);
		}
		return BuildIcmpV6Error(ipPacket, IcmpV6Type.DestinationUnreachable, IcmpV6Code.PortUnreachable, 0u, updateChecksum);
	}

	public static IpPacket BuildIcmpPacketTooBigReply(IpPacket ipPacket, int mtu, bool updateChecksum = true)
	{
		if (ipPacket.Version != IpVersion.IPv6)
		{
			return BuildIcmpV4Error(ipPacket, IcmpV4Type.DestinationUnreachable, IcmpV4Code.FragmentationNeeded, (ushort)(mtu & 0xFFFF), updateChecksum);
		}
		return BuildIcmpV6Error(ipPacket, IcmpV6Type.PacketTooBig, IcmpV6Code.NoRoute, (ushort)mtu, updateChecksum);
	}

	public static IpPacket BuildIcmpV4Error(IpPacket ipPacket, IcmpV4Type icmpV4Type, IcmpV4Code code, uint messageSpecific, bool updateChecksum)
	{
		if (ipPacket == null)
		{
			throw new ArgumentNullException("ipPacket");
		}
		Memory<byte> memory = ipPacket.Header;
		int val = memory.Length + 8;
		memory = ipPacket.Buffer;
		int length = Math.Min(val, memory.Length);
		IPAddress destinationAddress = ipPacket.DestinationAddress;
		IPAddress sourceAddress = ipPacket.SourceAddress;
		memory = ipPacket.Buffer;
		IpPacket ipPacket2 = BuildIcmpV4(destinationAddress, sourceAddress, memory.Span.Slice(0, length));
		IcmpV4Packet icmpV4Packet = ipPacket2.ExtractIcmpV4();
		icmpV4Packet.Type = icmpV4Type;
		icmpV4Packet.Code = (byte)code;
		icmpV4Packet.MessageSpecific = messageSpecific;
		if (updateChecksum)
		{
			ipPacket2.UpdateAllChecksums();
		}
		return ipPacket2;
	}

	public static IpPacket BuildIcmpV6Error(IpPacket ipPacket, IcmpV6Type icmpV6Type, IcmpV6Code code, uint messageSpecific, bool updateChecksum = true)
	{
		if (ipPacket == null)
		{
			throw new ArgumentNullException("ipPacket");
		}
		Memory<byte> buffer = ipPacket.Buffer;
		int length = Math.Min(1240, buffer.Length);
		IPAddress destinationAddress = ipPacket.DestinationAddress;
		IPAddress sourceAddress = ipPacket.SourceAddress;
		buffer = ipPacket.Buffer;
		IpPacket ipPacket2 = BuildIcmpV6(destinationAddress, sourceAddress, buffer.Span.Slice(0, length));
		IcmpV6Packet icmpV6Packet = ipPacket2.ExtractIcmpV6();
		icmpV6Packet.Type = icmpV6Type;
		icmpV6Packet.Code = (byte)code;
		icmpV6Packet.MessageSpecific = messageSpecific;
		if (updateChecksum)
		{
			ipPacket2.UpdateAllChecksums();
		}
		return ipPacket2;
	}
}
