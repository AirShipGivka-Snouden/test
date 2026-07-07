using System;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Proxies.EndPointManagement.Abstractions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Proxies.EndPointManagement;

internal class ProxyEndPointEntry(ProxyEndPointInfo endPointInfo)
{
	private readonly Lock _lock = new Lock();

	public ProxyEndPointInfo Info => endPointInfo;

	public ProxyEndPointStatus Status => endPointInfo.Status;

	public ProxyEndPoint EndPoint => endPointInfo.EndPoint;

	public IPEndPoint? IpEndPoint { get; init; }

	public long GetSortValue(long currentRequestCount)
	{
		using (_lock.EnterScope())
		{
			return Info.Status.QueuePosition - currentRequestCount;
		}
	}

	private static TimeSpan GetSlowThreshold(TimeSpan fastestLatency)
	{
		return fastestLatency * 2.0 + TimeSpan.FromSeconds(2L);
	}

	public void RecordSuccess(TimeSpan latency, TimeSpan? fastestLatency, long currentQueuePos)
	{
		using (_lock.EnterScope())
		{
			Status.SucceededCount++;
			Status.Latency = latency;
			Status.LastSucceeded = FastDateTime.UtcNow;
			Status.ErrorMessage = null;
			if (fastestLatency.HasValue && latency > GetSlowThreshold(fastestLatency.Value))
			{
				Status.Penalty++;
				VhLogger.Instance.LogDebug("Proxy server responded slowly. {ProxyServer}, ResponseTime: {ResponseTime}, PenaltyRate: {Penalty}", VhLogger.FormatHostName(EndPoint.Host), latency, Status.Penalty);
			}
			else if (Status.Penalty > 0)
			{
				Status.Penalty--;
			}
			UpdatePosition(currentQueuePos);
		}
	}

	private void UpdatePosition(long currentQueuePos)
	{
		using (_lock.EnterScope())
		{
			Info.Status.QueuePosition = currentQueuePos + Status.Penalty * 3 + 1;
		}
	}

	public void RecordFailed(Exception? exception, long currentQueuePos)
	{
		using (_lock.EnterScope())
		{
			Status.Penalty++;
			Status.Penalty++;
			Status.FailedCount++;
			Status.LastFailed = FastDateTime.UtcNow;
			Status.ErrorMessage = exception?.Message;
			UpdatePosition(currentQueuePos);
			VhLogger.Instance.LogDebug("Failed to connect to proxy server. {ProxyServer}, FailedCount: {FailedCount}, Penalty: {Penalty}", VhLogger.FormatHostName(EndPoint.Host), Status.FailedCount, Status.Penalty);
		}
	}
}
