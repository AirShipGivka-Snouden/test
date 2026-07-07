using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Filtering.Abstractions;
using VpnHood.Core.Filtering.DomainFiltering.SniExtractors;
using VpnHood.Core.Packets;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Filtering.DomainFiltering.SniFilteringServices;

public abstract class PacketSniFilteringService(IDomainFilter domainFilter, TimeSpan flowTimeout, EventId? sniEventId) : IDisposable
{
	private readonly FlowCacheService _flowCacheService = new FlowCacheService(flowTimeout);

	private bool _disposed;

	protected IDomainFilter DomainFilter { get; } = domainFilter;

	protected EventId? SniEventId { get; } = sniEventId;

	protected abstract string ProtocolName { get; }

	public PacketSniFilterResult ProcessPacket(IpPacket ipPacket)
	{
		ObjectDisposedException.ThrowIf(_disposed, GetType().Name);
		if (!TryValidateAndExtractPayload(ipPacket, out var flowKey, out var payload))
		{
			return PacketSniFilterResult.Passthrough();
		}
		long num = Environment.TickCount64 * 10000;
		if (_flowCacheService.TryGetValue(flowKey, out FlowInfo value) && value.Decision.HasValue)
		{
			if (IsFlowEnd(ipPacket))
			{
				_flowCacheService.TryRemove(flowKey, out FlowInfo _);
			}
			else
			{
				value.LastSeenTicks = num;
			}
			return new PacketSniFilterResult(value.Decision.Value, value.DomainName, null, isNewFlow: false);
		}
		PacketSniResult packetSniResult = ExtractSni(payload, value?.SniState, num);
		if (packetSniResult.DomainName != null)
		{
			LogSni(packetSniResult.DomainName, ipPacket);
			return HandleSniFound(flowKey, value, packetSniResult.DomainName, num);
		}
		if (packetSniResult.NeedMore && packetSniResult.State != null)
		{
			return HandleNeedMore(flowKey, ipPacket, value, packetSniResult.State, num);
		}
		return HandleGiveUp(flowKey, value, num);
	}

	protected abstract bool TryValidateAndExtractPayload(IpPacket ipPacket, out IpEndPointValue flowKey, out ReadOnlySpan<byte> payload);

	protected abstract PacketSniResult ExtractSni(ReadOnlySpan<byte> payload, object? state, long nowTicks);

	protected virtual bool IsFlowEnd(IpPacket ipPacket)
	{
		return false;
	}

	private void LogSni(string domainName, IpPacket ipPacket)
	{
		if (SniEventId.HasValue)
		{
			VhLogger.Instance.LogInformation(SniEventId.Value, "Domain: {Domain}, DestEp: {IP}, Protocol: {Protocol}", VhLogger.FormatHostName(domainName), VhLogger.Format(ipPacket.DestinationAddress), ProtocolName);
		}
	}

	private PacketSniFilterResult HandleSniFound(IpEndPointValue flowKey, FlowInfo? flowInfo, string domainName, long nowTicks)
	{
		FilterAction filterAction = DomainFilter.Process(domainName);
		List<IpPacket> packets = flowInfo?.BufferedPackets ?? new List<IpPacket>();
		FlowInfo flowInfo2 = flowInfo ?? new FlowInfo();
		flowInfo2.DomainName = domainName;
		flowInfo2.Decision = filterAction;
		flowInfo2.BufferedPackets = new List<IpPacket>();
		flowInfo2.SniState = null;
		flowInfo2.LastSeenTicks = nowTicks;
		_flowCacheService.Set(flowKey, flowInfo2);
		return new PacketSniFilterResult(filterAction, domainName, packets, isNewFlow: true);
	}

	private PacketSniFilterResult HandleNeedMore(IpEndPointValue flowKey, IpPacket ipPacket, FlowInfo? flowInfo, object sniState, long nowTicks)
	{
		FlowInfo flowInfo2 = flowInfo ?? new FlowInfo();
		flowInfo2.SniState = sniState;
		flowInfo2.BufferedPackets.Add(ipPacket);
		flowInfo2.LastSeenTicks = nowTicks;
		_flowCacheService.Set(flowKey, flowInfo2);
		return PacketSniFilterResult.Pending();
	}

	private PacketSniFilterResult HandleGiveUp(IpEndPointValue flowKey, FlowInfo? flowInfo, long nowTicks)
	{
		FlowInfo flowInfo2 = flowInfo ?? new FlowInfo();
		flowInfo2.Decision = FilterAction.Default;
		flowInfo2.BufferedPackets = new List<IpPacket>();
		flowInfo2.SniState = null;
		flowInfo2.LastSeenTicks = nowTicks;
		_flowCacheService.Set(flowKey, flowInfo2);
		return new PacketSniFilterResult(FilterAction.Default, null, flowInfo?.BufferedPackets, isNewFlow: false);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_flowCacheService.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
