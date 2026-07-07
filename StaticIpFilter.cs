using System;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Filtering.Abstractions;

public class StaticIpFilter(IIpFilter? nextFilter) : IIpFilter, IDisposable
{
	public IpRangeOrderedList BlockedRanges { get; set; } = new IpRangeOrderedList();

	public IpRangeOrderedList ExcludeRanges { get; set; } = new IpRangeOrderedList();

	public IpRangeOrderedList IncludeRanges { get; set; } = new IpRangeOrderedList();

	public FilterAction Process(IpProtocol protocol, IpEndPointValue endPoint)
	{
		FilterAction filterAction = nextFilter?.Process(protocol, endPoint) ?? FilterAction.Default;
		if (filterAction != FilterAction.Default)
		{
			return filterAction;
		}
		if (BlockedRanges.Count > 0 && BlockedRanges.Contains(endPoint.Address))
		{
			return FilterAction.Block;
		}
		if (ExcludeRanges.Count > 0 && ExcludeRanges.Contains(endPoint.Address))
		{
			return FilterAction.Exclude;
		}
		if (IncludeRanges.Count > 0 && IncludeRanges.Contains(endPoint.Address))
		{
			return FilterAction.Include;
		}
		return FilterAction.Default;
	}

	public void Dispose()
	{
	}
}
