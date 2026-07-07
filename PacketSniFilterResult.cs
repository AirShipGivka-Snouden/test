using System;
using System.Collections.Generic;
using VpnHood.Core.Filtering.Abstractions;
using VpnHood.Core.Packets;

namespace VpnHood.Core.Filtering.DomainFiltering;

public readonly struct PacketSniFilterResult
{
	public FilterAction Action { get; }

	public string? DomainName { get; }

	public bool IsNewFlow { get; }

	public bool NeedMore { get; }

	public IReadOnlyList<IpPacket> Packets { get; init; }

	public PacketSniFilterResult(FilterAction action, string? domainName, IReadOnlyList<IpPacket>? packets, bool isNewFlow)
	{
		NeedMore = false;
		Packets = Array.Empty<IpPacket>();
		Action = action;
		DomainName = domainName;
		IsNewFlow = isNewFlow;
		Packets = packets ?? Packets;
	}

	public PacketSniFilterResult(FilterAction action)
	{
		DomainName = null;
		IsNewFlow = false;
		NeedMore = false;
		Packets = Array.Empty<IpPacket>();
		Action = action;
	}

	public PacketSniFilterResult(bool needMore)
	{
		DomainName = null;
		IsNewFlow = false;
		Packets = Array.Empty<IpPacket>();
		Action = FilterAction.Block;
		NeedMore = needMore;
	}

	public static PacketSniFilterResult Pending()
	{
		return new PacketSniFilterResult(needMore: true);
	}

	public static PacketSniFilterResult Passthrough()
	{
		return new PacketSniFilterResult(FilterAction.Default);
	}
}
