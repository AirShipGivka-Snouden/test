using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public class RelayDomainManager
{
	public const string PrimaryDomain = "https://api.ciphravpn.com";

	private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(5L);

	private readonly Settings _settings;

	private readonly BlockchainRelayResolver _resolver;

	private readonly HttpClient? _healthCheckClient;

	private readonly HttpClient? _connectivityCheckClient;

	private readonly SemaphoreSlim _fallbackLock = new SemaphoreSlim(1, 1);

	private readonly List<RelayFailureEntry> _failureLog = new List<RelayFailureEntry>();

	private readonly object _failureLogLock = new object();

	private volatile string _currentDomain;

	private int _requestsServedOnCurrentDomain;

	private DateTime _currentDomainActivatedAt;

	public string CurrentDomain => _currentDomain;

	public bool IsUsingRelay => _currentDomain != "https://api.ciphravpn.com";

	public IAppAnalytics? Analytics { get; set; }

	public RelayDomainManager(Settings settings, BlockchainRelayResolver resolver, HttpClient? healthCheckClient = null, HttpClient? connectivityCheckClient = null)
	{
		_settings = settings ?? throw new ArgumentNullException("settings");
		_resolver = resolver ?? throw new ArgumentNullException("resolver");
		_healthCheckClient = healthCheckClient;
		_connectivityCheckClient = connectivityCheckClient;
		string relayDomain = settings.RelayDomain;
		_currentDomain = ((!string.IsNullOrWhiteSpace(relayDomain)) ? relayDomain : "https://api.ciphravpn.com");
		_currentDomainActivatedAt = DateTime.UtcNow;
		_requestsServedOnCurrentDomain = 0;
		Log.Information<string>("RelayDomainManager initialized. CurrentDomain={Domain}", _currentDomain);
	}

	public void ConfirmDomainWorking(string domain)
	{
		if (domain == _currentDomain)
		{
			Interlocked.Increment(ref _requestsServedOnCurrentDomain);
		}
	}

	public async Task<string?> HandleDomainFailureAsync(string failureReason, CancellationToken ct)
	{
		string failedDomain = _currentDomain;
		await _fallbackLock.WaitAsync(ct);
		try
		{
			if (_currentDomain != failedDomain)
			{
				Log.Information<string>("Domain already changed to {Domain} by concurrent fallback.", _currentDomain);
				return _currentDomain;
			}
			return await HandleDomainFailureCoreAsync(failureReason, ct);
		}
		finally
		{
			_fallbackLock.Release();
		}
	}

	public List<RelayFailureEntry> DrainFailureLog()
	{
		List<RelayFailureEntry> result;
		lock (_failureLogLock)
		{
			List<RelayFailureEntry> list = _settings.RelayFailureLog ?? new List<RelayFailureEntry>();
			Dictionary<(string, string), RelayFailureEntry> dictionary = new Dictionary<(string, string), RelayFailureEntry>();
			foreach (RelayFailureEntry item in list)
			{
				dictionary[(item.Domain, item.FailedAt.ToString("o"))] = item;
			}
			foreach (RelayFailureEntry item2 in _failureLog)
			{
				dictionary[(item2.Domain, item2.FailedAt.ToString("o"))] = item2;
			}
			result = dictionary.Values.ToList();
			_failureLog.Clear();
		}
		_settings.RelayFailureLog = new List<RelayFailureEntry>();
		return result;
	}

	private async Task<string?> HandleDomainFailureCoreAsync(string failureReason, CancellationToken ct)
	{
		Log.Warning<string, string>("Domain failure on {Domain}: {Reason}. Starting fallback.", _currentDomain, failureReason);
		if (!(await ConnectivityChecker.IsInternetAvailableAsync(ct, _connectivityCheckClient)))
		{
			Log.Information("Internet appears to be down. No fallback attempted.");
			return null;
		}
		if (IsUsingRelay)
		{
			string failedRelay = _currentDomain;
			TimeSpan timeInUse = DateTime.UtcNow - _currentDomainActivatedAt;
			int requestsServed = _requestsServedOnCurrentDomain;
			Log.Information<string, int, TimeSpan>("Relay {Relay} failed after {Requests} requests over {Duration}.", failedRelay, requestsServed, timeInUse);
			if (await IsHealthyAsync("https://api.ciphravpn.com", ct))
			{
				Log.Information("Primary domain is reachable. Switching back.");
				RelayFailureEntry entry = new RelayFailureEntry(failedRelay, DateTime.UtcNow, failureReason, requestsServed, timeInUse, "primary");
				AppendFailureEntry(entry);
				SwitchToDomain("https://api.ciphravpn.com", clearRelay: true);
				return "https://api.ciphravpn.com";
			}
			RelayFailureEntry pendingEntry = new RelayFailureEntry(failedRelay, DateTime.UtcNow, failureReason, requestsServed, timeInUse, "pending");
			AppendFailureEntry(pendingEntry);
		}
		List<string> endpoints;
		try
		{
			endpoints = await _resolver.GetRelayEndpointsAsync(ct);
			TrackBlockchainResolution(success: true, endpoints.Count, failureReason);
		}
		catch (Exception ex) when (!(ex is OperationCanceledException) || !ct.IsCancellationRequested)
		{
			Log.Warning<string>(ex, "Blockchain relay resolution failed: {Message}", ex.Message);
			TrackBlockchainResolution(success: false, 0, failureReason, ex.Message);
			UpdateLastPendingEntryOutcome("failed");
			return null;
		}
		foreach (string endpoint in endpoints)
		{
			if (!IsValidRelayEndpoint(endpoint))
			{
				Log.Warning<string>("Skipping invalid relay endpoint from blockchain: {Endpoint}", endpoint);
			}
			else if (await IsHealthyAsync(endpoint, ct))
			{
				Log.Information<string>("Relay endpoint {Endpoint} is healthy. Switching.", endpoint);
				SwitchToDomain(endpoint, clearRelay: false);
				_settings.RelayDomain = endpoint;
				UpdateLastPendingEntryOutcome("blockchain");
				TrackFallbackActivated(endpoint, failureReason);
				return endpoint;
			}
		}
		Log.Warning<int>("All {Count} relay endpoints failed health check.", endpoints.Count);
		UpdateLastPendingEntryOutcome("failed");
		return null;
	}

	private async Task<bool> IsHealthyAsync(string domainOrUrl, CancellationToken ct)
	{
		try
		{
			string url = NormalizeUrl(domainOrUrl);
			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(HealthCheckTimeout);
			HttpClient client = _healthCheckClient ?? new HttpClient();
			bool disposeClient = _healthCheckClient == null;
			try
			{
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, url);
				return (await client.SendAsync(request, cts.Token)).IsSuccessStatusCode;
			}
			finally
			{
				if (disposeClient)
				{
					client.Dispose();
				}
			}
		}
		catch (Exception ex) when (!(ex is OperationCanceledException) || !ct.IsCancellationRequested)
		{
			Log.Debug<string, string>(ex, "Health check failed for {Domain}: {Message}", domainOrUrl, ex.Message);
			return false;
		}
	}

	private static bool IsValidRelayEndpoint(string endpoint)
	{
		if (string.IsNullOrWhiteSpace(endpoint) || endpoint.Length > 500)
		{
			return false;
		}
		string uriString = NormalizeUrl(endpoint);
		if (!Uri.TryCreate(uriString, UriKind.Absolute, out Uri result))
		{
			return false;
		}
		if (!result.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (result.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (IPAddress.TryParse(result.Host, out IPAddress address))
		{
			if (IPAddress.IsLoopback(address))
			{
				return false;
			}
			byte[] addressBytes = address.GetAddressBytes();
			if (addressBytes.Length == 4)
			{
				if (addressBytes[0] == 10 || (addressBytes[0] == 172 && addressBytes[1] >= 16 && addressBytes[1] <= 31) || (addressBytes[0] == 192 && addressBytes[1] == 168) || (addressBytes[0] == 169 && addressBytes[1] == 254))
				{
					return false;
				}
			}
			else if (addressBytes.Length == 16)
			{
				if (addressBytes[0] == 254 && (addressBytes[1] & 0xC0) == 128)
				{
					return false;
				}
				if ((addressBytes[0] & 0xFE) == 252)
				{
					return false;
				}
			}
		}
		return true;
	}

	private static string NormalizeUrl(string domainOrUrl)
	{
		if (domainOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || domainOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			return domainOrUrl;
		}
		return "https://" + domainOrUrl;
	}

	private void SwitchToDomain(string domain, bool clearRelay)
	{
		_currentDomain = domain;
		_requestsServedOnCurrentDomain = 0;
		_currentDomainActivatedAt = DateTime.UtcNow;
		if (clearRelay)
		{
			_settings.RelayDomain = string.Empty;
		}
	}

	private void UpdateLastPendingEntryOutcome(string outcome)
	{
		lock (_failureLogLock)
		{
			for (int num = _failureLog.Count - 1; num >= 0; num--)
			{
				if (_failureLog[num].ResolutionOutcome == "pending")
				{
					_failureLog[num] = _failureLog[num]with
					{
						ResolutionOutcome = outcome
					};
					break;
				}
			}
		}
		try
		{
			List<RelayFailureEntry> list = _settings.RelayFailureLog ?? new List<RelayFailureEntry>();
			for (int num2 = list.Count - 1; num2 >= 0; num2--)
			{
				if (list[num2].ResolutionOutcome == "pending")
				{
					list[num2] = list[num2]with
					{
						ResolutionOutcome = outcome
					};
					break;
				}
			}
			_settings.RelayFailureLog = list;
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to update relay failure entry outcome in settings.");
		}
	}

	private void AppendFailureEntry(RelayFailureEntry entry)
	{
		lock (_failureLogLock)
		{
			_failureLog.Add(entry);
		}
		try
		{
			List<RelayFailureEntry> list = _settings.RelayFailureLog ?? new List<RelayFailureEntry>();
			list.Add(entry);
			_settings.RelayFailureLog = list;
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to persist relay failure entry to settings.");
		}
	}

	private void TrackBlockchainResolution(bool success, int endpointCount, string trigger, string? error = null)
	{
		try
		{
			List<(string, string)> list = new List<(string, string)>
			{
				("success", success.ToString()),
				("endpoint_count", endpointCount.ToString()),
				("trigger_reason", trigger)
			};
			if (error != null)
			{
				list.Add(("error", error));
			}
			Analytics?.SendEvent("relay_blockchain_resolution", list.ToArray());
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Failed to send relay_blockchain_resolution event.");
		}
	}

	private void TrackFallbackActivated(string relayDomain, string reason)
	{
		try
		{
			Analytics?.SendEvent("relay_fallback_activated", ("relay_domain", relayDomain), ("reason", reason), ("primary_domain", "https://api.ciphravpn.com"));
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Failed to send relay_fallback_activated event.");
		}
	}
}
