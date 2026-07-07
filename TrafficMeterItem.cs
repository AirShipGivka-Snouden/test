using System;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Tunneling;

internal sealed class TrafficMeterItem : IDisposable
{
	private static readonly TimeSpan MaxThrottleDelay = TimeSpan.FromSeconds(2L);

	private long _total;

	private long _lastTotal;

	private long _windowTotal;

	private DateTime _lastSpeedUpdateTime = FastDateTime.Now;

	private DateTime _windowStartTime = FastDateTime.Now;

	private long _speed;

	private readonly Lock _speedLock = new Lock();

	private readonly SemaphoreSlim _throttleSemaphore = new SemaphoreSlim(1, 1);

	private bool _disposed;

	public required TimeSpan SpeedInterval { get; init; }

	public long MaxSpeed { get; set; }

	public long Traffic => Interlocked.Read(in _total);

	public long Speed
	{
		get
		{
			UpdateSpeed();
			using (_speedLock.EnterScope())
			{
				return _speed;
			}
		}
	}

	public void OnTraffic(long bytes)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		Interlocked.Add(ref _total, bytes);
		Interlocked.Add(ref _windowTotal, bytes);
	}

	public bool ShouldThrottle()
	{
		return GetThrottleDelay() > TimeSpan.Zero;
	}

	public async ValueTask ThrottleAsync(CancellationToken cancellationToken)
	{
		TimeSpan throttleDelay = GetThrottleDelay();
		if (throttleDelay <= TimeSpan.Zero)
		{
			return;
		}
		await _throttleSemaphore.WaitAsync(cancellationToken).Vhc();
		try
		{
			throttleDelay = GetThrottleDelay();
			if (!(throttleDelay <= TimeSpan.Zero))
			{
				await Task.Delay(throttleDelay, cancellationToken).Vhc();
				DateTime now = FastDateTime.Now;
				double totalSeconds = (now - _windowStartTime).TotalSeconds;
				long num = (long)((double)MaxSpeed * totalSeconds);
				long num2 = Interlocked.Read(in _windowTotal);
				long value = Math.Max(0L, num2 - num);
				Interlocked.Exchange(ref _windowTotal, value);
				_windowStartTime = now;
			}
		}
		finally
		{
			_throttleSemaphore.Release();
		}
	}

	private TimeSpan GetThrottleDelay()
	{
		if (_disposed || MaxSpeed == 0L)
		{
			return TimeSpan.Zero;
		}
		double num = (FastDateTime.Now - _windowStartTime).TotalSeconds;
		if (num < 0.1)
		{
			num = 0.1;
		}
		double num2 = (double)Interlocked.Read(in _windowTotal) / (double)MaxSpeed - num;
		if (!(num2 > 0.0))
		{
			return TimeSpan.Zero;
		}
		return TimeSpan.FromSeconds(Math.Min(num2, MaxThrottleDelay.TotalSeconds));
	}

	private void UpdateSpeed()
	{
		using (_speedLock.EnterScope())
		{
			DateTime now = FastDateTime.Now;
			double totalSeconds = (now - _lastSpeedUpdateTime).TotalSeconds;
			if (!(totalSeconds < 1.0))
			{
				long num = Interlocked.Read(in _total);
				_speed = (long)((double)(num - _lastTotal) / totalSeconds);
				_lastSpeedUpdateTime = now;
				_lastTotal = num;
			}
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_throttleSemaphore.Dispose();
		}
	}
}
