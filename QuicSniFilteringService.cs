using System;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Filtering.Abstractions;
using VpnHood.Core.Filtering.DomainFiltering.SniExtractors;
using VpnHood.Core.Filtering.DomainFiltering.SniExtractors.Quic;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Filtering.DomainFiltering.SniFilteringServices;

public class QuicSniFilteringService : PacketSniFilteringService
{
	protected override string ProtocolName => "QUIC";

	public QuicSniFilteringService(IDomainFilter domainFilter, TimeSpan flowTimeout, EventId? sniEventId)
		: base(domainFilter, flowTimeout, sniEventId)
	{
	}

	protected override bool TryValidateAndExtractPayload(IpPacket ipPacket, out IpEndPointValue flowKey, out ReadOnlySpan<byte> payload)
	{
		flowKey = default(IpEndPointValue);
		payload = default(ReadOnlySpan<byte>);
		if (ipPacket.Protocol != IpProtocol.Udp)
		{
			return false;
		}
		UdpPacket udpPacket = ipPacket.ExtractUdp();
		flowKey = new IpEndPointValue(ipPacket.SourceAddress, udpPacket.SourcePort);
		payload = udpPacket.Payload.Span;
		return true;
	}

	protected override PacketSniResult ExtractSni(ReadOnlySpan<byte> payload, object? state, long nowTicks)
	{
		QuicSniState state2 = state as QuicSniState;
		QuicSniResult quicSniResult = QuicSniExtractor.TryExtractSniFromUdpPayload(payload, state2, nowTicks);
		if (quicSniResult.DomainName != null)
		{
			return PacketSniResult.Found(quicSniResult.DomainName);
		}
		if (quicSniResult.NeedMore && quicSniResult.State != null)
		{
			return PacketSniResult.Pending(quicSniResult.State);
		}
		return PacketSniResult.NotFound;
	}
}
