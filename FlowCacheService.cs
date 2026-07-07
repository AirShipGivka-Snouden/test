using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Packets;
using VpnHood.Core.Toolkit.Jobs;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Filtering.DomainFiltering.SniFilteringServices;

internal class FlowCacheService : IDisposable
{
	private readonly ConcurrentDictionary<IpEndPointValue, FlowInfo> _flowCache = new ConcurrentDictionary<IpEndPointValue, FlowInfo>();

	private readonly TimeSpan _flowTimeout;

	private readonly Job _cleanupJob;

	private bool _disposed;

	public FlowCacheService(TimeSpan flowTimeout)
	{
		_flowTimeout = flowTimeout;
		TimeSpan period = TimeSpan.FromSeconds(Math.Min(flowTimeout.TotalSeconds, 60.0));
		_cleanupJob = new Job(CleanupExpiredFlows, period, "FlowCacheCleanup");
	}

	public bool TryGetValue(IpEndPointValue key, [NotNullWhen(true)] out FlowInfo? value)
	{
		return _flowCache.TryGetValue(key, out value);
	}

	public bool TryRemove(IpEndPointValue key, out FlowInfo? value)
	{
		return _flowCache.TryRemove(key, out value);
	}

	public void Set(IpEndPointValue key, FlowInfo value)
	{
		_flowCache[key] = value;
	}

	private ValueTask CleanupExpiredFlows(CancellationToken cancellationToken)
	{
		if (_disposed)
		{
			return ValueTask.CompletedTask;
		}
		long num = Environment.TickCount64 * 10000;
		long ticks = _flowTimeout.Ticks;
		foreach (KeyValuePair<IpEndPointValue, FlowInfo> item in _flowCache)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}
			if (num - item.Value.LastSeenTicks <= ticks || !_flowCache.TryRemove(item.Key, out FlowInfo value))
			{
				continue;
			}
			foreach (IpPacket bufferedPacket in value.BufferedPackets)
			{
				bufferedPacket.Dispose();
			}
		}
		return ValueTask.CompletedTask;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		_cleanupJob.Dispose();
		foreach (KeyValuePair<IpEndPointValue, FlowInfo> item in _flowCache)
		{
			foreach (IpPacket bufferedPacket in item.Value.BufferedPackets)
			{
				bufferedPacket.Dispose();
			}
		}
		_flowCache.Clear();
		GC.SuppressFinalize(this);
	}
}
