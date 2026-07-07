using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Jobs;
using VpnHood.Core.Toolkit.Logging;

namespace VpnHood.Core.Toolkit.Utils;

public class EventReporter : IDisposable
{
	private readonly Lock _lockObject = new Lock();

	private readonly string _message;

	private readonly EventId _eventId;

	private readonly Job _reportJob;

	private bool _disposed;

	public int TotalEventCount { get; private set; }

	public int LastReportEventCount { get; private set; }

	public DateTime LastReportEventTime { get; private set; } = FastDateTime.Now;

	public LogScope LogScope { get; set; }

	public EventReporter(string message, EventId eventId = default(EventId), LogScope? logScope = null, TimeSpan? period = null)
	{
		_message = message;
		_eventId = eventId;
		LogScope = logScope ?? new LogScope();
		_reportJob = new Job(ReportJob, period ?? JobOptions.DefaultInterval, "EventReporter");
	}

	public void Raise()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().Name);
		}
		using (_lockObject.EnterScope())
		{
			TotalEventCount++;
		}
		if (VhLogger.MinLogLevel <= LogLevel.Debug)
		{
			ReportInternal();
		}
	}

	private ValueTask ReportJob(CancellationToken cancellationToken)
	{
		ReportInternal();
		return default(ValueTask);
	}

	private void ReportInternal()
	{
		using (_lockObject.EnterScope())
		{
			if (TotalEventCount - LastReportEventCount != 0)
			{
				Report();
				LastReportEventTime = FastDateTime.Now;
				LastReportEventCount = TotalEventCount;
			}
		}
	}

	protected virtual void Report()
	{
		Tuple<string, object>[] array = new Tuple<string, object>[3]
		{
			Tuple.Create("EventDuration", (object)(FastDateTime.Now - LastReportEventTime).ToString("hh\\:mm\\:ss")),
			Tuple.Create("EventCount", (object)(TotalEventCount - LastReportEventCount)),
			Tuple.Create("EventTotal", (object)TotalEventCount)
		};
		LogScope logScope = LogScope;
		if (logScope != null)
		{
			List<Tuple<string, object>> data = logScope.Data;
			if (data != null && data.Count > 0)
			{
				array = array.Concat<Tuple<string, object>>(LogScope.Data).ToArray();
			}
		}
		string message = _message + " " + string.Join(", ", array.Select((Tuple<string, object> x) => x.Item1 + ": {" + x.Item1 + "}"));
		VhLogger.Instance.LogInformation(_eventId, message, array.Select((Tuple<string, object> x) => x.Item2).ToArray());
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			ReportInternal();
			_reportJob.Dispose();
			_disposed = true;
		}
	}
}
