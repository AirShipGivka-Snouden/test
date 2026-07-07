using System;
using VpnHood.Core.Toolkit.Collections;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Filtering.Abstractions;

public class CachedIpFilter(IIpFilter nextFilter, TimeSpan timeout) : IIpFilter, IDisposable
{
	private readonly TimeoutDictionary<IpEndPointValue, TimeoutItem<FilterAction>> _cache = new TimeoutDictionary<IpEndPointValue, TimeoutItem<FilterAction>>(timeout);

	public FilterAction Process(IpProtocol protocol, IpEndPointValue endPoint)
	{
		if (_cache.TryGetValue(endPoint, out TimeoutItem<FilterAction> value))
		{
			return value.Value;
		}
		FilterAction filterAction = nextFilter.Process(protocol, endPoint);
		_cache.TryAdd(endPoint, new TimeoutItem<FilterAction>(filterAction));
		return filterAction;
	}

	public void ClearCache()
	{
		_cache.Cleanup();
	}

	public void Dispose()
	{
		_cache.Dispose();
	}
}
