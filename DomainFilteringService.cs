using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Filtering.Abstractions;
using VpnHood.Core.Filtering.DomainFiltering.Observation;
using VpnHood.Core.Filtering.DomainFiltering.SniExtractors;
using VpnHood.Core.Filtering.DomainFiltering.SniExtractors.TlsStream;
using VpnHood.Core.Filtering.DomainFiltering.SniFilteringServices;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Filtering.DomainFiltering;

public class DomainFilteringService
{
	private static readonly TimeSpan UdpFlowTimeout = TimeSpan.FromMinutes(2L);

	private readonly IDomainFilter _domainFilter;

	private readonly QuicSniFilteringService _quicSniService;

	private readonly EventId _sniEventId;

	private readonly int _tlsBufferSize;

	private readonly bool _trackObservations;

	public DomainObserver DomainObserver { get; }

	public DomainObserverStat TcpStat { get; } = new DomainObserverStat();

	public DomainObserverStat QuicStat { get; } = new DomainObserverStat();

	public bool IsEnabled { get; set; }

	public DomainFilteringService(IDomainFilter domainFilter, EventId sniEventId, int tlsBufferSize, bool trackObservations = false)
	{
		_domainFilter = domainFilter;
		_sniEventId = sniEventId;
		_tlsBufferSize = tlsBufferSize;
		_trackObservations = trackObservations;
		_quicSniService = new QuicSniFilteringService(_domainFilter, UdpFlowTimeout, sniEventId);
		DomainObserver = new DomainObserver(sniEventId);
	}

	public PacketSniFilterResult ProcessPacket(IpPacket ipPacket)
	{
		if (!IsEnabled)
		{
			return PacketSniFilterResult.Passthrough();
		}
		PacketSniFilterResult packetSniFilterResult = ((ipPacket.Protocol != IpProtocol.Udp) ? PacketSniFilterResult.Passthrough() : _quicSniService.ProcessPacket(ipPacket));
		PacketSniFilterResult result = packetSniFilterResult;
		if (!result.IsNewFlow || string.IsNullOrEmpty(result.DomainName))
		{
			return result;
		}
		QuicStat.Update(result.Action);
		if (_trackObservations)
		{
			DomainObserver.Track(result.DomainName, result.Action, DomainObservationProtocol.Quic, ipPacket.GetDestinationEndPoint());
		}
		return result;
	}

	public async Task<StreamSniFilterResult> ProcessStream(Stream tlsStream, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
	{
		if (!IsEnabled)
		{
			return StreamSniFilterResult.Passthrough();
		}
		StreamSniResult streamSniResult = await StreamSniExtractor.ExtractSni(tlsStream, _sniEventId, _tlsBufferSize, cancellationToken).Vhc();
		StreamSniFilterResult result = new StreamSniFilterResult
		{
			DomainName = streamSniResult.DomainName,
			ReadData = streamSniResult.ReadData,
			Action = _domainFilter.Process(streamSniResult.DomainName)
		};
		if (!string.IsNullOrEmpty(result.DomainName))
		{
			TcpStat.Update(result.Action);
			if (_trackObservations)
			{
				DomainObserver.Track(result.DomainName, result.Action, DomainObservationProtocol.Tcp, remoteEndPoint.ToValue());
			}
		}
		return result;
	}
}
