using System;
using System.Threading;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Toolkit.Monitoring;

public class ProgressMonitor(int totalTaskCount, TimeSpan taskTimeout, int maxDegreeOfParallelism = 1)
{
	private readonly DateTime _startTime = FastDateTime.Now;

	private DateTime _currentBatchStartTime = FastDateTime.Now;

	private readonly Lock _incrementLock = new Lock();

	private int _completedTaskCount;

	private int TotalBatches => (int)Math.Ceiling((double)totalTaskCount / (double)maxDegreeOfParallelism);

	private TimeSpan MaxDuration => TimeSpan.FromMilliseconds(taskTimeout.TotalMilliseconds * (double)TotalBatches);

	private int CurrentBatchIndex => _completedTaskCount / maxDegreeOfParallelism;

	public ProgressStatus Progress => new ProgressStatus(_completedTaskCount, totalTaskCount, _startTime, ProgressPercentage);

	private int ProgressPercentage
	{
		get
		{
			using (_incrementLock.EnterScope())
			{
				double val = (double)_completedTaskCount / (double)totalTaskCount * 100.0;
				TimeSpan timeSpan = CurrentBatchIndex * taskTimeout + (FastDateTime.Now - _currentBatchStartTime);
				TimeSpan timeSpan2 = (CurrentBatchIndex + 1) * taskTimeout;
				if (timeSpan > timeSpan2)
				{
					timeSpan = timeSpan2;
				}
				double val2 = timeSpan / MaxDuration * 100.0;
				double val3 = Math.Max(val, val2);
				val3 = Math.Min(100.0, val3);
				val3 = Math.Max(0.0, val3);
				return (int)val3;
			}
		}
	}

	public void IncrementCompleted()
	{
		using (_incrementLock.EnterScope())
		{
			_completedTaskCount++;
			if (_completedTaskCount % maxDegreeOfParallelism == 0)
			{
				_currentBatchStartTime = FastDateTime.Now;
			}
		}
	}

	public void Finish()
	{
		using (_incrementLock.EnterScope())
		{
			_completedTaskCount = totalTaskCount;
		}
	}
}
