using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Toolkit.Jobs;

public class Job : IDisposable
{
	private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

	private readonly SemaphoreSlim _jobSemaphore = new SemaphoreSlim(1, 1);

	private readonly Func<CancellationToken, ValueTask> _jobFunc;

	private readonly TimeSpan _dueTime;

	private readonly int? _maxRetry;

	private long _currentFailedCount;

	private int _isDisposed;

	private readonly JobRunner _jobRunner;

	public TimeSpan Interval { get; set; }

	public long SucceededCount { get; private set; }

	public long FailedCount { get; private set; }

	public bool IsStarted => StartedTime.HasValue;

	public DateTime? StartedTime { get; set; }

	public DateTime? LastExecutedTime { get; private set; }

	public string Name { get; init; }

	public bool IsReadyToRun
	{
		get
		{
			if (!StartedTime.HasValue)
			{
				return false;
			}
			if (_jobSemaphore.CurrentCount == 0)
			{
				return false;
			}
			DateTime now = FastDateTime.Now;
			DateTime value;
			DateTime? startedTime;
			if (!LastExecutedTime.HasValue)
			{
				value = now;
				startedTime = StartedTime;
				return value - startedTime >= _dueTime;
			}
			value = now;
			startedTime = LastExecutedTime;
			return value - startedTime >= Interval;
		}
	}

	public Job(Func<CancellationToken, ValueTask> jobFunc, JobOptions options, JobRunner? jobRunner = null)
	{
		_jobFunc = jobFunc;
		_dueTime = options.DueTime ?? options.Interval;
		Interval = options.Interval;
		_maxRetry = options.MaxRetry;
		Name = options.Name ?? "NoName";
		if (options.AutoStart)
		{
			Start();
		}
		if (jobRunner != null)
		{
			_jobRunner = jobRunner;
		}
		else
		{
			_jobRunner = ((options.Interval >= JobRunner.SlowInstance.Interval) ? JobRunner.SlowInstance : JobRunner.FastInstance);
		}
		_jobRunner.Add(this);
	}

	public Job(Func<CancellationToken, ValueTask> jobFunc, TimeSpan period, string? name = null)
		: this(jobFunc, new JobOptions
		{
			Interval = period,
			Name = name,
			DueTime = TimeSpan.Zero
		})
	{
	}

	public Job(Func<CancellationToken, ValueTask> jobFunc, string? name = null)
		: this(jobFunc, new JobOptions
		{
			Name = name,
			DueTime = TimeSpan.Zero
		})
	{
	}

	public void Start()
	{
		if (_isDisposed != 0)
		{
			throw new ObjectDisposedException("Job");
		}
		if (IsStarted)
		{
			throw new InvalidOperationException("Job is already started.");
		}
		StartedTime = FastDateTime.Now;
	}

	public void Stop()
	{
		if (!IsStarted)
		{
			throw new InvalidOperationException("Job is not started.");
		}
		StartedTime = null;
		_currentFailedCount = 0L;
	}

	public Task RunNow()
	{
		return RunInternal(_cancellationTokenSource.Token);
	}

	public async Task RunNow(CancellationToken cancellationToken)
	{
		using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
		await RunInternal(linkedCts.Token).ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task RunInternal(CancellationToken cancellationToken)
	{
		if (_isDisposed != 0)
		{
			throw new ObjectDisposedException("Job");
		}
		await _jobSemaphore.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			await _jobFunc(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			_currentFailedCount = 0L;
			SucceededCount++;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
		}
		catch (Exception exception)
		{
			FailedCount++;
			_currentFailedCount++;
			if (_currentFailedCount > _maxRetry)
			{
				_jobRunner.Logger?.LogError(exception, "Job failed too many times and stopped. JobName: {JobName}, FailedCount: {FailedCount}, TotalErrorCount: {TotalFailedCount}", Name, _currentFailedCount, FailedCount);
				Stop();
			}
			throw;
		}
		finally
		{
			LastExecutedTime = FastDateTime.Now;
			VhUtils.TryInvoke(() => _jobSemaphore.Release(), 0);
		}
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, 1) != 1)
		{
			_cancellationTokenSource.TryCancel();
			_cancellationTokenSource.Dispose();
			_jobSemaphore.Dispose();
			StartedTime = null;
			_jobRunner.Remove(this);
		}
	}
}
