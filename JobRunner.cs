using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Toolkit.Jobs;

public class JobRunner
{
	private class JobItem
	{
		public required string Name { get; init; }

		public required WeakReference<Job> JobReference { get; init; }
	}

	private SemaphoreSlim _semaphore;

	private readonly LinkedList<JobItem> _jobs = new LinkedList<JobItem>();

	private static readonly Lazy<JobRunner> SlowInstanceLazy = new Lazy<JobRunner>(() => new JobRunner(TimeSpan.FromSeconds(10L)));

	private static readonly Lazy<JobRunner> FastInstanceLazy = new Lazy<JobRunner>(() => new JobRunner(TimeSpan.FromSeconds(2L)));

	private int _maxDegreeOfParallelism = 2;

	private readonly TimeSpan _cleanupTimeSpan = TimeSpan.FromSeconds(60L);

	private DateTime _lastCleanupTime = FastDateTime.Now;

	public static JobRunner SlowInstance => SlowInstanceLazy.Value;

	public static JobRunner FastInstance => FastInstanceLazy.Value;

	public TimeSpan Interval { get; set; }

	public ILogger? Logger { get; set; } = VhLogger.Instance;

	public int MaxDegreeOfParallelism
	{
		get
		{
			return _maxDegreeOfParallelism;
		}
		set
		{
			if (value < 1)
			{
				throw new ArgumentOutOfRangeException("value", "MaxDegreeOfParallelism must be greater than 0.");
			}
			_maxDegreeOfParallelism = value;
			_semaphore = new SemaphoreSlim(_maxDegreeOfParallelism);
		}
	}

	public JobRunner(TimeSpan interval)
	{
		Interval = interval;
		_semaphore = new SemaphoreSlim(_maxDegreeOfParallelism);
		Task.Run((Func<Task?>)RunJobs);
	}

	private async Task RunJobs()
	{
		while (true)
		{
			await Task.Delay(Interval).Vhc();
			DateTime now = FastDateTime.Now;
			if (now - _lastCleanupTime >= _cleanupTimeSpan)
			{
				RemoveDeadCallbacks();
				_lastCleanupTime = now;
			}
			await RunJobsInternal().Vhc();
		}
	}

	private async Task RunJobsInternal()
	{
		IReadOnlyList<Job> readyJobs = GetReadyJobs();
		foreach (Job jobCallback in readyJobs)
		{
			await _semaphore.WaitAsync().Vhc();
			RunJob(jobCallback);
		}
	}

	private async Task RunJob(Job job)
	{
		try
		{
			await job.RunNow().Vhc();
		}
		catch (ObjectDisposedException)
		{
			Remove(job);
		}
		catch (Exception exception)
		{
			Logger?.LogCritical(exception, "JobCallback should not throw this exception.");
		}
		finally
		{
			_semaphore.Release();
		}
	}

	private void RemoveDeadCallbacks()
	{
		lock (_jobs)
		{
			LinkedListNode<JobItem> linkedListNode = _jobs.First;
			while (linkedListNode != null)
			{
				LinkedListNode<JobItem> next = linkedListNode.Next;
				if (!linkedListNode.Value.JobReference.TryGetTarget(out Job _))
				{
					Logger?.LogDebug("Removing a dead job. Ensure proper disposal by the caller. JobName: {JobName}", linkedListNode.Value.Name);
					_jobs.Remove(linkedListNode);
				}
				linkedListNode = next;
			}
		}
	}

	private IReadOnlyList<Job> GetReadyJobs()
	{
		lock (_jobs)
		{
			List<Job> list = new List<Job>(_jobs.Count);
			foreach (JobItem job in _jobs)
			{
				if (job.JobReference.TryGetTarget(out Job target) && target.IsReadyToRun)
				{
					list.Add(target);
				}
			}
			return list;
		}
	}

	public void Add(Job job)
	{
		lock (_jobs)
		{
			_jobs.AddLast(new JobItem
			{
				Name = job.Name,
				JobReference = new WeakReference<Job>(job)
			});
		}
	}

	public void Remove(Job job)
	{
		lock (_jobs)
		{
			JobItem jobItem = _jobs.FirstOrDefault((JobItem x) => x.JobReference.TryGetTarget(out Job target) && target == job);
			if (jobItem != null)
			{
				_jobs.Remove(jobItem);
			}
		}
	}
}
