using System;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Exceptions;
using Ciphra.VPN.Common.Models;
using Serilog;
using VpnHood.Core.Common.Messaging;

namespace Ciphra.VPN.Common.Services;

internal sealed class MidSessionSmartHealCoordinator : IDisposable
{
	private static readonly TimeSpan BaseReconnectBackoff = TimeSpan.FromSeconds(10L);

	private static readonly TimeSpan MaxReconnectBackoff = TimeSpan.FromSeconds(300L);

	private const int ReconnectFailureThresholdForBlockedDialog = 3;

	public const long ProductiveBytesThreshold = 102400L;

	public const long ShortSessionThresholdMs = 30000L;

	private readonly IVpnServiceKeepAliveBridge _bridge;

	private readonly Settings _settings;

	private readonly IAppAnalytics? _analytics;

	private readonly TimeProvider _timeProvider;

	private readonly Subject<Unit> _blockedNetworkDetectedSubject = new Subject<Unit>();

	private readonly Subject<SmartHealCandidate> _smartHealSucceededSubject = new Subject<SmartHealCandidate>();

	private readonly BehaviorSubject<bool> _isSmartHealInProgressSubject = new BehaviorSubject<bool>(false);

	private int _consecutiveReconnectFailures;

	private bool _smartHealAttemptedThisSession;

	private DateTime? _connectedSinceUtc;

	private bool _blockedNetworkSignalEmittedThisOutage;

	private DateTime? _firstReconnectFailureAtUtc;

	private DateTime? _instabilityBaselineAtUtc;

	private int _unstableCountBaseline;

	private int _waitingCountBaseline;

	private Task _reconnectBackoffTask = Task.CompletedTask;

	public IObservable<Unit> BlockedNetworkDetected => (IObservable<Unit>)_blockedNetworkDetectedSubject;

	public IObservable<SmartHealCandidate> SmartHealSucceeded => (IObservable<SmartHealCandidate>)_smartHealSucceededSubject;

	public IObservable<bool> IsSmartHealInProgress => (IObservable<bool>)_isSmartHealInProgressSubject;

	public MidSessionSmartHealCoordinator(IVpnServiceKeepAliveBridge bridge, Settings settings, IAppAnalytics? analytics, TimeProvider? timeProvider = null)
	{
		_bridge = bridge ?? throw new ArgumentNullException("bridge");
		_settings = settings ?? throw new ArgumentNullException("settings");
		_analytics = analytics;
		_timeProvider = timeProvider ?? TimeProvider.System;
	}

	internal void SetSmartHealInProgress(bool value)
	{
		((SubjectBase<bool>)(object)_isSmartHealInProgressSubject).OnNext(value);
	}

	public void EmitSmartHealSucceeded(SmartHealCandidate winner)
	{
		((SubjectBase<SmartHealCandidate>)(object)_smartHealSucceededSubject).OnNext(winner);
	}

	public void OnSuccessfulConnect()
	{
		OnNewSessionEstablished(default(NewSessionInfo));
	}

	public void OnNewSessionEstablished(NewSessionInfo info)
	{
		bool flag = info.PriorSessionExisted && info.PriorSessionDurationMs < 30000 && info.PriorSessionTotalBytes < 102400;
		bool flag2 = info.NewSessionSuppressedTo == SessionSuppressType.YourSelf;
		if (flag2 && flag)
		{
			_consecutiveReconnectFailures++;
			DateTime valueOrDefault = _firstReconnectFailureAtUtc.GetValueOrDefault();
			if (!_firstReconnectFailureAtUtc.HasValue)
			{
				valueOrDefault = _timeProvider.GetUtcNow().UtcDateTime;
				_firstReconnectFailureAtUtc = valueOrDefault;
			}
			Log.Warning<int, long, long>("Keep-Alive: A5 self-suppression failure detected (consecutive: {N}). Prior session: {Ms}ms, {Bytes} bytes. SmartHeal will fire when outage age reaches threshold.", _consecutiveReconnectFailures, info.PriorSessionDurationMs, info.PriorSessionTotalBytes);
			_analytics?.SendEvent("keepalive_a5_self_suppression", ("consecutive_failures", _consecutiveReconnectFailures.ToString()), ("prior_duration_ms", info.PriorSessionDurationMs.ToString()), ("prior_total_bytes", info.PriorSessionTotalBytes.ToString()));
			ResetSessionBaseline();
		}
		else
		{
			ResetOutageState();
		}
	}

	public async Task ProcessTickAsync(VpnServerDto savedServer, CancellationToken ct)
	{
		if (savedServer == null)
		{
			throw new ArgumentNullException("savedServer");
		}
		HealthSnapshot snap = await _bridge.SampleHealthAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		UpdateConnectedSince(snap);
		var (unstableDelta, waitingDelta) = UpdateInstabilityBaseline(snap);
		if (snap.NeedsReconnect)
		{
			if (_reconnectBackoffTask.IsCompleted)
			{
				await HandleReconnectAsync(savedServer, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		else if (snap.IsClientConnected)
		{
			DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
			SmartHealTriggerInput policyInput = new SmartHealTriggerInput(TimeSinceConnected: _connectedSinceUtc.HasValue ? new TimeSpan?(now - _connectedSinceUtc.Value) : ((TimeSpan?)null), ConsecutiveReconnectFailures: _consecutiveReconnectFailures, TimeSinceFirstReconnectFailure: _firstReconnectFailureAtUtc.HasValue ? new TimeSpan?(now - _firstReconnectFailureAtUtc.Value) : ((TimeSpan?)null), IsClientConnected: true, BytesSent: snap.BytesSent, BytesReceived: snap.BytesReceived, UnstableCountInWindow: unstableDelta, WaitingCountInWindow: waitingDelta, SmartHealAlreadyAttemptedThisSession: _smartHealAttemptedThisSession, SmartHealEnabled: _settings.IsSmartHealEnabled);
			SmartHealTriggerReason reason = SmartHealTriggerPolicy.Evaluate(policyInput);
			bool flag = (uint)(reason - 2) <= 1u;
			if (flag && !_smartHealAttemptedThisSession)
			{
				await RunMidSessionSmartHealAsync(savedServer, reason, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	private void UpdateConnectedSince(HealthSnapshot snap)
	{
		if (snap.NeedsReconnect || !snap.IsClientConnected)
		{
			_connectedSinceUtc = null;
			return;
		}
		DateTime valueOrDefault = _connectedSinceUtc.GetValueOrDefault();
		if (!_connectedSinceUtc.HasValue)
		{
			valueOrDefault = _timeProvider.GetUtcNow().UtcDateTime;
			_connectedSinceUtc = valueOrDefault;
		}
	}

	private (int unstableDelta, int waitingDelta) UpdateInstabilityBaseline(HealthSnapshot snap)
	{
		if (!snap.IsClientConnected)
		{
			return (unstableDelta: 0, waitingDelta: 0);
		}
		DateTime utcDateTime = _timeProvider.GetUtcNow().UtcDateTime;
		DateTime? instabilityBaselineAtUtc = _instabilityBaselineAtUtc;
		if (!instabilityBaselineAtUtc.HasValue || utcDateTime - _instabilityBaselineAtUtc.Value >= SmartHealTriggerPolicy.InstabilityWindow)
		{
			_instabilityBaselineAtUtc = utcDateTime;
			_unstableCountBaseline = snap.UnstableCountCumulative;
			_waitingCountBaseline = snap.WaitingCountCumulative;
		}
		int item = Math.Max(0, snap.UnstableCountCumulative - _unstableCountBaseline);
		int item2 = Math.Max(0, snap.WaitingCountCumulative - _waitingCountBaseline);
		return (unstableDelta: item, waitingDelta: item2);
	}

	private async Task HandleReconnectAsync(VpnServerDto savedServer, CancellationToken ct)
	{
		Log.Information<int>("Keep-Alive: Triggering reconnection (consecutive failures so far: {N}).", _consecutiveReconnectFailures);
		VpnServerDto serverToReconnect = savedServer;
		VpnServerDto refreshed = await _bridge.RefreshAccessKeyAsync(serverToReconnect, ct).ConfigureAwait(continueOnCapturedContext: false);
		if (refreshed != null)
		{
			serverToReconnect = refreshed;
			Log.Information<string>("Keep-Alive: Refreshed AccessKey for server {ServerId}.", refreshed.Id);
		}
		Exception lastReconnectError;
		try
		{
			await _bridge.ReconnectAsync(serverToReconnect, null, ct).ConfigureAwait(continueOnCapturedContext: false);
			Log.Information("Keep-Alive: Reconnected successfully.");
			return;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			lastReconnectError = ex2;
			_consecutiveReconnectFailures++;
			DateTime nowStamp = _timeProvider.GetUtcNow().UtcDateTime;
			_firstReconnectFailureAtUtc.GetValueOrDefault();
			if (!_firstReconnectFailureAtUtc.HasValue)
			{
				_firstReconnectFailureAtUtc = nowStamp;
			}
			Log.Error<int>(ex2, "Keep-Alive: Reconnection attempt failed (consecutive failures: {N}).", _consecutiveReconnectFailures);
		}
		TimeSpan outageAge = (_firstReconnectFailureAtUtc.HasValue ? (_timeProvider.GetUtcNow().UtcDateTime - _firstReconnectFailureAtUtc.Value) : TimeSpan.Zero);
		if (_consecutiveReconnectFailures >= 2 && outageAge >= SmartHealTriggerPolicy.MinOutageAgeForSignal3 && !_smartHealAttemptedThisSession && _settings.IsSmartHealEnabled && lastReconnectError is VpnConnectionException vcex && !vcex.IsUnrecoverable() && !lastReconnectError.IsSmartHealNowSentinel() && !ct.IsCancellationRequested)
		{
			await RunMidSessionSmartHealAsync(serverToReconnect, SmartHealTriggerReason.ConsecutiveReconnectFailures, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (_consecutiveReconnectFailures == 0)
			{
				return;
			}
		}
		if (_smartHealAttemptedThisSession && !_blockedNetworkSignalEmittedThisOutage && _consecutiveReconnectFailures >= 3)
		{
			_blockedNetworkSignalEmittedThisOutage = true;
			_analytics?.SendEvent("keepalive_blocked_network_detected", ("consecutive_failures", _consecutiveReconnectFailures.ToString()));
			((SubjectBase<Unit>)(object)_blockedNetworkDetectedSubject).OnNext(Unit.Default);
		}
		TimeSpan backoff = ReconnectBackoffCalculator.Calculate(_consecutiveReconnectFailures, BaseReconnectBackoff, MaxReconnectBackoff);
		Log.Information<double>("Keep-Alive: Waiting {Delay}s before next reconnect attempt.", backoff.TotalSeconds);
		_reconnectBackoffTask = Task.Delay(backoff, _timeProvider, ct);
	}

	private async Task RunMidSessionSmartHealAsync(VpnServerDto server, SmartHealTriggerReason reason, CancellationToken ct)
	{
		_smartHealAttemptedThisSession = true;
		Log.Warning<SmartHealTriggerReason, int>("Keep-Alive: Invoking mid-session SmartHeal. Reason={Reason}, failures={N}.", reason, _consecutiveReconnectFailures);
		_analytics?.SendEvent("keepalive_smartheal_triggered", ("reason", reason.ToString()), ("failure_count", _consecutiveReconnectFailures.ToString()), ("original_protocol", _settings.ChannelProtocol.ToString()));
		((SubjectBase<bool>)(object)_isSmartHealInProgressSubject).OnNext(true);
		try
		{
			ConnectionSettings original = new ConnectionSettings(_settings.DropQuic, _settings.DropUdp, _settings.ChannelProtocol);
			SmartHealCandidate winner = await _bridge.TrySmartHealAsync(server, original, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (winner != null)
			{
				ResetOutageState();
				((SubjectBase<SmartHealCandidate>)(object)_smartHealSucceededSubject).OnNext(winner);
				_analytics?.SendEvent("keepalive_smartheal_result", ("success", "true"), ("winning_settings", winner.Settings.ToString()));
				Log.Information<ConnectionSettings>("Keep-Alive: SmartHeal found working config: {Settings}. Continuing session.", winner.Settings);
			}
			else
			{
				_analytics?.SendEvent("keepalive_smartheal_result", ("success", "false"), ("winning_settings", "none"));
				Log.Warning("Keep-Alive: SmartHeal exhausted all candidates without success.");
			}
		}
		finally
		{
			((SubjectBase<bool>)(object)_isSmartHealInProgressSubject).OnNext(false);
		}
	}

	private void ResetOutageState()
	{
		ResetOutageCounters();
		ResetSessionBaseline();
	}

	private void ResetOutageCounters()
	{
		_consecutiveReconnectFailures = 0;
		_smartHealAttemptedThisSession = false;
		_blockedNetworkSignalEmittedThisOutage = false;
		_firstReconnectFailureAtUtc = null;
	}

	private void ResetSessionBaseline()
	{
		_connectedSinceUtc = null;
		_instabilityBaselineAtUtc = null;
		_unstableCountBaseline = 0;
		_waitingCountBaseline = 0;
		_reconnectBackoffTask = Task.CompletedTask;
	}

	public void Dispose()
	{
		((SubjectBase<Unit>)(object)_blockedNetworkDetectedSubject).OnCompleted();
		((SubjectBase<Unit>)(object)_blockedNetworkDetectedSubject).Dispose();
		((SubjectBase<SmartHealCandidate>)(object)_smartHealSucceededSubject).OnCompleted();
		((SubjectBase<SmartHealCandidate>)(object)_smartHealSucceededSubject).Dispose();
		((SubjectBase<bool>)(object)_isSmartHealInProgressSubject).OnCompleted();
		((SubjectBase<bool>)(object)_isSmartHealInProgressSubject).Dispose();
	}
}
