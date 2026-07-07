using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services.Interfaces;
using Serilog;
using VpnHood.Core.Client.Abstractions;

namespace Ciphra.VPN.Common.Services;

public class TrafficAggregator : IDisposable
{
	private const int PollIntervalSeconds = 1;

	private const int FlushIntervalSeconds = 30;

	private readonly Settings _settings;

	private readonly IDisposable _pollSub;

	private readonly IDisposable _flushSub;

	private readonly BehaviorSubject<string> _trafficUsageText;

	private readonly BehaviorSubject<string> _resetCountdownText;

	private readonly Subject<Unit> _trafficLimitExceeded = new Subject<Unit>();

	private long _aggregateTrafficUsed;

	private long _lastSeenServerTraffic = -1L;

	private long _maxTraffic;

	private DateTime _cycleExpiration = DateTime.MinValue;

	private bool _wasConnected;

	private bool _limitFired;

	private volatile bool _disposed;

	public IObservable<string> TrafficUsageText => (IObservable<string>)_trafficUsageText;

	public IObservable<string> ResetCountdownText => (IObservable<string>)_resetCountdownText;

	public IObservable<Unit> TrafficLimitExceeded => (IObservable<Unit>)_trafficLimitExceeded;

	public bool IsTrafficLimitExceeded => _maxTraffic > 0 && _aggregateTrafficUsed >= _maxTraffic;

	public TrafficAggregator(IVpnService vpnService, Settings settings)
	{
		TrafficAggregator trafficAggregator = this;
		_settings = settings ?? throw new ArgumentNullException("settings");
		if (vpnService == null)
		{
			throw new ArgumentNullException("vpnService");
		}
		if (settings.LastMaxTraffic > 0)
		{
			_cycleExpiration = settings.TrafficCycleExpiration;
			if (IsCycleExpired())
			{
				settings.AggregateTrafficUsed = 0L;
			}
			else
			{
				_aggregateTrafficUsed = settings.AggregateTrafficUsed;
			}
			_maxTraffic = settings.LastMaxTraffic;
		}
		_trafficUsageText = new BehaviorSubject<string>(FormatUsageText());
		_resetCountdownText = new BehaviorSubject<string>(FormatCountdown());
		_pollSub = ObservableExtensions.Subscribe<ConnectionStateDto>(Observable.Where<ConnectionStateDto>(Observable.Select<long, ConnectionStateDto>(Observable.Interval(TimeSpan.FromSeconds(1L)), (Func<long, ConnectionStateDto>)((long _) => vpnService.CheckConnectionState())), (Func<ConnectionStateDto, bool>)((ConnectionStateDto s) => s != null)), (Action<ConnectionStateDto>)delegate(ConnectionStateDto state)
		{
			trafficAggregator.OnConnectionState(state);
		});
		_flushSub = ObservableExtensions.Subscribe<long>(Observable.Where<long>(Observable.Interval(TimeSpan.FromSeconds(30L)), (Func<long, bool>)((long _) => trafficAggregator._maxTraffic > 0)), (Action<long>)delegate
		{
			trafficAggregator.Flush();
		});
	}

	private void OnConnectionState(ConnectionStateDto state)
	{
		if (_disposed)
		{
			return;
		}
		try
		{
			OnConnectionStateCore(state);
		}
		catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
		{
			Log.Warning(ex, "TrafficAggregator.OnConnectionState persistence failed");
		}
	}

	private void OnConnectionStateCore(ConnectionStateDto state)
	{
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		ClientState? clientState = state.ClientState;
		bool flag;
		if (clientState.HasValue)
		{
			ClientState valueOrDefault = clientState.GetValueOrDefault();
			if ((uint)(valueOrDefault - 5) <= 2u)
			{
				flag = true;
				goto IL_0027;
			}
		}
		flag = false;
		goto IL_0027;
		IL_0027:
		bool flag2 = flag;
		if (_wasConnected && !flag2)
		{
			_lastSeenServerTraffic = -1L;
			Flush();
		}
		_wasConnected = flag2;
		if (state.MaxTraffic > 0)
		{
			if (state.SessionExpirationTime.HasValue)
			{
				DateTime value = state.SessionExpirationTime.Value;
				if (value != _cycleExpiration)
				{
					if (value > _cycleExpiration && _cycleExpiration != DateTime.MinValue)
					{
						_aggregateTrafficUsed = 0L;
						_limitFired = false;
					}
					_cycleExpiration = value;
					_settings.TrafficCycleExpiration = value;
				}
			}
			if (IsCycleExpired())
			{
				_aggregateTrafficUsed = 0L;
				_settings.AggregateTrafficUsed = 0L;
				_limitFired = false;
			}
			long num = state.CycleTrafficSent + state.CycleTrafficReceived;
			if (_lastSeenServerTraffic < 0)
			{
				_lastSeenServerTraffic = num;
			}
			else if (num >= _lastSeenServerTraffic)
			{
				_aggregateTrafficUsed += num - _lastSeenServerTraffic;
				_lastSeenServerTraffic = num;
			}
			else
			{
				_lastSeenServerTraffic = num;
			}
			if (_maxTraffic != state.MaxTraffic)
			{
				_maxTraffic = state.MaxTraffic;
				Flush();
			}
			if (!_limitFired && _aggregateTrafficUsed >= _maxTraffic)
			{
				_limitFired = true;
				Flush();
				((SubjectBase<Unit>)(object)_trafficLimitExceeded).OnNext(Unit.Default);
			}
		}
		else if (_maxTraffic > 0)
		{
			_maxTraffic = 0L;
			_aggregateTrafficUsed = 0L;
			_lastSeenServerTraffic = -1L;
			_limitFired = false;
			_cycleExpiration = DateTime.MinValue;
			_settings.LastMaxTraffic = 0L;
			_settings.AggregateTrafficUsed = 0L;
			_settings.TrafficCycleExpiration = DateTime.MinValue;
		}
		((SubjectBase<string>)(object)_trafficUsageText).OnNext(FormatUsageText());
		((SubjectBase<string>)(object)_resetCountdownText).OnNext(FormatCountdown());
	}

	private void Flush()
	{
		if (_disposed || _maxTraffic <= 0)
		{
			return;
		}
		try
		{
			_settings.LastMaxTraffic = _maxTraffic;
			_settings.AggregateTrafficUsed = _aggregateTrafficUsed;
		}
		catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
		{
			Log.Warning(ex, "TrafficAggregator.Flush failed to persist traffic state");
		}
	}

	private string FormatUsageText()
	{
		return (_maxTraffic > 0) ? (FormatTraffic(_aggregateTrafficUsed) + " / " + FormatTraffic(_maxTraffic)) : string.Empty;
	}

	private string FormatCountdown()
	{
		if (_maxTraffic <= 0 || _cycleExpiration == DateTime.MinValue)
		{
			return string.Empty;
		}
		TimeSpan timeSpan = _cycleExpiration - DateTime.UtcNow;
		if (timeSpan <= TimeSpan.Zero)
		{
			return string.Empty;
		}
		return (timeSpan.TotalHours >= 1.0) ? $"Resets in {(int)timeSpan.TotalHours}h {timeSpan.Minutes:D2}m" : $"Resets in {timeSpan.Minutes}m";
	}

	private bool IsCycleExpired()
	{
		return _cycleExpiration != DateTime.MinValue && DateTime.UtcNow >= _cycleExpiration;
	}

	private static string FormatTraffic(long bytes)
	{
		if (1 == 0)
		{
		}
		string result = ((bytes >= 1000000) ? ((bytes < 1000000000) ? $"{(double)bytes / 1000000.0:0.#} MB" : $"{(double)bytes / 1000000000.0:0.##} GB") : ((bytes < 1000) ? $"{bytes} B" : $"{(double)bytes / 1000.0:0.#} KB"));
		if (1 == 0)
		{
		}
		return result;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_pollSub.Dispose();
			_flushSub.Dispose();
			try
			{
				Flush();
			}
			catch (Exception ex)
			{
				Log.Debug(ex, "TrafficAggregator.Dispose: final flush failed.");
			}
			_disposed = true;
			((SubjectBase<string>)(object)_trafficUsageText).Dispose();
			((SubjectBase<string>)(object)_resetCountdownText).Dispose();
			((SubjectBase<Unit>)(object)_trafficLimitExceeded).Dispose();
		}
	}
}
