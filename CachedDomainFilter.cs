using System;
using VpnHood.Core.Toolkit.Collections;

namespace VpnHood.Core.Filtering.Abstractions;

public class CachedDomainFilter(IDomainFilter netFilter, TimeSpan timeout) : IDomainFilter, IDisposable
{
	private readonly TimeoutDictionary<string, TimeoutItem<FilterAction>> _cache = new TimeoutDictionary<string, TimeoutItem<FilterAction>>(timeout);

	public FilterAction Process(string? domain)
	{
		if (domain == null)
		{
			return netFilter.Process(domain);
		}
		if (_cache.TryGetValue(domain, out TimeoutItem<FilterAction> value))
		{
			return value.Value;
		}
		FilterAction filterAction = netFilter.Process(domain);
		_cache.TryAdd(domain, new TimeoutItem<FilterAction>(filterAction));
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
