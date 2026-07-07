using System.Collections.Generic;
using System.Linq;
using System.Net;
using VpnHood.Core.Filtering.Abstractions;
using VpnHood.Core.Filtering.DomainFiltering;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Tunneling;
using VpnHood.Core.Tunneling.Exceptions;
using VpnHood.Core.Tunneling.Proxies;

namespace VpnHood.Core.Client;

internal class ClientPacketHandler(Tunnel tunnel, IClientTcpHost clientTcpHost, DomainFilteringService domainFilteringService, NetFilter netFilter, ProxyManager proxyManager, IReadOnlyList<IPAddress> dnsServers, bool isIpV6SupportedByServer, PassthroughState passthroughState)
{
	public bool IsDnsOverTlsDetected { get; private set; }

	public bool DropQuic { get; set; }

	public bool DropUdp { get; set; }

	public bool UseTcpProxy { get; set; }

	public bool IsIpV6SupportedByClient { get; set; }

	public bool IsIpV6SupportedByServer => isIpV6SupportedByServer;

	public void ProcessOutgoingPacket(IpPacket ipPacket)
	{
		if (ipPacket.Protocol == IpProtocol.Udp && domainFilteringService.IsEnabled)
		{
			ProcessOutgoingPacketWithDomainFilter(ipPacket);
		}
		else
		{
			ProcessOutgoingPacket(ipPacket, FilterAction.Default);
		}
	}

	private void ProcessOutgoingPacketWithDomainFilter(IpPacket ipPacket)
	{
		PacketSniFilterResult packetSniFilterResult = domainFilteringService.ProcessPacket(ipPacket);
		if (packetSniFilterResult.NeedMore)
		{
			return;
		}
		if (packetSniFilterResult.Action == FilterAction.Block)
		{
			foreach (IpPacket packet in packetSniFilterResult.Packets)
			{
				packet.Dispose();
			}
			ipPacket.Dispose();
			return;
		}
		foreach (IpPacket packet2 in packetSniFilterResult.Packets)
		{
			ProcessOutgoingPacket(packet2, packetSniFilterResult.Action);
		}
		ProcessOutgoingPacket(ipPacket, packetSniFilterResult.Action);
	}

	private void ProcessOutgoingPacket(IpPacket ipPacket, FilterAction filterAction)
	{
		if (filterAction == FilterAction.Default && netFilter.IpFilter != null)
		{
			filterAction = netFilter.IpFilter.Process(ipPacket.Protocol, ipPacket.GetDestinationEndPoint());
		}
		if (clientTcpHost.IsOwnPacket(ipPacket))
		{
			clientTcpHost.ProcessOutgoingPacket(ipPacket);
			return;
		}
		if (filterAction == FilterAction.Block)
		{
			throw new NetFilterException("A packet has been dropped by the domain filter.");
		}
		if (ShouldPassthroughForAd(ipPacket))
		{
			filterAction = FilterAction.Exclude;
		}
		if (ipPacket.IsIcmpEcho())
		{
			filterAction = FilterAction.Include;
		}
		IsDnsOverTlsDetected |= ipPacket.Protocol == IpProtocol.Tcp && ipPacket.ExtractTcp().DestinationPort == 853;
		if (filterAction == FilterAction.Include)
		{
			ProcessOutgoingPacketInclude(ipPacket);
		}
		else
		{
			ProcessOutgoingPacketExclude(ipPacket);
		}
	}

	private void ProcessOutgoingPacketInclude(IpPacket ipPacket)
	{
		if (ipPacket.IsV6() && !IsIpV6SupportedByServer)
		{
			throw new PacketDropException("A protected IPv6 packet is dropped because server can not handle it.");
		}
		if (ipPacket.Protocol == IpProtocol.Tcp)
		{
			if (UseTcpProxy)
			{
				clientTcpHost.ProcessOutgoingPacket(ipPacket);
			}
			else
			{
				tunnel.SendPacketQueued(ipPacket);
			}
		}
		else if (ipPacket.Protocol == IpProtocol.Udp)
		{
			if (ShouldDropUdpPacket(ipPacket.ExtractUdp()))
			{
				throw new PacketDropException("A UDP packet is dropped because it is blocked by the configuration.");
			}
			tunnel.SendPacketQueued(ipPacket);
		}
		else
		{
			if (!ipPacket.IsIcmpEcho())
			{
				throw new PacketDropException("Packet has been dropped because no one handle it.");
			}
			tunnel.SendPacketQueued(ipPacket);
		}
	}

	private void ProcessOutgoingPacketExclude(IpPacket ipPacket)
	{
		if (ipPacket.Protocol != IpProtocol.Tcp)
		{
			IIpMapper? ipMapper = netFilter.IpMapper;
			if (ipMapper != null && ipMapper.ToHost(ipPacket.Protocol, ipPacket.GetDestinationEndPoint(), out var newEndPoint))
			{
				ipPacket.SetDestinationEndPoint(newEndPoint);
				ipPacket.UpdateAllChecksums();
			}
		}
		if (ipPacket.IsV6() && !IsIpV6SupportedByClient)
		{
			throw new PacketDropException("An unprotected IPv6 packet is dropped because client can not handle it.");
		}
		if (ipPacket.Protocol == IpProtocol.Tcp)
		{
			clientTcpHost.ProcessOutgoingPacket(ipPacket);
			return;
		}
		if (ipPacket.Protocol == IpProtocol.Udp)
		{
			proxyManager.SendPacketQueued(ipPacket);
			return;
		}
		if (ipPacket.IsIcmpEcho())
		{
			throw new PacketDropException("An ICMP echo request packet is dropped because it can not be handled by the local proxy.");
		}
		throw new PacketDropException("Packet has been dropped because no one handle it.");
	}

	private bool ShouldDropUdpPacket(UdpPacket udpPacket)
	{
		ushort destinationPort = udpPacket.DestinationPort;
		if ((destinationPort == 53 || destinationPort == 853) ? true : false)
		{
			return false;
		}
		if (DropUdp)
		{
			return true;
		}
		bool flag = DropQuic;
		if (flag)
		{
			destinationPort = udpPacket.DestinationPort;
			bool flag2 = ((destinationPort == 80 || destinationPort == 443) ? true : false);
			flag = flag2;
		}
		return flag;
	}

	private bool ShouldPassthroughForAd(IpPacket ipPacket)
	{
		if (!passthroughState.PassthroughForAd)
		{
			return false;
		}
		return ipPacket.Protocol != IpProtocol.Udp || (ipPacket.GetDestinationEndPoint().Port != 53 && !dnsServers.Contains(ipPacket.DestinationAddress));
	}
}
