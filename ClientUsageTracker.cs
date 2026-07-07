using System;
using System.Threading;
using System.Threading.Tasks;
using Ga4.Trackers;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Toolkit.Jobs;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Client;

internal class ClientUsageTracker : IDisposable
{
	private readonly TimeSpan _reportInterval = TimeSpan.FromMinutes(25L);

	private readonly AsyncLock _reportLock = new AsyncLock();

	private Traffic _lastTraffic = new Traffic();

	private int _lastRequestCount;

	private int _lastConnectionCount;

	private bool _disposed;

	private readonly ISessionStatus _sessionStatus;

	private readonly ITracker _tracker;

	private readonly Job _reportJob;

	public ClientUsageTracker(ISessionStatus sessionStatus, ITracker tracker)
	{
		_sessionStatus = sessionStatus;
		_tracker = tracker;
		_reportJob = new Job(Report, new JobOptions
		{
			Interval = _reportInterval,
			MaxRetry = 2,
			Name = "ClientReporter"
		});
	}

	public async ValueTask Report(CancellationToken cancellationToken)
	{
		using AsyncLock.ILockAsyncResult lockAsync = await _reportLock.LockAsync(TimeSpan.Zero, cancellationToken).Vhc();
		if (lockAsync.Succeeded)
		{
			Traffic traffic = _sessionStatus.SessionTraffic;
			Traffic traffic2 = traffic - _lastTraffic;
			int requestCount = _sessionStatus.ConnectorStatus.RequestCount;
			int connectionCount = _sessionStatus.ConnectorStatus.CreatedConnectionCount;
			TrackEvent item = ClientTrackerBuilder.BuildUsage(traffic2, requestCount - _lastRequestCount, connectionCount - _lastConnectionCount);
			await _tracker.Track(new global::_003C_003Ez__ReadOnlySingleElementList<TrackEvent>(item), cancellationToken).Vhc();
			_lastTraffic = traffic;
			_lastRequestCount = requestCount;
			_lastConnectionCount = connectionCount;
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_reportJob.Dispose();
			_disposed = true;
		}
	}
}
