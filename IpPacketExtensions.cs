using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Packets.Extensions;

public static class IpPacketExtensions
{
	extension(IpPacket ipPacket)
	{
		public IpPacket Clone()
		{
			return PacketBuilder.Parse(ipPacket.Buffer.Span);
		}

		public UdpPacket ExtractUdp()
		{
			if (ipPacket.Protocol != IpProtocol.Udp)
			{
				throw new InvalidDataException($"Invalid UDP packet. It is: {ipPacket.Protocol}");
			}
			if (ipPacket.PayloadPacket == null)
			{
				IPayloadPacket payloadPacket = (ipPacket.PayloadPacket = new UdpPacket(ipPacket.Payload, building: false));
			}
			return (UdpPacket)ipPacket.PayloadPacket;
		}

		public UdpPacket BuildUdp()
		{
			if (ipPacket.Protocol != IpProtocol.Udp)
			{
				throw new InvalidDataException($"Invalid UDP packet. It is: {ipPacket.Protocol}");
			}
			if (ipPacket.PayloadPacket == null)
			{
				IPayloadPacket payloadPacket = (ipPacket.PayloadPacket = new UdpPacket(ipPacket.Payload, building: true));
			}
			return (UdpPacket)ipPacket.PayloadPacket;
		}

		public TcpPacket ExtractTcp()
		{
			if (ipPacket.Protocol != IpProtocol.Tcp)
			{
				throw new InvalidDataException($"Invalid TCP packet. It is: {ipPacket.Protocol}");
			}
			if (ipPacket.PayloadPacket == null)
			{
				IPayloadPacket payloadPacket = (ipPacket.PayloadPacket = new TcpPacket(ipPacket.Payload));
			}
			return (TcpPacket)ipPacket.PayloadPacket;
		}

		public TcpPacket BuildTcp(int optionsLength = 0)
		{
			if (ipPacket.Protocol != IpProtocol.Tcp)
			{
				throw new InvalidDataException($"Invalid TCP packet. It is: {ipPacket.Protocol}");
			}
			if (ipPacket.PayloadPacket == null)
			{
				IPayloadPacket payloadPacket = (ipPacket.PayloadPacket = new TcpPacket(ipPacket.Payload, optionsLength));
			}
			return (TcpPacket)ipPacket.PayloadPacket;
		}

		public IcmpV4Packet BuildIcmpV4()
		{
			if (ipPacket.Protocol != IpProtocol.IcmpV4)
			{
				throw new InvalidDataException($"Invalid IcmpV4 packet. It is: {ipPacket.Protocol}");
			}
			if (ipPacket.PayloadPacket == null)
			{
				IPayloadPacket payloadPacket = (ipPacket.PayloadPacket = new IcmpV4Packet(ipPacket.Payload, building: true));
			}
			return (IcmpV4Packet)ipPacket.PayloadPacket;
		}

		public IcmpV4Packet ExtractIcmpV4()
		{
			if (ipPacket.Protocol != IpProtocol.IcmpV4)
			{
				throw new InvalidDataException($"Invalid IcmpV4 packet. It is: {ipPacket.Protocol}");
			}
			if (ipPacket.PayloadPacket == null)
			{
				IPayloadPacket payloadPacket = (ipPacket.PayloadPacket = new IcmpV4Packet(ipPacket.Payload, building: false));
			}
			return (IcmpV4Packet)ipPacket.PayloadPacket;
		}

		public IcmpV6Packet BuildIcmpV6()
		{
			if (ipPacket.Protocol != IpProtocol.IcmpV6)
			{
				throw new InvalidDataException($"Invalid IcmpV6 packet. It is: {ipPacket.Protocol}");
			}
			if (ipPacket.PayloadPacket == null)
			{
				IPayloadPacket payloadPacket = (ipPacket.PayloadPacket = new IcmpV6Packet(ipPacket.Payload, building: true));
			}
			return (IcmpV6Packet)ipPacket.PayloadPacket;
		}

		public IcmpV6Packet ExtractIcmpV6()
		{
			if (ipPacket.Protocol != IpProtocol.IcmpV6)
			{
				throw new InvalidDataException($"Invalid IcmpV6 packet. It is: {ipPacket.Protocol}");
			}
			if (ipPacket.PayloadPacket == null)
			{
				IPayloadPacket payloadPacket = (ipPacket.PayloadPacket = new IcmpV6Packet(ipPacket.Payload, building: false));
			}
			return (IcmpV6Packet)ipPacket.PayloadPacket;
		}

		public bool IsMulticast()
		{
			IpProtocol protocol = ipPacket.Protocol;
			if ((protocol == IpProtocol.IcmpV4 || protocol == IpProtocol.Udp || protocol == IpProtocol.IcmpV6) ? true : false)
			{
				return ipPacket.DestinationAddress.IsMulticast();
			}
			return false;
		}

		public bool IsBroadcast()
		{
			return IPAddress.Broadcast.SpanEquals(ipPacket.DestinationAddressSpan);
		}

		public IpEndPointValue GetSourceEndPoint()
		{
			return ipPacket.Protocol switch
			{
				IpProtocol.Tcp => new IpEndPointValue(ipPacket.SourceAddress, ipPacket.ExtractTcp().SourcePort), 
				IpProtocol.Udp => new IpEndPointValue(ipPacket.SourceAddress, ipPacket.ExtractUdp().SourcePort), 
				_ => new IpEndPointValue(ipPacket.SourceAddress, 0), 
			};
		}

		public IpEndPointValue GetDestinationEndPoint()
		{
			return ipPacket.Protocol switch
			{
				IpProtocol.Tcp => new IpEndPointValue(ipPacket.DestinationAddress, ipPacket.ExtractTcp().DestinationPort), 
				IpProtocol.Udp => new IpEndPointValue(ipPacket.DestinationAddress, ipPacket.ExtractUdp().DestinationPort), 
				_ => new IpEndPointValue(ipPacket.DestinationAddress, 0), 
			};
		}

		public void SetSourceEndPoint(IpEndPointValue value)
		{
			ipPacket.SourceAddress = value.Address;
			switch (ipPacket.Protocol)
			{
			case IpProtocol.Tcp:
				ipPacket.ExtractTcp().SourcePort = (ushort)value.Port;
				return;
			case IpProtocol.Udp:
				ipPacket.ExtractUdp().SourcePort = (ushort)value.Port;
				return;
			}
			if (value.Port != 0)
			{
				throw new InvalidOperationException($"Cannot set non-zero port {value.Port} for protocol {ipPacket.Protocol}. Only TCP and UDP support ports.");
			}
		}

		public void SetDestinationEndPoint(IpEndPointValue value)
		{
			ipPacket.DestinationAddress = value.Address;
			switch (ipPacket.Protocol)
			{
			case IpProtocol.Tcp:
				ipPacket.ExtractTcp().DestinationPort = (ushort)value.Port;
				return;
			case IpProtocol.Udp:
				ipPacket.ExtractUdp().DestinationPort = (ushort)value.Port;
				return;
			}
			if (value.Port != 0)
			{
				throw new InvalidOperationException($"Cannot set non-zero port {value.Port} for protocol {ipPacket.Protocol}. Only TCP and UDP support ports.");
			}
		}

		public IPEndPointPair GetEndPoints()
		{
			return new IPEndPointPair(ipPacket.GetSourceEndPoint().ToIPEndPoint(), ipPacket.GetDestinationEndPoint().ToIPEndPoint());
		}

		public void UpdateAllChecksums()
		{
			if (ipPacket is IpV4Packet ipV4Packet)
			{
				ipV4Packet.UpdateHeaderChecksum();
			}
			if (ipPacket.Protocol == IpProtocol.Udp)
			{
				ipPacket.ExtractUdp().UpdateChecksum(ipPacket);
			}
			if (ipPacket.Protocol == IpProtocol.Tcp)
			{
				ipPacket.ExtractTcp().UpdateChecksum(ipPacket);
			}
			if (ipPacket.Protocol == IpProtocol.IcmpV4)
			{
				ipPacket.ExtractIcmpV4().UpdateChecksum(ipPacket);
			}
			if (ipPacket.Protocol == IpProtocol.IcmpV6)
			{
				ipPacket.ExtractIcmpV6().UpdateChecksum(ipPacket);
			}
		}

		public bool IsV4()
		{
			return ipPacket.Version == IpVersion.IPv4;
		}

		public bool IsV6()
		{
			return ipPacket.Version == IpVersion.IPv6;
		}

		public bool IsIcmpEcho()
		{
			if (ipPacket != null)
			{
				switch (ipPacket.Version)
				{
				case IpVersion.IPv4:
					if (ipPacket.Protocol != IpProtocol.IcmpV4)
					{
						break;
					}
					return ipPacket.ExtractIcmpV4().IsEcho;
				case IpVersion.IPv6:
					if (ipPacket.Protocol != IpProtocol.IcmpV6)
					{
						break;
					}
					return ipPacket.ExtractIcmpV6().IsEcho;
				}
			}
			return false;
		}

		public byte[] GetUnderlyingBufferUnsafe(byte[] backupBuffer, out int length)
		{
			if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)ipPacket.Buffer, out ArraySegment<byte> segment) && segment.Offset == 0)
			{
				length = segment.Count;
				return segment.Array;
			}
			ipPacket.Buffer.CopyTo(backupBuffer);
			length = ipPacket.Buffer.Length;
			return backupBuffer;
		}

		public byte[] GetUnderlyingBufferUnsafe(byte[] backupBuffer, out int offset, out int length)
		{
			if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)ipPacket.Buffer, out ArraySegment<byte> segment))
			{
				offset = segment.Offset;
				length = segment.Count;
				return segment.Array;
			}
			ipPacket.Buffer.CopyTo(backupBuffer);
			offset = 0;
			length = ipPacket.Buffer.Length;
			return backupBuffer;
		}
	}

	extension(IChecksumPayloadPacket payloadPacket)
	{
		public bool IsChecksumValid(IpPacket ipPacket)
		{
			return payloadPacket.IsChecksumValid(ipPacket.SourceAddressSpan, ipPacket.DestinationAddressSpan);
		}

		public ushort ComputeChecksum(IpPacket ipPacket)
		{
			return payloadPacket.ComputeChecksum(ipPacket.SourceAddressSpan, ipPacket.DestinationAddressSpan);
		}

		public void UpdateChecksum(IpPacket ipPacket)
		{
			payloadPacket.UpdateChecksum(ipPacket.SourceAddressSpan, ipPacket.DestinationAddressSpan);
		}
	}
}
