using System;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Tunneling;

public class TrafficMeter : IDisposable
{
	private readonly TrafficMeterItem _sent;

	private readonly TrafficMeterItem _received;

	private bool _disposed;

	public TimeSpan SpeedInterval { get; init; } = TimeSpan.FromSeconds(2L);

	public Traffic Traffic => new Traffic(_sent.Traffic, _received.Traffic);

	public DateTime LastActivityTime { get; private set; } = FastDateTime.Now;

	public Traffic MaxSpeed
	{
		get
		{
			return new Traffic(_sent.MaxSpeed, _received.MaxSpeed);
		}
		set
		{
			_sent.MaxSpeed = value.Sent;
			_received.MaxSpeed = value.Received;
		}
	}

	public Traffic Speed => new Traffic(_sent.Speed, _received.Speed);

	public TrafficMeter()
	{
		_sent = new TrafficMeterItem
		{
			SpeedInterval = SpeedInterval
		};
		_received = new TrafficMeterItem
		{
			SpeedInterval = SpeedInterval
		};
	}

	public void OnSent(long bytes)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_sent.OnTraffic(bytes);
		LastActivityTime = FastDateTime.Now;
	}

	public void OnReceived(long bytes)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_received.OnTraffic(bytes);
		LastActivityTime = FastDateTime.Now;
	}

	public bool ShouldThrottleSend()
	{
		return _sent.ShouldThrottle();
	}

	public bool ShouldThrottleReceive()
	{
		return _received.ShouldThrottle();
	}

	public ValueTask ThrottleSendAsync(CancellationToken cancellationToken)
	{
		return _sent.ThrottleAsync(cancellationToken);
	}

	public ValueTask ThrottleReceiveAsync(CancellationToken cancellationToken)
	{
		return _received.ThrottleAsync(cancellationToken);
	}

	public bool ShouldThrottle()
	{
		if (!ShouldThrottleSend())
		{
			return ShouldThrottleReceive();
		}
		return true;
	}

	public async ValueTask ThrottleAsync(CancellationToken cancellationToken)
	{
		await ThrottleSendAsync(cancellationToken);
		await ThrottleReceiveAsync(cancellationToken);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_sent.Dispose();
			_received.Dispose();
		}
	}
}
