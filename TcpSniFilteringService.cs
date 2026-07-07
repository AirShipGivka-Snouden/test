using System;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Filtering.Abstractions;
using VpnHood.Core.Filtering.DomainFiltering.SniExtractors;
using VpnHood.Core.Filtering.DomainFiltering.SniExtractors.Tcp;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Filtering.DomainFiltering.SniFilteringServices;

public class TcpSniFilteringService : PacketSniFilteringService
{
	protected override string ProtocolName => "TLS";

	public TcpSniFilteringService(IDomainFilter domainFilter, TimeSpan flowTimeout, EventId? sniEventId)
		: base(domainFilter, flowTimeout, sniEventId)
	{
	}

	protected override bool TryValidateAndExtractPayload(IpPacket ipPacket, out IpEndPointValue flowKey, out ReadOnlySpan<byte> payload)
	{
		flowKey = default(IpEndPointValue);
		payload = default(ReadOnlySpan<byte>);
		if (ipPacket.Protocol != IpProtocol.Tcp)
		{
			return false;
		}
		TcpPacket tcpPacket = ipPacket.ExtractTcp();
		if (tcpPacket == null || tcpPacket.DestinationPort != 443)
		{
			return false;
		}
		Memory<byte> payload2 = tcpPacket.Payload;
		if (payload2.Length == 0)
		{
			return false;
		}
		flowKey = new IpEndPointValue(ipPacket.SourceAddress, tcpPacket.SourcePort);
		payload2 = tcpPacket.Payload;
		payload = payload2.Span;
		return true;
	}

	protected override bool IsFlowEnd(IpPacket ipPacket)
	{
		if (ipPacket.Protocol != IpProtocol.Tcp)
		{
			return false;
		}
		TcpPacket tcpPacket = ipPacket.ExtractTcp();
		if (!tcpPacket.Finish)
		{
			return tcpPacket.Reset;
		}
		return true;
	}

	protected override PacketSniResult ExtractSni(ReadOnlySpan<byte> payload, object? state, long nowTicks)
	{
		return TcpSniExtractor.TryExtractSniFromTcpPayload(payload, state, nowTicks);
	}
}
