using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Proxies.EndPointManagement.Abstractions;
using VpnHood.Core.Proxies.EndPointManagement.Abstractions.Options;
using VpnHood.Core.Toolkit.Jobs;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Monitoring;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Sockets;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Proxies.EndPointManagement;

public class ProxyEndPointManager : IDisposable
{
	private class Data
	{
		public ProxyEndPointInfo[] EndPointInfos { get; init; } = Array.Empty<ProxyEndPointInfo>();

		public long QueuePosition { get; init; }
	}

	private ProxyEndPointEntry? _fastestEntry;

	private long _queuePosition;

	private readonly TimeSpan _serverCheckTimeout;

	private readonly ISocketFactory _socketFactory;

	private ProxyEndPointEntry[] _proxyEndPointEntries;

	private ProgressMonitor? _progressMonitor;

	private readonly string _proxyEndPointInfosFile;

	private Job? _autoUpdateJob;

	private ProxyAutoUpdateOptions _autoUpdateOptions;

	private bool _disposed;

	private bool _verifyTls;

	private readonly DateTime _sessionCreatedTime = FastDateTime.UtcNow;

	private readonly ProxyEndPointStatus _sessionStatus = new ProxyEndPointStatus();

	private const int CheckServerMaxDegreeOfParallelism = 50;

	private readonly AsyncLock _connectLock = new AsyncLock();

	public bool IsEnabled { get; private set; }

	public bool UseRecentSucceeded { get; set; }

	public ProgressStatus? Progress => _progressMonitor?.Progress;

	public TimeSpan? RequestTimeout { get; set; }

	public ProxyEndPointManagerStatus Status
	{
		get
		{
			lock (_sessionStatus)
			{
				return new ProxyEndPointManagerStatus
				{
					AutoUpdate = (_autoUpdateOptions.Interval > TimeSpan.Zero && _autoUpdateOptions.Url != null),
					SessionStatus = _sessionStatus,
					ProxyEndPointInfos = _proxyEndPointEntries.Select((ProxyEndPointEntry x) => x.Info).ToArray(),
					IsAnySucceeded = _proxyEndPointEntries.Any((ProxyEndPointEntry x) => x.Status.ErrorMessage == null),
					SucceededServerCount = _proxyEndPointEntries.Count((ProxyEndPointEntry x) => x.EndPoint.IsEnabled && x.Status.IsLastUsedSucceeded),
					FailedServerCount = _proxyEndPointEntries.Count(delegate(ProxyEndPointEntry x)
					{
						if (x.EndPoint.IsEnabled)
						{
							ProxyEndPointStatus status = x.Status;
							if (status != null && status.HasUsed)
							{
								return !status.IsLastUsedSucceeded;
							}
							return false;
						}
						return false;
					}),
					UnknownServerCount = _proxyEndPointEntries.Count(delegate(ProxyEndPointEntry x)
					{
						if (x.EndPoint.IsEnabled)
						{
							ProxyEndPointStatus status = x.Status;
							if (status != null)
							{
								return !status.HasUsed;
							}
							return false;
						}
						return false;
					}),
					DisabledServerCount = _proxyEndPointEntries.Count((ProxyEndPointEntry x) => !x.EndPoint.IsEnabled)
				};
			}
		}
	}

	public ProxyEndPointManager(ProxyOptions proxyOptions, string storagePath, ISocketFactory socketFactory, TimeSpan? serverCheckTimeout = null)
	{
		_serverCheckTimeout = serverCheckTimeout ?? TimeSpan.FromSeconds(7L);
		_socketFactory = socketFactory;
		_proxyEndPointInfosFile = Path.Combine(storagePath, "proxies.json");
		_autoUpdateOptions = proxyOptions.AutoUpdateOptions;
		_verifyTls = proxyOptions.VerifyTls;
		IsEnabled = proxyOptions.ProxyEndPoints.Any();
		Data data = JsonUtils.TryDeserializeFile<Data>(_proxyEndPointInfosFile) ?? new Data();
		_queuePosition = data.QueuePosition;
		_proxyEndPointEntries = UpdateEntriesByOptions(data.EndPointInfos.Select((ProxyEndPointInfo x) => new ProxyEndPointEntry(x)), proxyOptions).ToArray();
		TimeSpan? interval = _autoUpdateOptions.Interval;
		TimeSpan zero = TimeSpan.Zero;
		if (interval.HasValue && interval.GetValueOrDefault() > zero && _autoUpdateOptions.Url != null)
		{
			StartAutoUpdate(_autoUpdateOptions.Interval.Value);
		}
	}

	public void UpdateOptions(ProxyOptions proxyOptions)
	{
		IsEnabled = proxyOptions.ProxyEndPoints.Any();
		_verifyTls = proxyOptions.VerifyTls;
		_proxyEndPointEntries = UpdateEntriesByOptions(_proxyEndPointEntries, proxyOptions).ToArray();
		_autoUpdateOptions = proxyOptions.AutoUpdateOptions;
		TimeSpan? interval = _autoUpdateOptions.Interval;
		TimeSpan zero = TimeSpan.Zero;
		if (interval.HasValue && interval.GetValueOrDefault() > zero && _autoUpdateOptions.Url != null)
		{
			StartAutoUpdate(_autoUpdateOptions.Interval.Value);
		}
	}

	private void StartAutoUpdate(TimeSpan interval)
	{
		Job? autoUpdateJob = _autoUpdateJob;
		if (autoUpdateJob == null || autoUpdateJob.Interval != interval)
		{
			_autoUpdateJob?.Dispose();
		}
		_autoUpdateJob = new Job(UpdateFromUrlAsync, new JobOptions
		{
			Interval = interval,
			DueTime = TimeSpan.Zero,
			Name = "ProxyEndPointAutoUpdate",
			AutoStart = true
		});
	}

	private async ValueTask UpdateFromUrlAsync(CancellationToken cancellationToken)
	{
		if (_autoUpdateOptions.Url == null)
		{
			return;
		}
		try
		{
			VhLogger.Instance.LogInformation("Downloading proxy list from {Url}...", _autoUpdateOptions.Url);
			using HttpClient httpClient = new HttpClient();
			ProxyEndPointInfo[] currentInfos = _proxyEndPointEntries.Select((ProxyEndPointEntry x) => x.Info).ToArray();
			ProxyEndPoint[] array = ProxyEndPointUpdater.Merge(currentInfos, await ProxyEndPointUpdater.LoadFromUrlAsync(httpClient, _autoUpdateOptions.Url, cancellationToken).Vhc(), _autoUpdateOptions.MaxItemCount, _autoUpdateOptions.MaxPenalty, _autoUpdateOptions.RemoveDuplicateIps);
			if (array.Length == 0)
			{
				VhLogger.Instance.LogWarning("No proxies found in downloaded content from {Url}", _autoUpdateOptions.Url);
				return;
			}
			VhLogger.Instance.LogInformation("Downloaded and merged proxy list. Total proxies: {Count}", array.Length);
			_proxyEndPointEntries = UpdateEntries(_proxyEndPointEntries, array, resetStates: false, keepEnabledState: true).ToArray();
			VhLogger.Instance.LogInformation("Updated proxy list. Total proxies: {Count}", _proxyEndPointEntries.Length);
			SaveNodeInfos();
			await CheckServers(cancellationToken);
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogError(exception, "Failed to update proxy list from {Url}", _autoUpdateOptions.Url);
		}
	}

	private static IEnumerable<ProxyEndPointEntry> UpdateEntriesByOptions(IEnumerable<ProxyEndPointEntry> items, ProxyOptions options)
	{
		items = UpdateEntries(items, options.ProxyEndPoints, options.ResetStates, keepEnabledState: false);
		return items;
	}

	private static IEnumerable<ProxyEndPointEntry> UpdateEntries(IEnumerable<ProxyEndPointEntry> existingEntries, ProxyEndPoint[] newEntries, bool resetStates, bool keepEnabledState)
	{
		Dictionary<string, ProxyEndPointEntry> existingEntryDic = existingEntries.DistinctBy((ProxyEndPointEntry x) => x.EndPoint.Id).ToDictionary((ProxyEndPointEntry x) => x.EndPoint.Id);
		Dictionary<string, ProxyEndPoint> dictionary = newEntries.DistinctBy((ProxyEndPoint x) => x.Id).ToDictionary((ProxyEndPoint x) => x.Id);
		foreach (ProxyEndPointEntry value2 in existingEntryDic.Values)
		{
			ProxyEndPoint valueOrDefault = dictionary.GetValueOrDefault(value2.EndPoint.Id);
			ProxyEndPoint endPoint = value2.Info.EndPoint;
			if (valueOrDefault == null)
			{
				existingEntryDic.Remove(value2.EndPoint.Id);
				continue;
			}
			if (resetStates)
			{
				value2.Info.Status = new ProxyEndPointStatus();
			}
			value2.Info.EndPoint = valueOrDefault;
			if (keepEnabledState)
			{
				value2.Info.EndPoint.IsEnabled = endPoint.IsEnabled;
			}
		}
		foreach (ProxyEndPoint item in newEntries.Where((ProxyEndPoint x) => !existingEntryDic.ContainsKey(x.Id)))
		{
			ProxyEndPointEntry value = new ProxyEndPointEntry(new ProxyEndPointInfo
			{
				EndPoint = item,
				Status = new ProxyEndPointStatus()
			});
			existingEntryDic.Add(item.Id, value);
		}
		return existingEntryDic.Select<KeyValuePair<string, ProxyEndPointEntry>, ProxyEndPointEntry>((KeyValuePair<string, ProxyEndPointEntry> x) => x.Value);
	}

	private async Task<TimeSpan> CheckConnectionAsync(IProxyClient proxyClient, TcpClient tcpClient, ProgressMonitor? progressMonitor, CancellationToken cancellationToken)
	{
		using CancellationTokenSource serverCheckCts = new CancellationTokenSource(_serverCheckTimeout);
		_ = 1;
		try
		{
			IPEndPoint destination = IPEndPoint.Parse("1.1.1.1:443");
			long tickCount = Environment.TickCount64;
			using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, serverCheckCts.Token);
			await proxyClient.ConnectAsync(tcpClient, destination, linkedCts.Token).Vhc();
			TimeSpan latency = TimeSpan.FromMilliseconds(Environment.TickCount64 - tickCount);
			if (_verifyTls)
			{
				await new SslStream(tcpClient.GetStream()).AuthenticateAsClientAsync(new SslClientAuthenticationOptions
				{
					TargetHost = "one.one.one.one"
				}, linkedCts.Token).Vhc();
			}
			return latency;
		}
		catch (OperationCanceledException innerException) when (serverCheckCts.IsCancellationRequested)
		{
			throw new TimeoutException("Connection check timed out.", innerException);
		}
		catch (AuthenticationException)
		{
			throw new ProxyClientException(SocketError.AccessDenied, "Verification of TLS connection failed.");
		}
		finally
		{
			progressMonitor?.IncrementCompleted();
		}
	}

	public Task CheckServers(CancellationToken cancellationToken)
	{
		ProxyEndPointEntry[] endpoints = (from x in _proxyEndPointEntries
			where x.EndPoint.IsEnabled
			orderby x.Status.Quality
			select x).ToArray();
		return CheckServers(endpoints, 10, cancellationToken);
	}

	private async Task CheckServers(IEnumerable<ProxyEndPointEntry> endpoints, int satisfiedSuccessCount, CancellationToken cancellationToken)
	{
		VhLogger.Instance.LogDebug("Checking proxy servers for reachability...");
		_progressMonitor = new ProgressMonitor(_proxyEndPointEntries.Length, _serverCheckTimeout, 50);
		try
		{
			ConcurrentBag<(ProxyEndPointEntry Entry, TcpClient? Client, TimeSpan? Latency, Exception? Error)> results = new ConcurrentBag<(ProxyEndPointEntry, TcpClient, TimeSpan?, Exception)>();
			ParallelOptions parallelOptions = new ParallelOptions
			{
				CancellationToken = cancellationToken,
				MaxDegreeOfParallelism = 50
			};
			int successCount = 0;
			await Parallel.ForEachAsync(endpoints, parallelOptions, async delegate(ProxyEndPointEntry entry, CancellationToken ct)
			{
				TcpClient tcpClient2 = null;
				try
				{
					if (successCount < satisfiedSuccessCount || entry.Status.Quality == StatusQuality.Unknown)
					{
						IProxyClient proxyClient = await ProxyClientFactory.CreateProxyClient(entry.EndPoint, ct).Vhc();
						tcpClient2 = _socketFactory.CreateTcpClient(proxyClient.ProxyEndPoint);
						tcpClient2.ReceiveBufferSize = 4096;
						tcpClient2.SendBufferSize = 4096;
						TimeSpan value = await CheckConnectionAsync(proxyClient, tcpClient2, _progressMonitor, ct).Vhc();
						results.Add((entry, tcpClient2, value, null));
						Interlocked.Increment(ref successCount);
					}
				}
				catch (Exception item)
				{
					results.Add((entry, tcpClient2, null, item));
				}
			}).Vhc();
			(ProxyEndPointEntry, TcpClient, TimeSpan?, Exception)[] source = (from x in results
				where x.Error == null && x.Entry.EndPoint.IsEnabled && x.Latency.HasValue
				orderby x.Latency.Value
				select x).ToArray();
			TimeSpan? fastestLatency = (source.Any() ? source.First().Item3 : ((TimeSpan?)null));
			foreach (var (proxyEndPointEntry, tcpClient, timeSpan, ex) in results)
			{
				tcpClient?.Dispose();
				if (ex == null)
				{
					RecordSuccess(proxyEndPointEntry, timeSpan.Value, fastestLatency, checkMode: true);
					continue;
				}
				if (!cancellationToken.IsCancellationRequested)
				{
					RecordFailed(proxyEndPointEntry, ex, checkMode: true);
				}
				if ((ex is ProxyClientException { SocketErrorCode: var socketErrorCode } && (socketErrorCode == SocketError.AccessDenied || socketErrorCode == SocketError.ProtocolNotSupported)) ? true : false)
				{
					proxyEndPointEntry.EndPoint.IsEnabled = false;
				}
			}
			cancellationToken.ThrowIfCancellationRequested();
		}
		finally
		{
			_progressMonitor = null;
		}
	}

	private ProxyEndPointEntry[] GetOrderedEntriesQuery(int maxPriorityFailed = 1)
	{
		ProxyEndPointEntry[] array = (from x in _proxyEndPointEntries
			where x.EndPoint.IsEnabled
			where !UseRecentSucceeded || x.Status.LastSucceeded >= _sessionCreatedTime
			orderby x.GetSortValue(_queuePosition), x.Status.LastUsed
			select x).ToArray();
		if (!array.Any((ProxyEndPointEntry x) => x.Status.IsLastUsedSucceeded))
		{
			return array;
		}
		List<ProxyEndPointEntry> list = new List<ProxyEndPointEntry>();
		List<ProxyEndPointEntry> list2 = new List<ProxyEndPointEntry>(array.Length);
		int num = 0;
		ProxyEndPointEntry[] array2 = array;
		foreach (ProxyEndPointEntry proxyEndPointEntry in array2)
		{
			if (proxyEndPointEntry.Status.IsLastUsedSucceeded)
			{
				list2.Add(proxyEndPointEntry);
				continue;
			}
			num++;
			if (num <= maxPriorityFailed)
			{
				list2.Add(proxyEndPointEntry);
			}
			else
			{
				list.Add(proxyEndPointEntry);
			}
		}
		list2.AddRange(list);
		return list2.ToArray();
	}

	private async Task<ProxyEndPointEntry[]> GetOrderedEntries(CancellationToken cancellationToken)
	{
		using (await _connectLock.LockAsync(cancellationToken))
		{
			Interlocked.Increment(ref _queuePosition);
			ProxyEndPointEntry[] array = GetOrderedEntriesQuery().ToArray();
			if (array.FirstOrDefault()?.Status.IsLastUsedSucceeded ?? false)
			{
				return array;
			}
			await CheckServers(array, 1, cancellationToken);
			return GetOrderedEntriesQuery().ToArray();
		}
	}

	public async Task<TcpClient> ConnectAsync(IPEndPoint ipEndPoint, Action? onAttempt, CancellationToken cancellationToken)
	{
		ProxyEndPointEntry[] entries = await GetOrderedEntries(cancellationToken);
		ProxyEndPointEntry[] array = entries;
		foreach (ProxyEndPointEntry entry in array)
		{
			long tickCount = Environment.TickCount64;
			TcpClient tcpClient = null;
			cancellationToken.ThrowIfCancellationRequested();
			ObjectDisposedException.ThrowIf(_disposed, this);
			try
			{
				VhLogger.Instance.LogDebug("Connecting via a {ProxyType} proxy server {ProxyServer}...", entry.EndPoint.Protocol, VhLogger.FormatHostName(entry.EndPoint.Host));
				IProxyClient proxyClient = await ProxyClientFactory.CreateProxyClient(entry.EndPoint, cancellationToken).Vhc();
				tcpClient = _socketFactory.CreateTcpClient(proxyClient.ProxyEndPoint);
				await proxyClient.ConnectAsync(tcpClient, ipEndPoint, cancellationToken).Vhc();
				TimeSpan timeSpan = TimeSpan.FromMilliseconds(Environment.TickCount64 - tickCount);
				if (entries.Contains<ProxyEndPointEntry>(_fastestEntry, null))
				{
					TimeSpan value = timeSpan;
					TimeSpan? obj = _fastestEntry?.Status.Latency;
					if (!(value < obj))
					{
						goto IL_02d7;
					}
				}
				_fastestEntry = entry;
				goto IL_02d7;
				IL_02d7:
				RecordSuccess(entry, timeSpan, _fastestEntry?.Status.Latency, checkMode: false);
				onAttempt?.Invoke();
				if (_autoUpdateOptions.RemoveDuplicateIps)
				{
					foreach (ProxyEndPointEntry item in entries.Where((ProxyEndPointEntry x) => x.EndPoint.Protocol == entry.EndPoint.Protocol && x.EndPoint.Host.Equals(entry.EndPoint.Host, StringComparison.OrdinalIgnoreCase) && x.EndPoint.Id != entry.EndPoint.Id))
					{
						item.EndPoint.IsEnabled = false;
						Uri value2 = entry.EndPoint.BuildUrlWithoutPassword();
						item.Status.ErrorMessage = $"Duplicate IP disabled in favour of {value2}";
					}
				}
				return tcpClient;
			}
			catch (Exception ex)
			{
				TimeSpan timeSpan2 = TimeSpan.FromMilliseconds(Environment.TickCount64 - tickCount);
				VhLogger.Instance.LogError(ex, "Failed to connect to {ProxyType} proxy server {ProxyServer}. Delay: {delay}", entry.EndPoint.Protocol, VhLogger.FormatHostName(entry.EndPoint.Host), timeSpan2);
				if (!(ex is OperationCanceledException) || !(timeSpan2 < _serverCheckTimeout))
				{
					RecordFailed(entry, ex, checkMode: false);
					onAttempt?.Invoke();
				}
				tcpClient?.Dispose();
			}
		}
		throw new SocketException(10051);
	}

	private void RecordSuccess(ProxyEndPointEntry entry, TimeSpan? latency, TimeSpan? fastestLatency, bool checkMode)
	{
		entry.RecordSuccess(latency.Value, fastestLatency, _queuePosition);
		if (!checkMode)
		{
			lock (_sessionStatus)
			{
				_sessionStatus.SucceededCount++;
				_sessionStatus.LastSucceeded = DateTime.UtcNow;
				_sessionStatus.QueuePosition = _queuePosition;
				_sessionStatus.Latency = entry.Status.Latency;
				_sessionStatus.Penalty = entry.Status.Penalty;
				_sessionStatus.ErrorMessage = null;
			}
		}
	}

	private void RecordFailed(ProxyEndPointEntry entry, Exception error, bool checkMode)
	{
		entry.RecordFailed(error, _queuePosition);
		if (!checkMode)
		{
			lock (_sessionStatus)
			{
				_sessionStatus.FailedCount++;
				_sessionStatus.LastFailed = DateTime.UtcNow;
				_sessionStatus.QueuePosition = _queuePosition;
				_sessionStatus.Latency = null;
				_sessionStatus.Penalty = entry.Status.Penalty;
				_sessionStatus.ErrorMessage = entry.Status.ErrorMessage;
			}
		}
	}

	public void RecordFailed(TcpClient tcpClient, Exception ex)
	{
		IPEndPoint remoteEndPoint = tcpClient.TryGetRemoteEndPoint();
		ProxyEndPointEntry proxyEndPointEntry = _proxyEndPointEntries.FirstOrDefault((ProxyEndPointEntry x) => x.IpEndPoint?.Equals(remoteEndPoint) ?? false);
		if (proxyEndPointEntry != null)
		{
			proxyEndPointEntry.Status.SucceededCount--;
			RecordFailed(proxyEndPointEntry, ex, checkMode: false);
		}
	}

	private void SaveNodeInfos()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_proxyEndPointInfosFile));
		Data value = new Data
		{
			QueuePosition = _queuePosition,
			EndPointInfos = Status.ProxyEndPointInfos
		};
		File.WriteAllText(_proxyEndPointInfosFile, JsonSerializer.Serialize(value));
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_autoUpdateJob?.Dispose();
			VhUtils.TryInvoke("Save ProxyEndPoints status", SaveNodeInfos);
		}
	}
}
