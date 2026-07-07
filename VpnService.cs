using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reactive;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Exceptions;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services.Interfaces;
using Ciphra.VPN.Common.Utils;
using Serilog;
using VpnHood.Core.Client.Abstractions;
using VpnHood.Core.Client.Abstractions.Exceptions;
using VpnHood.Core.Client.VpnServices.Abstractions;
using VpnHood.Core.Client.VpnServices.Manager;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Toolkit.ApiClients;
using VpnHood.Core.Toolkit.Net;

namespace Ciphra.VPN.Common.Services;

public class VpnService : IVpnService, IDisposable
{
	private enum A1ProbeOutcome
	{
		Succeeded,
		ReachedButRejected,
		NotReached
	}

	private sealed class KeepAliveBridge : IVpnServiceKeepAliveBridge
	{
		private readonly VpnService _owner;

		public KeepAliveBridge(VpnService owner)
		{
			_owner = owner;
		}

		public Task<HealthSnapshot> SampleHealthAsync(CancellationToken ct)
		{
			bool needsReconnect = false;
			bool isClientConnected = false;
			long bytesSent = 0L;
			long bytesReceived = 0L;
			int unstableCountCumulative = 0;
			int waitingCountCumulative = 0;
			try
			{
				if (_owner._serviceManager == null)
				{
					needsReconnect = true;
					Log.Warning("Keep-Alive: Manager is null.");
				}
				else
				{
					ConnectionInfo connectionInfo = _owner._serviceManager.ConnectionInfo;
					ClientState clientState = connectionInfo.ClientState;
					if (clientState == ClientState.Disposed || clientState == ClientState.None)
					{
						needsReconnect = true;
						Log.Warning<ClientState>("Keep-Alive: Client state is {State}.", clientState);
					}
					else if (clientState == ClientState.Connected)
					{
						isClientConnected = true;
						SessionStatus sessionStatus = connectionInfo.SessionStatus;
						Traffic? traffic = sessionStatus?.SessionTraffic;
						bytesSent = traffic?.Sent ?? 0;
						bytesReceived = traffic?.Received ?? 0;
						unstableCountCumulative = sessionStatus?.UnstableCount ?? 0;
						waitingCountCumulative = sessionStatus?.WaitingCount ?? 0;
					}
				}
			}
			catch (ObjectDisposedException)
			{
				needsReconnect = true;
				Log.Warning("Keep-Alive: Manager threw ObjectDisposedException.");
			}
			return Task.FromResult(new HealthSnapshot(needsReconnect, isClientConnected, bytesSent, bytesReceived, unstableCountCumulative, waitingCountCumulative));
		}

		public async Task<VpnServerDto?> RefreshAccessKeyAsync(VpnServerDto server, CancellationToken ct)
		{
			try
			{
				VpnServerDto refreshed = await _owner._vpnServerRepository.RefreshServerAccessKeyAsync(server, ct);
				if (refreshed != null)
				{
					_owner._savedServer = refreshed;
					Log.Information<string>("Keep-Alive: Refreshed AccessKey for server {ServerId}.", refreshed.Id);
				}
				return refreshed;
			}
			catch (Exception ex)
			{
				Exception refreshEx = ex;
				Log.Warning(refreshEx, "Keep-Alive: Token refresh failed.");
				return null;
			}
		}

		public Task ReconnectAsync(VpnServerDto server, ConnectionSettings? overrides, CancellationToken ct)
		{
			ConnectionSettings overrideSettings = overrides ?? _owner._sessionOverrideSettings;
			return _owner.ConnectToServerAsync(server, ct, overrideSettings, enableInitialConnectSmartHeal: false);
		}

		public async Task<SmartHealCandidate?> TrySmartHealAsync(VpnServerDto server, ConnectionSettings original, CancellationToken ct)
		{
			using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, Volatile.Read(in _owner._disconnectCts).Token);
			CancellationToken effectiveCt = linkedCts.Token;
			bool lockAcquired = false;
			try
			{
				lockAcquired = await _owner._connectionLock.WaitAsync(TimeSpan.FromSeconds(15L), effectiveCt);
				if (!lockAcquired)
				{
					Log.Warning("Keep-Alive: Could not acquire connection lock for SmartHeal within 15s.");
					return null;
				}
				SmartHealCandidate winner = await _owner.TrySmartHealAsync(server, original, effectiveCt);
				if (winner != null)
				{
					_owner._sessionOverrideSettings = winner.Settings;
				}
				return winner;
			}
			finally
			{
				if (lockAcquired)
				{
					_owner._connectionLock.Release();
				}
			}
		}
	}

	private const int MaxRetryAttempts = 5;

	private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1L);

	private static readonly TimeSpan AdapterIpStackSettleDelay = TimeSpan.FromSeconds(3L);

	private static readonly TimeSpan WinTunDevnodeGoneTimeout = TimeSpan.FromSeconds(8L);

	private bool _rotatedDuringCurrentConnect;

	private bool _batteryExemptionRequestedDuringConnect;

	private VpnServiceManager? _serviceManager = null;

	private readonly IVpnServiceManagerFactory _serviceManagerFactory;

	private readonly IVpnStorageFolderProvider _storageFolderProvider;

	private readonly Settings _settings;

	private readonly ISmartHealService _smartHealService;

	private readonly VpnServerRepository _vpnServerRepository;

	private readonly RelayDomainManager? _relayDomainManager;

	private readonly IAppAnalytics? _analytics;

	private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

	private CancellationTokenSource _disconnectCts = new CancellationTokenSource();

	private bool _isConnecting = false;

	private bool _isDisconnecting = false;

	private volatile bool _keepAlive = false;

	private VpnServerDto? _savedServer;

	private ConnectionSettings? _sessionOverrideSettings;

	private CancellationTokenSource? _keepAliveCts;

	private Task? _keepAliveTask;

	private string? _logClientState;

	private readonly MidSessionSmartHealCoordinator _coordinator;

	private static readonly TimeSpan A1ValidationWindow = TimeSpan.FromSeconds(10L);

	private const long A1ReceivedBytesThreshold = 1024L;

	private const long A1SentBytesThreshold = 4096L;

	private static readonly string[] A1ProbeUrls = new string[2] { "https://cp.cloudflare.com/generate_204", "https://connectivitycheck.gstatic.com/generate_204" };

	private static readonly TimeSpan A1ProbeTimeout = TimeSpan.FromSeconds(5L);

	private static readonly HttpClient A1ProbeClient = new HttpClient(new SocketsHttpHandler
	{
		PooledConnectionLifetime = TimeSpan.Zero,
		PooledConnectionIdleTimeout = TimeSpan.Zero,
		AllowAutoRedirect = false
	})
	{
		Timeout = Timeout.InfiniteTimeSpan
	};

	private static readonly string[] WinTunAdminPrivilegeCodes = new string[5] { "5", "-2147024891", "1314", "0x80070522", "-2147023582" };

	private const string WinDivertAssemblyName = "VpnHood.Core.VpnAdapters.WinDivert";

	private const int HResultErrorAlreadyExists = -2147024713;

	private static readonly Regex CommentStripperRegex = new Regex("#.*|;.*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private string CurrentAdapterName => _settings.WintunAdapterCurrentName ?? _serviceManagerFactory.WinTunAdapterBaseName;

	public IObservable<SmartHealCandidate> SmartHealSucceeded => _coordinator.SmartHealSucceeded;

	public IObservable<bool> IsSmartHealInProgress => _coordinator.IsSmartHealInProgress;

	public IObservable<Unit> BlockedNetworkDetected => _coordinator.BlockedNetworkDetected;

	public VpnService(IVpnServiceManagerFactory serviceManagerFactory, IVpnStorageFolderProvider storageFolderProvider, Settings settings, ISmartHealService smartHealService, VpnServerRepository vpnServerRepository, RelayDomainManager? relayDomainManager = null, IAppAnalytics? analytics = null)
	{
		_serviceManagerFactory = serviceManagerFactory ?? throw new ArgumentNullException("serviceManagerFactory");
		_storageFolderProvider = storageFolderProvider ?? throw new ArgumentNullException("storageFolderProvider");
		_settings = settings ?? throw new ArgumentNullException("settings");
		_smartHealService = smartHealService ?? throw new ArgumentNullException("smartHealService");
		_vpnServerRepository = vpnServerRepository ?? throw new ArgumentNullException("vpnServerRepository");
		_relayDomainManager = relayDomainManager;
		_analytics = analytics;
		_coordinator = new MidSessionSmartHealCoordinator(new KeepAliveBridge(this), _settings, _analytics);
	}

	public ConnectionStateDto? CheckConnectionState(bool createIfMissing = false)
	{
		if (_isConnecting)
		{
			return new ConnectionStateDto(ClientState.Connecting, null, null, 0L, 0L, 0L, 0L, 0L, 0L, 0L);
		}
		if (_isDisconnecting)
		{
			return new ConnectionStateDto(ClientState.Disconnecting, null, null, 0L, 0L, 0L, 0L, 0L, 0L, 0L);
		}
		VpnServiceManager manager = (createIfMissing ? (_serviceManager ?? (_serviceManager = _serviceManagerFactory.CreateVpnServiceManager(_settings.UseWinTunOnlyAdapter))) : _serviceManager);
		return CheckConnectionState(manager);
	}

	private ConnectionStateDto? CheckConnectionState(VpnServiceManager? manager)
	{
		try
		{
			if (manager == null)
			{
				return null;
			}
			try
			{
				string text = manager?.ConnectionInfo?.ClientState.ToString();
				if (_logClientState != text)
				{
					_logClientState = text ?? "null";
					Log.Information<string>("VPN client state changed: {State}", _logClientState);
				}
			}
			catch
			{
			}
			ConnectionInfo connectionInfo = manager.ConnectionInfo;
			try
			{
				ClientState? clientState = connectionInfo?.ClientState;
				if (clientState.HasValue && clientState == ClientState.Disposed)
				{
					return null;
				}
			}
			catch (ObjectDisposedException)
			{
				return null;
			}
			if (connectionInfo == null)
			{
				return null;
			}
			Exception ex2 = null;
			if (connectionInfo.ClientState != ClientState.Disconnecting && connectionInfo.Error != null)
			{
				ex2 = connectionInfo.Error.ToException();
				Log.Error<string>(ex2, "VPN service connection error: {ErrorMessage}", connectionInfo.Error.Message);
			}
			SessionStatus sessionStatus = connectionInfo.SessionStatus;
			Traffic? traffic = sessionStatus?.Speed;
			Traffic? traffic2 = sessionStatus?.SessionTraffic;
			Traffic? traffic3 = sessionStatus?.CycleTraffic;
			return new ConnectionStateDto(connectionInfo.ClientState, connectionInfo.SessionInfo?.CreatedTime, ex2, traffic?.Sent ?? 0, traffic?.Received ?? 0, traffic2?.Sent ?? 0, traffic2?.Received ?? 0, sessionStatus?.SessionMaxTraffic ?? 0, traffic3?.Sent ?? 0, traffic3?.Received ?? 0, sessionStatus?.SessionExpirationTime, sessionStatus?.ActiveClientCount, sessionStatus?.ChannelProtocol, connectionInfo.SessionInfo?.IsUdpChannelSupported);
		}
		catch (Exception ex3)
		{
			Log.Error(ex3, "Error checking VPN service connection state.");
		}
		return null;
	}

	public Task ConnectToServerAsync(VpnServerDto vpnServer, CancellationToken ct, ConnectionSettings? overrideSettings = null)
	{
		return ConnectToServerAsync(vpnServer, ct, overrideSettings, enableInitialConnectSmartHeal: true);
	}

	private async Task ConnectToServerAsync(VpnServerDto vpnServer, CancellationToken ct, ConnectionSettings? overrideSettings, bool enableInitialConnectSmartHeal)
	{
		using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, Volatile.Read(in _disconnectCts).Token);
		CancellationToken token = linkedCts.Token;
		await _connectionLock.WaitAsync(token);
		_rotatedDuringCurrentConnect = false;
		_batteryExemptionRequestedDuringConnect = false;
		if (OperatingSystem.IsWindows())
		{
			Task.Run(delegate
			{
				try
				{
					HashSet<string> hashSet = new HashSet<string>(_settings.KnownWintunAdapterNames);
					foreach (string winTunAdapterSeedName in _serviceManagerFactory.WinTunAdapterSeedNames)
					{
						hashSet.Add(winTunAdapterSeedName);
					}
					IReadOnlyList<string> readOnlyList = WinTunAdapterCleaner.SweepOrphanedAdapters(hashSet, CurrentAdapterName);
					if (readOnlyList.Count > 0)
					{
						_settings.PruneKnownWintunAdapterNames(readOnlyList);
					}
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex4)
				{
					Log.Warning(ex4, "Pre-connect WinTun sweep failed.");
				}
			});
		}
		try
		{
			if (string.IsNullOrEmpty(_settings.UserId))
			{
				throw new InvalidOperationException("User ID is not set in settings.");
			}
			if (vpnServer == null)
			{
				throw new ArgumentNullException("vpnServer");
			}
			if (string.IsNullOrEmpty(vpnServer.AccessKey))
			{
				throw new ArgumentException("VPN server access key cannot be null or empty.", "AccessKey");
			}
			_savedServer = vpnServer;
			_sessionOverrideSettings = overrideSettings;
			_isConnecting = true;
			Log.Information<string>("Connecting to VPN server: {ServerName}", vpnServer.Id);
			NewSessionInfo priorSnapshot = CapturePriorSessionSnapshot();
			try
			{
				await ConnectToServerCoreAsync(vpnServer, overrideSettings, token);
				OnConnectSucceededInternal(priorSnapshot);
			}
			catch (VpnConnectionException ex)
			{
				VpnConnectionException ex2 = ex;
				if (enableInitialConnectSmartHeal && _settings.IsSmartHealEnabled && !ex2.IsUnrecoverable() && !ex2.IsSmartHealNowSentinel() && !token.IsCancellationRequested)
				{
					_coordinator.SetSmartHealInProgress(value: true);
					try
					{
						ConnectionSettings original = new ConnectionSettings(_settings.DropQuic, _settings.DropUdp, _settings.ChannelProtocol);
						SmartHealCandidate winner = await TrySmartHealAsync(vpnServer, original, token);
						if (winner != null)
						{
							_sessionOverrideSettings = winner.Settings;
							OnConnectSucceededInternal(priorSnapshot);
							_coordinator.EmitSmartHealSucceeded(winner);
							return;
						}
					}
					finally
					{
						_coordinator.SetSmartHealInProgress(value: false);
					}
				}
				if (!(ex is Exception source))
				{
					throw ex;
				}
				ExceptionDispatchInfo.Capture(source).Throw();
			}
		}
		finally
		{
			_isConnecting = false;
			_rotatedDuringCurrentConnect = false;
			_batteryExemptionRequestedDuringConnect = false;
			_connectionLock.Release();
		}
	}

	private async Task ConnectToServerCoreAsync(VpnServerDto vpnServer, ConnectionSettings? overrideSettings, CancellationToken ct)
	{
		TimeSpan currentDelay = InitialRetryDelay;
		bool clearStorageOnNextAttempt = false;
		bool useWinTunOnly = _settings.UseWinTunOnlyAdapter;
		try
		{
			for (int attempt = 1; attempt <= 5; attempt++)
			{
				TimeSpan attemptTimeout = ((attempt <= 2 && OperatingSystem.IsAndroid()) ? TimeSpan.FromSeconds(180L) : TimeSpan.FromSeconds(30L));
				using CancellationTokenSource attemptCts = new CancellationTokenSource(attemptTimeout);
				using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, attemptCts.Token))
				{
					try
					{
						await StopAndDisposeServiceManagerAsync();
						if (clearStorageOnNextAttempt)
						{
							clearStorageOnNextAttempt = false;
							_serviceManagerFactory.DisposeCurrentDevice();
							if (ClearVpnStorageFolder())
							{
								Log.Information("Cleared VPN storage folder before retry.");
							}
							else
							{
								Log.Warning("Could not fully clear VPN storage folder before retry.");
							}
						}
						_serviceManager = _serviceManagerFactory.CreateVpnServiceManager(useWinTunOnly);
						ClientOptions clientOptions = BuildClientOptions(_settings, vpnServer, overrideSettings, CurrentAdapterName, _serviceManagerFactory.SplitTunnelCapabilities);
						await _serviceManager.Start(clientOptions, linkedCts.Token);
						Log.Information<string>("Connected to VPN server: {ServerName}", vpnServer.Id);
						if (useWinTunOnly && !_settings.UseWinTunOnlyAdapter)
						{
							_settings.UseWinTunOnlyAdapter = true;
						}
						LogChannelDiagnostics(vpnServer.Id, overrideSettings?.ChannelProtocol ?? _settings.ChannelProtocol);
						FlushRelayFailureLog();
						break;
					}
					catch (OperationCanceledException) when (!ct.IsCancellationRequested)
					{
						Log.Warning<int>("VPN Start attempt {Attempt} hung and was timed out.", attempt);
						if (attempt == 5)
						{
							throw new TimeoutException("VPN failed to start after maximum retry attempts.");
						}
						await StopAndDisposeServiceManagerAsync();
						if (OperatingSystem.IsWindows())
						{
							int killed = WinTunAdapterCleaner.TryKillOrphanedProcesses();
							if (killed > 0)
							{
								Log.Information<int>("Killed {Count} orphaned VPN process(es) after hang-timeout.", killed);
							}
							_serviceManagerFactory.DisposeCurrentDevice();
							if (_serviceManagerFactory.TryCleanupStaleAdapter(CurrentAdapterName))
							{
								Log.Information("Cleaned up stale WinTun adapter after hang-timeout.");
							}
							clearStorageOnNextAttempt = true;
						}
						await Task.Delay(currentDelay, ct);
						currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2.0);
					}
					catch (Exception ex2) when (ex2 is OperationCanceledException || ex2 is TaskCanceledException)
					{
						Log.Information("User cancelled the connection attempt. Exiting retry loop.");
						throw;
					}
					catch (Exception ex3) when (IsWinDivertLoadFailure(ex3) && !useWinTunOnly)
					{
						Log.Warning<int, int>(ex3, "WinDivert assembly load failed (attempt {Attempt}/{MaxAttempts}); switching to WinTun-only adapter for this session.", attempt, 5);
						useWinTunOnly = true;
						try
						{
							_analytics?.SendEvent("WinDivertFallbackActivated");
						}
						catch (Exception ex4)
						{
							Exception analyticsEx = ex4;
							Log.Debug(analyticsEx, "Failed to send WinDivertFallbackActivated event.");
						}
						await StopAndDisposeServiceManagerAsync();
						if (attempt == 5)
						{
							throw;
						}
					}
					catch (Exception ex5) when (ContainsMessage(ex5, "VpnService could not establish any connection"))
					{
						Log.Warning<int, int, string>(ex5, "VpnService could not connect error detected (attempt {Attempt}/{MaxAttempts}): {Message}", attempt, 5, ex5.Message);
						if (attempt == 5)
						{
							throw;
						}
						await StopAndDisposeServiceManagerAsync();
						if (attempt == 1)
						{
							vpnServer = await TryRefreshAccessKeyAsync(vpnServer, "no-connection", ct);
						}
						Log.Information<double>("Retrying connection in {Delay} seconds...", currentDelay.TotalSeconds);
						await Task.Delay(currentDelay, ct);
						currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2.0);
					}
					catch (Exception ex6) when (ContainsExceptionType<UnreachableServerException>(ex6))
					{
						Log.Warning<int, int, string>(ex6, "Unreachable server error detected (attempt {Attempt}/{MaxAttempts}): {Message}", attempt, 5, ex6.Message);
						if (attempt == 5)
						{
							throw;
						}
						await StopAndDisposeServiceManagerAsync();
						if (attempt <= 2)
						{
							vpnServer = await TryRefreshAccessKeyAsync(vpnServer, "unreachable", ct);
						}
						Log.Information<double>("Retrying connection in {Delay} seconds...", currentDelay.TotalSeconds);
						await Task.Delay(currentDelay, ct);
						currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2.0);
					}
					catch (Exception ex7) when (IsWinTunAdapterException(ex7) || IsVpnServiceUnreachable(ex7) || ContainsMessage(ex7, "does not support IPv4 or IPv6") || ContainsMessage(ex7, "Failed to start WinTun session"))
					{
						Log.Warning<int, int, string>(ex7, "WinTun/service-init error detected (attempt {Attempt}/{MaxAttempts}): {Message}", attempt, 5, SanitizeWinTunErrorMessage(ex7));
						if (_rotatedDuringCurrentConnect && IsWinTunAlreadyExistsException(ex7))
						{
							Log.Warning<string>("WinTun ERROR_ALREADY_EXISTS persists on rotated name '{Name}'. Rotation did not resolve the stuck slot; giving up this connect.", CurrentAdapterName);
							throw;
						}
						if (attempt == 5)
						{
							throw;
						}
						if (IsWinTunDriverNotFound(ex7))
						{
							ClearWinTunTempFolder();
							if (attempt >= 2)
							{
								Log.Error<int>("WinTun driver not found after re-extraction (attempt {Attempt}). Possible causes: Memory Integrity (HVCI), antivirus quarantine, or missing driver components.", attempt);
								throw;
							}
						}
						else
						{
							int killed2 = WinTunAdapterCleaner.TryKillOrphanedProcesses();
							if (killed2 > 0)
							{
								Log.Information<int>("Killed {Count} orphaned VPN process(es) before adapter cleanup.", killed2);
							}
							_serviceManagerFactory.DisposeCurrentDevice();
							string oldNameForCleanup = CurrentAdapterName;
							if (_serviceManagerFactory.TryCleanupStaleAdapter(oldNameForCleanup))
							{
								Log.Information<string>("Cleaned up stale WinTun adapter '{Name}' before retry.", oldNameForCleanup);
							}
							if (attempt >= 2)
							{
								TryRotateAdapterName(ex7);
							}
						}
						clearStorageOnNextAttempt = true;
						if (_rotatedDuringCurrentConnect)
						{
							await Task.Delay(TimeSpan.FromMilliseconds(250L), ct);
						}
						else
						{
							Guid oldGuid = WinTunAdapterCleaner.BuildAdapterGuid(CurrentAdapterName);
							Log.Information<double>("Waiting up to {Delay}s for WinTun devnode to clear before retry...", WinTunDevnodeGoneTimeout.TotalSeconds);
							await WinTunAdapterCleaner.WaitUntilDevnodeGoneAsync(oldGuid, WinTunDevnodeGoneTimeout, ct);
						}
						if (ContainsMessage(ex7, "does not support IPv4 or IPv6"))
						{
							Log.Information<double>("Adapter IP stack was not ready; settling {Delay}s before retry...", AdapterIpStackSettleDelay.TotalSeconds);
							await Task.Delay(AdapterIpStackSettleDelay, ct);
						}
					}
					catch (ApiException ex8)
					{
						Log.Warning<int, int, string>((Exception)ex8, "Failed to connect to VPN service (attempt {Attempt}/{MaxAttempts}): {Message}", attempt, 5, SanitizeWinTunErrorMessage(ex8));
						if (IsWinTunAccessDenied(ex8) || attempt == 5)
						{
							throw;
						}
						if (IsWinTunAdapterException(ex8))
						{
							if (_rotatedDuringCurrentConnect && IsWinTunAlreadyExistsException(ex8))
							{
								Log.Warning<string>("ApiException WinTun ERROR_ALREADY_EXISTS persists on rotated name '{Name}'. Rotation did not resolve; giving up this connect.", CurrentAdapterName);
								throw;
							}
							int killed3 = WinTunAdapterCleaner.TryKillOrphanedProcesses();
							if (killed3 > 0)
							{
								Log.Information<int>("Killed {Count} orphaned VPN process(es) before adapter cleanup.", killed3);
							}
							_serviceManagerFactory.DisposeCurrentDevice();
							string oldNameForCleanup2 = CurrentAdapterName;
							if (_serviceManagerFactory.TryCleanupStaleAdapter(oldNameForCleanup2))
							{
								Log.Information<string>("Cleaned up stale WinTun adapter '{Name}' (from ApiException) before retry.", oldNameForCleanup2);
							}
							if (attempt >= 2)
							{
								TryRotateAdapterName(ex8);
							}
							clearStorageOnNextAttempt = true;
							if (_rotatedDuringCurrentConnect)
							{
								await Task.Delay(TimeSpan.FromMilliseconds(250L), ct);
								continue;
							}
							Guid oldGuid2 = WinTunAdapterCleaner.BuildAdapterGuid(oldNameForCleanup2);
							Log.Information<double>("Waiting up to {Delay}s for WinTun devnode to clear before retry...", WinTunDevnodeGoneTimeout.TotalSeconds);
							await WinTunAdapterCleaner.WaitUntilDevnodeGoneAsync(oldGuid2, WinTunDevnodeGoneTimeout, ct);
							continue;
						}
						await StopAndDisposeServiceManagerAsync();
						Log.Information<double>("Retrying connection in {Delay} seconds...", currentDelay.TotalSeconds);
						await Task.Delay(currentDelay, ct);
						currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2.0);
					}
					catch (UnauthorizedAccessException ex9)
					{
						Log.Warning<int, int, string>((Exception)ex9, "Server returned unauthorized (attempt {Attempt}/{MaxAttempts}): {Message}. Refreshing token and AccessKey...", attempt, 5, ex9.Message);
						if (attempt == 5)
						{
							throw;
						}
						await StopAndDisposeServiceManagerAsync();
						vpnServer = await TryRefreshAccessKeyAsync(vpnServer, "unauthorized", ct);
						Log.Information<double>("Retrying connection in {Delay} seconds...", currentDelay.TotalSeconds);
						await Task.Delay(currentDelay, ct);
						currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2.0);
					}
					catch (Exception ex10) when (!(ex10 is UnauthorizedAccessException) && ContainsMessage(ex10, "unauthorized operation"))
					{
						Log.Warning<int, int, string>(ex10, "Wrapped unauthorized error detected (attempt {Attempt}/{MaxAttempts}): {Message}. Refreshing token and AccessKey...", attempt, 5, ex10.Message);
						if (attempt == 5)
						{
							throw;
						}
						await StopAndDisposeServiceManagerAsync();
						vpnServer = await TryRefreshAccessKeyAsync(vpnServer, "unauthorized-wrapped", ct);
						Log.Information<double>("Retrying connection in {Delay} seconds...", currentDelay.TotalSeconds);
						await Task.Delay(currentDelay, ct);
						currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2.0);
					}
					catch (Exception ex11) when (ContainsMessage(ex11, "json length is"))
					{
						Log.Warning<int, int, string>(ex11, "Protocol framing error detected (attempt {Attempt}/{MaxAttempts}): {Message}. Server endpoint may be returning non-VPN responses.", attempt, 5, ex11.Message);
						if (attempt == 5)
						{
							throw;
						}
						await StopAndDisposeServiceManagerAsync();
						if (attempt == 1)
						{
							vpnServer = await TryRefreshAccessKeyAsync(vpnServer, "protocol-error", ct);
						}
						Log.Information<double>("Retrying connection in {Delay} seconds...", currentDelay.TotalSeconds);
						await Task.Delay(currentDelay, ct);
						currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2.0);
					}
					catch (Exception ex12) when (ContainsMessage(ex12, "Could not add virtual IPv4"))
					{
						Log.Warning<int, int, string>(ex12, "Virtual IPv4 collision detected (attempt {Attempt}/{MaxAttempts}): {Message}", attempt, 5, ex12.Message);
						if (attempt == 5)
						{
							throw;
						}
						await StopAndDisposeServiceManagerAsync();
						Log.Information<double>("Retrying connection in {Delay} seconds...", currentDelay.TotalSeconds);
						await Task.Delay(currentDelay, ct);
						currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2.0);
					}
					catch (Exception ex13) when (ContainsMessage(ex13, "session has been closed"))
					{
						Log.Warning<int, int, string>(ex13, "Session closed by server (attempt {Attempt}/{MaxAttempts}): {Message}", attempt, 5, ex13.Message);
						if (attempt >= 2)
						{
							throw;
						}
						await StopAndDisposeServiceManagerAsync();
						vpnServer = await TryRefreshAccessKeyAsync(vpnServer, "session-closed", ct);
						Log.Information<double>("Retrying connection in {Delay} seconds...", currentDelay.TotalSeconds);
						await Task.Delay(currentDelay, ct);
						currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2.0);
					}
					catch (Exception ex14) when (ContainsMessage(ex14, "Could not grant VPN permission"))
					{
						Log.Warning<int, int, string>(ex14, "Android VPN permission timeout (attempt {Attempt}/{MaxAttempts}): {Message}", attempt, 5, ex14.Message);
						if (attempt >= 2)
						{
							throw;
						}
						await StopAndDisposeServiceManagerAsync();
						await Task.Delay(currentDelay, ct);
						currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2.0);
					}
					catch (Exception ex15) when (ContainsMessage(ex15, "Could not start the VpnService"))
					{
						bool batteryOptIgnored = _serviceManagerFactory.IsBatteryOptimizationIgnored();
						Log.Warning(ex15, "Android VPN service startup timeout (attempt {Attempt}/{MaxAttempts}): {Message}. Battery optimization ignored: {BatteryOptIgnored}", new object[4] { attempt, 5, ex15.Message, batteryOptIgnored });
						if (attempt >= 3)
						{
							if (!batteryOptIgnored && !_batteryExemptionRequestedDuringConnect)
							{
								_batteryExemptionRequestedDuringConnect = _serviceManagerFactory.TryRequestBatteryOptimizationExemption();
								Log.Information<bool>("Battery optimization exemption dialog shown: {Shown}", _batteryExemptionRequestedDuringConnect);
							}
							try
							{
								_analytics?.SendEvent("AndroidServiceStartFailed", ("BatteryOptimizationIgnored", batteryOptIgnored.ToString()), ("ExemptionDialogShown", _batteryExemptionRequestedDuringConnect.ToString()), ("Attempts", attempt.ToString()));
							}
							catch
							{
							}
							throw;
						}
						await StopAndDisposeServiceManagerAsync();
						Log.Information<double>("Retrying connection in {Delay} seconds...", currentDelay.TotalSeconds);
						await Task.Delay(currentDelay, ct);
						currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2.0);
					}
					catch (Exception ex16) when (IsTransientSocketError(ex16))
					{
						Log.Warning<int, int, string>(ex16, "Transient socket error detected (attempt {Attempt}/{MaxAttempts}): {Message}", attempt, 5, ex16.Message);
						if (attempt == 5)
						{
							throw;
						}
						await StopAndDisposeServiceManagerAsync();
						Log.Information<double>("Retrying connection in {Delay} seconds...", currentDelay.TotalSeconds);
						await Task.Delay(currentDelay, ct);
						currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2.0);
					}
					catch (Exception ex4)
					{
						Exception ex17 = ex4;
						DisposeServiceManager();
						Log.Error(ex17, "Unexpected error while connecting to VPN service.");
						throw;
					}
					goto end_IL_00a6;
				}
				end_IL_00a6:;
			}
		}
		catch (Exception ex18) when (!(ex18 is OperationCanceledException))
		{
			if (IsWinTunAccessDenied(ex18))
			{
				SendWinTunAccessDeniedDiagnostics();
			}
			throw new VpnConnectionException(GetUserFacingErrorMessage(ex18), ex18);
		}
	}

	private void SendWinTunAccessDeniedDiagnostics()
	{
		try
		{
			bool? flag = WindowsSecurityDiagnostics.TryIsProcessElevated();
			bool? flag2 = WindowsSecurityDiagnostics.TryIsMemoryIntegrityEnabled();
			Log.Warning<bool?, bool?>("WinTun access denied diagnostics: ProcessElevated={Elevated}, MemoryIntegrityEnabled={MemoryIntegrity}", flag, flag2);
			_analytics?.SendEvent("WinTunAccessDenied", ("ProcessElevated", flag?.ToString() ?? "Unknown"), ("MemoryIntegrityEnabled", flag2?.ToString() ?? "Unknown"));
		}
		catch
		{
		}
	}

	private void OnConnectSucceededInternal(NewSessionInfo priorSnapshot)
	{
		_keepAlive = true;
		NewSessionInfo info = priorSnapshot with
		{
			NewSessionSuppressedTo = ReadCurrentSuppressedTo()
		};
		_coordinator.OnNewSessionEstablished(info);
		StartKeepAliveMonitor();
		if (OperatingSystem.IsWindows())
		{
			_settings.AddKnownWintunAdapterName(CurrentAdapterName);
		}
	}

	private NewSessionInfo CapturePriorSessionSnapshot()
	{
		try
		{
			VpnServiceManager serviceManager = _serviceManager;
			if (serviceManager == null)
			{
				return default(NewSessionInfo);
			}
			ConnectionInfo connectionInfo = serviceManager.ConnectionInfo;
			SessionInfo sessionInfo = connectionInfo?.SessionInfo;
			if (sessionInfo == null)
			{
				return default(NewSessionInfo);
			}
			Traffic? traffic = connectionInfo?.SessionStatus?.SessionTraffic;
			long priorSessionTotalBytes = (traffic?.Sent ?? 0) + (traffic?.Received ?? 0);
			long num = (long)(DateTime.UtcNow - sessionInfo.CreatedTime).TotalMilliseconds;
			if (num < 0)
			{
				num = 0L;
			}
			return new NewSessionInfo(PriorSessionExisted: true, num, priorSessionTotalBytes, SessionSuppressType.None);
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "CapturePriorSessionSnapshot threw; defaulting to no prior.");
			return default(NewSessionInfo);
		}
	}

	private SessionSuppressType ReadCurrentSuppressedTo()
	{
		try
		{
			return (_serviceManager?.ConnectionInfo?.SessionInfo?.SuppressedTo).GetValueOrDefault();
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "ReadCurrentSuppressedTo threw; defaulting to None.");
			return SessionSuppressType.None;
		}
	}

	private void LogChannelDiagnostics(string? serverId, ChannelProtocol requested)
	{
		try
		{
			ConnectionInfo connectionInfo = _serviceManager?.ConnectionInfo;
			SessionInfo sessionInfo = connectionInfo?.SessionInfo;
			SessionStatus sessionStatus = connectionInfo?.SessionStatus;
			if (sessionInfo == null || sessionStatus == null)
			{
				Log.Information<string, bool, bool>("Channel diagnostics unavailable for {ServerId}: SessionInfo={HasInfo} SessionStatus={HasStatus}", serverId, sessionInfo != null, sessionStatus != null);
				return;
			}
			string text = ((sessionInfo.ChannelProtocols != null) ? string.Join(",", sessionInfo.ChannelProtocols) : "<none>");
			Log.Information("Channel diagnostics {ServerId}: requested={Requested} active={Active} udpSupported={UdpSupported} serverProtocols=[{Advertised}] packetChannels={PacketChannels} serverVer={ServerVer} sessionId={SessionId}", new object[8] { serverId, requested, sessionStatus.ChannelProtocol, sessionInfo.IsUdpChannelSupported, text, sessionStatus.PacketChannelCount, sessionInfo.ServerVersion, sessionInfo.SessionId });
			if (requested == ChannelProtocol.Udp && sessionStatus.ChannelProtocol != ChannelProtocol.Udp)
			{
				if (!sessionInfo.IsUdpChannelSupported)
				{
					Log.Warning<string, ChannelProtocol>("UDP downgrade {ServerId}: server did not advertise UDP (HelloResponse.UdpPort=0 or ProtocolVersion<11) — SDK fell back to {Active}", serverId, sessionStatus.ChannelProtocol);
				}
				else
				{
					Log.Warning<string, ChannelProtocol>("UDP downgrade {ServerId}: server advertised UDP but active channel is {Active} — inspect VpnHood SDK logs for handshake failure", serverId, sessionStatus.ChannelProtocol);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "LogChannelDiagnostics threw — non-fatal.");
		}
	}

	private async Task<SmartHealCandidate?> TrySmartHealAsync(VpnServerDto server, ConnectionSettings original, CancellationToken ct)
	{
		SmartHealCandidate result = default(SmartHealCandidate);
		foreach (SmartHealCandidate candidate in _smartHealService.Candidates)
		{
			if (candidate.Settings == original)
			{
				continue;
			}
			ct.ThrowIfCancellationRequested();
			bool candidateConnected = false;
			try
			{
				try
				{
					await ConnectToServerCoreAsync(server, candidate.Settings, ct);
					candidateConnected = true;
				}
				catch (OperationCanceledException)
				{
					ct.ThrowIfCancellationRequested();
					Log.Information<string>("SmartHeal candidate timed out: {Description}", candidate.Description);
					continue;
				}
				catch (VpnConnectionException ex2)
				{
					VpnConnectionException ex3 = ex2;
					Log.Information<string>((Exception)ex3, "SmartHeal candidate failed: {Description}", candidate.Description);
					continue;
				}
				if (await ValidateCandidateAsync(candidate, ct).ConfigureAwait(continueOnCapturedContext: false))
				{
					candidateConnected = false;
					result = candidate;
				}
			}
			finally
			{
				if (candidateConnected)
				{
					try
					{
						await StopAndDisposeServiceManagerAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
					catch (Exception ex4)
					{
						Exception ex5 = ex4;
						Log.Warning(ex5, "SmartHeal teardown of failed candidate threw (best-effort).");
					}
				}
			}
			int num;
			if (num == 2)
			{
				return result;
			}
			result = null;
		}
		return null;
	}

	private async Task<bool> ValidateCandidateAsync(SmartHealCandidate candidate, CancellationToken ct)
	{
		VpnServiceManager manager = _serviceManager;
		if (manager == null)
		{
			Log.Information<string>("SmartHeal validation: no ServiceManager — treating as failed: {Description}", candidate.Description);
			return false;
		}
		await manager.RefreshState(ct).ConfigureAwait(continueOnCapturedContext: false);
		ConnectionInfo startCi = manager.ConnectionInfo;
		SessionInfo startInfo = startCi?.SessionInfo;
		if (startInfo == null)
		{
			Log.Information<string>("SmartHeal validation: SessionInfo not yet published — treating as failed: {Description}", candidate.Description);
			return false;
		}
		Traffic? startTraffic = startCi?.SessionStatus?.SessionTraffic;
		string startSessionId = startInfo.SessionId;
		DateTime startCreatedTime = startInfo.CreatedTime;
		long startSent = startTraffic?.Sent ?? 0;
		long startRecv = startTraffic?.Received ?? 0;
		Task<A1ProbeOutcome> probeTask = RunActiveProbeAsync(ct);
		await Task.Delay(A1ValidationWindow, ct).ConfigureAwait(continueOnCapturedContext: false);
		await manager.RefreshState(ct).ConfigureAwait(continueOnCapturedContext: false);
		ConnectionInfo endCi = manager.ConnectionInfo;
		SessionInfo endInfo = endCi?.SessionInfo;
		Traffic? endTraffic = endCi?.SessionStatus?.SessionTraffic;
		int num;
		if (!(endInfo?.SessionId != startSessionId))
		{
			num = ((endInfo == null || endInfo.CreatedTime != startCreatedTime) ? 1 : 0);
		}
		else
		{
			num = 1;
		}
		if (num != 0)
		{
			Log.Information<string, string, string>("SmartHeal validation aborted: session changed mid-window for {Description} (startId={SId}, endId={EId})", candidate.Description, startSessionId, endInfo?.SessionId);
			try
			{
				await probeTask.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch
			{
			}
			return false;
		}
		A1ProbeOutcome probeOutcome;
		try
		{
			probeOutcome = await probeTask.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch
		{
			probeOutcome = A1ProbeOutcome.NotReached;
		}
		long deltaSent = (endTraffic?.Sent ?? 0) - startSent;
		long deltaRecv = (endTraffic?.Received ?? 0) - startRecv;
		bool passiveEligible = probeOutcome == A1ProbeOutcome.NotReached;
		bool receivedPath = passiveEligible && deltaRecv >= 1024;
		bool sentWithSomeRecvPath = passiveEligible && deltaSent >= 4096 && deltaRecv > 0;
		if (probeOutcome == A1ProbeOutcome.Succeeded || receivedPath || sentWithSomeRecvPath)
		{
			Log.Information("SmartHeal candidate validated: {Description} (probe={Outcome}, sent={S}, recv={R})", new object[4] { candidate.Description, probeOutcome, deltaSent, deltaRecv });
			return true;
		}
		Log.Warning("SmartHeal candidate handshake passed but failed validation gate (DPI suspected): {Description} (probe={Outcome}, sent={S}, recv={R})", new object[4] { candidate.Description, probeOutcome, deltaSent, deltaRecv });
		return false;
	}

	private static async Task<A1ProbeOutcome> RunActiveProbeAsync(CancellationToken ct)
	{
		using CancellationTokenSource probeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		probeCts.CancelAfter(A1ProbeTimeout);
		bool anyReached = false;
		string[] a1ProbeUrls = A1ProbeUrls;
		foreach (string url in a1ProbeUrls)
		{
			if (probeCts.IsCancellationRequested)
			{
				break;
			}
			try
			{
				using HttpResponseMessage resp = await A1ProbeClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, probeCts.Token).ConfigureAwait(continueOnCapturedContext: false);
				anyReached = true;
				if (resp.StatusCode == HttpStatusCode.NoContent)
				{
					return A1ProbeOutcome.Succeeded;
				}
				Log.Debug<string, int>("SmartHeal probe to {Url} returned unexpected status {Status}; trying next if budget allows.", url, (int)resp.StatusCode);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex3)
			{
				Log.Debug<string>(ex3, "SmartHeal probe to {Url} failed; trying next if budget allows (expected when DPI blocks bulk traffic).", url);
			}
		}
		return anyReached ? A1ProbeOutcome.ReachedButRejected : A1ProbeOutcome.NotReached;
	}

	public async Task DisconnectAsync(TimeSpan? timeout = null)
	{
		_keepAlive = false;
		StopKeepAliveMonitor();
		try
		{
			_disconnectCts.Cancel();
		}
		catch (ObjectDisposedException ex)
		{
			ObjectDisposedException ex2 = ex;
			Log.Debug((Exception)ex2, "DisconnectAsync: CTS already disposed (concurrent call)");
		}
		CancellationTokenSource oldCts = Interlocked.Exchange(ref _disconnectCts, new CancellationTokenSource());
		oldCts.Dispose();
		bool lockAcquired = await _connectionLock.WaitAsync(TimeSpan.FromSeconds(15L));
		if (!lockAcquired)
		{
			Log.Warning("DisconnectAsync: timed out waiting for connection lock after 15s. Proceeding with best-effort disconnect.");
		}
		_isDisconnecting = true;
		try
		{
			Log.Information("Disconnecting from VPN service...");
			if (_serviceManager != null && _serviceManager.ConnectionInfo.ClientState.CanDisconnect())
			{
				await _serviceManager.TryStop(timeout ?? TimeSpan.FromSeconds(10L));
			}
			try
			{
				await _serviceManagerFactory.StopVpnTunnelAsync();
			}
			catch (Exception ex3)
			{
				Log.Warning(ex3, "StopVpnTunnelAsync failed (best effort).");
			}
			DisposeServiceManager();
		}
		finally
		{
			if (lockAcquired)
			{
				_connectionLock.Release();
			}
			_isDisconnecting = false;
		}
	}

	private void StartKeepAliveMonitor()
	{
		if (_keepAliveTask != null && !_keepAliveTask.IsCompleted)
		{
			return;
		}
		_keepAliveCts = new CancellationTokenSource();
		CancellationToken token = _keepAliveCts.Token;
		_keepAliveTask = Task.Run(async delegate
		{
			try
			{
				await KeepAliveLoopAsync(token);
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex2)
			{
				Exception ex3 = ex2;
				Log.Error(ex3, "Keep-Alive task failed unexpectedly.");
			}
		});
	}

	private void StopKeepAliveMonitor()
	{
		try
		{
			_keepAliveCts?.Cancel();
		}
		finally
		{
			_keepAliveCts?.Dispose();
			_keepAliveCts = null;
		}
	}

	private async Task KeepAliveLoopAsync(CancellationToken token)
	{
		Log.Information("Keep-Alive monitor started.");
		while (!token.IsCancellationRequested && _keepAlive)
		{
			try
			{
				await Task.Delay(TimeSpan.FromSeconds(5L), token);
				if (!_keepAlive)
				{
					break;
				}
				if (!_isConnecting)
				{
					VpnServerDto saved = Volatile.Read(in _savedServer);
					if (saved != null)
					{
						await _coordinator.ProcessTickAsync(saved, token);
					}
				}
				continue;
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex2)
			{
				Exception ex3 = ex2;
				Log.Error(ex3, "Keep-Alive loop encountered an unexpected error.");
				continue;
			}
			break;
		}
		Log.Information("Keep-Alive monitor stopped.");
	}

	private async Task StopAndDisposeServiceManagerAsync()
	{
		if (_serviceManager != null)
		{
			try
			{
				await _serviceManager.TryStop(TimeSpan.FromSeconds(3L));
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log.Debug(ex2, "TryStop during manager cleanup was unsuccessful (best effort).");
			}
		}
		DisposeServiceManager();
	}

	private void DisposeServiceManager()
	{
		try
		{
			_serviceManager?.Dispose();
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "DisposeServiceManager: exception during service manager dispose (suppressed, best-effort teardown).");
		}
		finally
		{
			_serviceManager = null;
		}
	}

	public void Dispose()
	{
		StopKeepAliveMonitor();
		try
		{
			_disconnectCts.Cancel();
		}
		catch (ObjectDisposedException)
		{
		}
		CancellationTokenSource cancellationTokenSource = Interlocked.Exchange(ref _disconnectCts, new CancellationTokenSource());
		cancellationTokenSource.Dispose();
		_connectionLock.Dispose();
		DisposeServiceManager();
		_coordinator.Dispose();
		(_serviceManagerFactory as IDisposable)?.Dispose();
	}

	private static bool ContainsExceptionType<T>(Exception ex) where T : Exception
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			if (ex2 is T)
			{
				return true;
			}
		}
		return false;
	}

	private string GetUserFacingErrorMessage(Exception ex)
	{
		if (IsDiskFull(ex))
		{
			return "Your device is out of disk space. Please free up some space and try again.";
		}
		if (IsWinTunAccessDenied(ex))
		{
			return (WindowsSecurityDiagnostics.TryIsProcessElevated() == true) ? "VPN adapter creation was blocked by your antivirus or a system security policy. Please add this app to your antivirus exclusions and try again." : "The VPN requires administrator privileges. Please run the application as administrator.";
		}
		if (IsWinTunDriverNotFound(ex))
		{
			return "VPN driver files are missing or blocked by your security software. Please reinstall the application or check your antivirus settings.";
		}
		if (IsWinTunAdapterException(ex))
		{
			return _rotatedDuringCurrentConnect ? "Unable to set up the VPN network adapter after multiple attempts. Please restart the app or Windows to clear the adapter state." : "A VPN network adapter from a previous session could not be released. Please try connecting again.";
		}
		if (ContainsMessage(ex, "WinTun is not supported"))
		{
			return "Your Windows version does not support the VPN driver. Please update to Windows 10 version 1903 or later.";
		}
		if (ContainsMessage(ex, "Failed to load WinTun DLL"))
		{
			return "VPN driver files are missing or blocked. Please reinstall the application or check your antivirus settings.";
		}
		if (ContainsMessage(ex, "does not support IPv4 or IPv6"))
		{
			return "VPN network adapter configuration failed. Please try connecting again.";
		}
		if (ContainsMessage(ex, "Failed to start WinTun session"))
		{
			return "VPN network adapter configuration failed. Please try connecting again.";
		}
		if (ContainsMessage(ex, "Could not grant VPN permission"))
		{
			return "VPN permission was not granted in time. Please tap 'OK' on the system dialog when it appears and try again.";
		}
		if (ContainsMessage(ex, "Could not start the VpnService"))
		{
			if (!OperatingSystem.IsAndroid())
			{
				return "VPN service could not start. Please try again.";
			}
			if (_batteryExemptionRequestedDuringConnect)
			{
				return "VPN service could not start because of battery optimization. Please tap 'Allow' on the dialog that just appeared, then try connecting again.";
			}
			if (_serviceManagerFactory.IsBatteryOptimizationIgnored())
			{
				return "VPN service could not start. Please restart your device and try again.";
			}
			return "VPN service could not start. Please go to Settings > Battery > Battery Optimization and set this app to 'Not optimized', then try again.";
		}
		if (ContainsMessage(ex, "VPN connection has been revoked"))
		{
			return "VPN was disconnected by your device or another VPN app. Please check your VPN settings and try again.";
		}
		if (ContainsMessage(ex, "session was suppressed"))
		{
			return "Your VPN session was ended because you connected from another device.";
		}
		if (ContainsMessage(ex, "session has been closed"))
		{
			return "Your VPN session has ended. Please reconnect.";
		}
		if (ContainsMessage(ex, "traffic quota"))
		{
			return "Your traffic quota has been used up. Please upgrade your plan to continue using the VPN.";
		}
		if (ContainsMessage(ex, "unauthorized operation"))
		{
			return "Your session has expired. Please try connecting again.";
		}
		if (ContainsExceptionType<UnreachableServerException>(ex))
		{
			return "This server is currently unavailable. Please try connecting to a different server.";
		}
		if (IsVpnServiceUnreachable(ex))
		{
			return "VPN service is not responding. Please try connecting again.";
		}
		if (ContainsMessage(ex, "VPN failed to start after maximum retry"))
		{
			return "VPN could not start after multiple attempts. Please try connecting again.";
		}
		return "We're having a temporary connection issue. Please try switching to another server.";
	}

	private static bool IsDiskFull(Exception ex)
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			if (ex2.HResult == -2147024784 || ex2.HResult == -2147024857)
			{
				return true;
			}
			string message = ex2.Message;
			if (!string.IsNullOrEmpty(message) && (message.Contains("not enough space", StringComparison.OrdinalIgnoreCase) || message.Contains("No space left on device", StringComparison.OrdinalIgnoreCase) || message.Contains("disk is full", StringComparison.OrdinalIgnoreCase) || message.Contains("Недостаточно места на диске", StringComparison.Ordinal) || message.Contains("磁盘空间不足", StringComparison.Ordinal)))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool IsWinTunAccessDenied(Exception ex)
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			string[] winTunAdminPrivilegeCodes = WinTunAdminPrivilegeCodes;
			foreach (string code in winTunAdminPrivilegeCodes)
			{
				if (HasWinTunErrorCode(ex2.Message, code))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool ContainsMessage(Exception ex, string text)
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			if (ex2.Message.Contains(text, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool IsWinDivertLoadFailure(Exception ex)
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			if (IsWinDivertLoadException(ex2))
			{
				return true;
			}
			if (ex2 is AggregateException ex3)
			{
				foreach (Exception innerException in ex3.Flatten().InnerExceptions)
				{
					if (IsWinDivertLoadException(innerException))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private static bool IsWinDivertLoadException(Exception ex)
	{
		bool flag = ((ex is FileNotFoundException || ex is FileLoadException || ex is BadImageFormatException) ? true : false);
		return flag ? ex.Message.Contains("VpnHood.Core.VpnAdapters.WinDivert", StringComparison.OrdinalIgnoreCase) : ex.Message.Contains("VpnHood.Core.VpnAdapters.WinDivert, Version=", StringComparison.OrdinalIgnoreCase);
	}

	private async Task<VpnServerDto> TryRefreshAccessKeyAsync(VpnServerDto vpnServer, string reason, CancellationToken ct)
	{
		try
		{
			VpnServerDto refreshed = await _vpnServerRepository.RefreshServerAccessKeyAsync(vpnServer, ct);
			if (refreshed != null)
			{
				_savedServer = refreshed;
				Log.Information<string, string>("AccessKey refreshed ({Reason}) for server {ServerId}. New endpoints will be used on next attempt.", reason, refreshed.Id);
				return refreshed;
			}
			Log.Warning<string, string>("AccessKey refresh ({Reason}) returned no match for server {ServerId}.", reason, vpnServer.Id);
		}
		catch (Exception ex)
		{
			Exception refreshEx = ex;
			Log.Warning<string>(refreshEx, "AccessKey refresh ({Reason}) failed, will retry with existing key.", reason);
		}
		return vpnServer;
	}

	private static bool IsVpnServiceUnreachable(Exception ex)
	{
		return ContainsMessage(ex, "VpnService is unreachable");
	}

	private static bool IsTransientSocketError(Exception ex)
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			if (ex2 is SocketException { SocketErrorCode: var socketErrorCode } && ((socketErrorCode == SocketError.AddressAlreadyInUse || (uint)(socketErrorCode - 10053) <= 1u || socketErrorCode == SocketError.ConnectionRefused) ? true : false))
			{
				return true;
			}
		}
		if (ContainsMessage(ex, "An existing connection was forcibly closed"))
		{
			return true;
		}
		return false;
	}

	private bool TryRotateAdapterName(Exception ex)
	{
		if (_rotatedDuringCurrentConnect)
		{
			return false;
		}
		if (!IsWinTunAdapterException(ex))
		{
			return false;
		}
		string currentAdapterName = CurrentAdapterName;
		string winTunAdapterBaseName = _serviceManagerFactory.WinTunAdapterBaseName;
		string text = $"{winTunAdapterBaseName}.{Guid.NewGuid():N}";
		_settings.AddKnownWintunAdapterName(currentAdapterName);
		_settings.WintunAdapterCurrentName = text;
		_settings.AddKnownWintunAdapterName(text);
		_rotatedDuringCurrentConnect = true;
		Log.Warning<string, string>("Rotated WinTun adapter: '{Old}' -> '{New}' (SwDeviceCreate slot poisoned, existing cleanup did not resolve).", currentAdapterName, text);
		try
		{
			_analytics?.SendEvent("WinTunAdapterRotated", ("from", currentAdapterName), ("to", text));
		}
		catch (Exception ex2)
		{
			Log.Debug(ex2, "Failed to send WinTunAdapterRotated analytics event.");
		}
		return true;
	}

	private static bool IsWinTunAlreadyExistsException(Exception ex)
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			if (ex2.HResult == -2147024713)
			{
				return true;
			}
			string message = ex2.Message;
			if (!string.IsNullOrEmpty(message) && (ContainsDelimited(message, "-2147024713") || message.Contains("0x800700B7", StringComparison.OrdinalIgnoreCase) || HasWinTunErrorCode(message, "183")))
			{
				return true;
			}
		}
		return false;
	}

	private static bool ContainsDelimited(string haystack, string needle)
	{
		int startIndex = 0;
		while ((startIndex = haystack.IndexOf(needle, startIndex, StringComparison.Ordinal)) >= 0)
		{
			int num = startIndex + needle.Length;
			if (num >= haystack.Length || !char.IsDigit(haystack[num]))
			{
				return true;
			}
			startIndex = num;
		}
		return false;
	}

	internal static bool IsWinTunAdapterException(Exception ex)
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			if (IsWinTunNonAdminError(ex2.Message))
			{
				return true;
			}
			if (ex2.Message.Contains("-2147024713") && !HasWinTunErrorCode(ex2.Message, "5") && !HasWinTunErrorCode(ex2.Message, "-2147024891"))
			{
				return true;
			}
		}
		return false;
	}

	private static string SanitizeWinTunErrorMessage(Exception ex)
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			string message = ex2.Message;
			if (message != null)
			{
				if (message.Contains("-2147024713") || message.Contains("0x800700B7"))
				{
					return "WinTun adapter slot still occupied (ERROR_ALREADY_EXISTS, 0x800700B7) — will retry after cleanup.";
				}
				if (message.Contains("-2147024894") || message.Contains("0x80070002"))
				{
					return "WinTun driver DLL not found (ERROR_FILE_NOT_FOUND, 0x80070002) — will re-extract on retry.";
				}
				if (message.Contains("-2147024891") || message.Contains("0x80070005") || IsWinTunAccessDenied(ex2))
				{
					return "WinTun ERROR_ACCESS_DENIED — admin elevation required.";
				}
			}
		}
		string message2 = ex.Message;
		string text = ((message2 == null) ? null : message2.Split('\n', 2)[0]?.Trim());
		return string.IsNullOrEmpty(text) ? ex.GetType().Name : text;
	}

	private static bool IsWinTunNonAdminError(string? message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return false;
		}
		if (!message.Contains("Failed to create WinTun adapter", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		int num = message.LastIndexOf("ErrorCode: ", StringComparison.Ordinal);
		if (num >= 0)
		{
			string value = message.Substring(num + "ErrorCode: ".Length).Trim();
			if (Array.IndexOf(WinTunAdminPrivilegeCodes, value) >= 0)
			{
				return false;
			}
		}
		return true;
	}

	private bool ClearVpnStorageFolder()
	{
		try
		{
			string vpnStorageFolderPath = _storageFolderProvider.GetVpnStorageFolderPath();
			if (!Directory.Exists(vpnStorageFolderPath))
			{
				Log.Information<string>("VPN storage folder does not exist: {Folder}", vpnStorageFolderPath);
				Directory.CreateDirectory(vpnStorageFolderPath);
				Log.Information<string>("Created VPN storage folder: {Folder}", vpnStorageFolderPath);
				return true;
			}
			Log.Information<string>("Clearing VPN storage folder: {Folder}", vpnStorageFolderPath);
			DirectoryInfo directoryInfo = new DirectoryInfo(vpnStorageFolderPath);
			FileInfo[] files = directoryInfo.GetFiles();
			foreach (FileInfo fileInfo in files)
			{
				try
				{
					fileInfo.Delete();
					Log.Debug<string>("Deleted file: {FileName}", fileInfo.Name);
				}
				catch (Exception ex)
				{
					Log.Warning<string>(ex, "Failed to delete file: {FileName}", fileInfo.Name);
				}
			}
			DirectoryInfo[] directories = directoryInfo.GetDirectories();
			foreach (DirectoryInfo directoryInfo2 in directories)
			{
				try
				{
					directoryInfo2.Delete(recursive: true);
					Log.Debug<string>("Deleted directory: {DirName}", directoryInfo2.Name);
				}
				catch (Exception ex2)
				{
					Log.Warning<string>(ex2, "Failed to delete directory: {DirName}", directoryInfo2.Name);
				}
			}
			if (!Directory.Exists(vpnStorageFolderPath))
			{
				Directory.CreateDirectory(vpnStorageFolderPath);
				Log.Information<string>("Recreated VPN storage folder after clearing: {Folder}", vpnStorageFolderPath);
			}
			Log.Information("VPN storage folder cleared successfully");
			return true;
		}
		catch (Exception ex3)
		{
			Log.Error<string>(ex3, "Failed to clear VPN storage folder: {Message}", ex3.Message);
			return false;
		}
	}

	private static bool IsWinTunDriverNotFound(Exception ex)
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			if (HasWinTunErrorCode(ex2.Message, "2"))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasWinTunErrorCode(string? message, string code)
	{
		if (string.IsNullOrEmpty(message))
		{
			return false;
		}
		if (!message.Contains("Failed to create WinTun adapter", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		int num = message.LastIndexOf("ErrorCode: ", StringComparison.Ordinal);
		if (num < 0)
		{
			return false;
		}
		string text = message.Substring(num + "ErrorCode: ".Length).Trim();
		return text == code;
	}

	private static void ClearWinTunTempFolder()
	{
		try
		{
			string text = Path.Combine(Path.GetTempPath(), "VpnHood", "WinTun");
			if (!Directory.Exists(text))
			{
				Log.Information<string>("WinTun temp folder does not exist, nothing to clear: {Folder}", text);
				return;
			}
			Log.Information<string>("Clearing WinTun temp folder to force re-extraction: {Folder}", text);
			Directory.Delete(text, recursive: true);
			Log.Information("WinTun temp folder cleared successfully.");
		}
		catch (Exception ex)
		{
			Log.Warning<string>(ex, "Failed to clear WinTun temp folder: {Message}", ex.Message);
		}
	}

	internal static ClientOptions BuildClientOptions(Settings settings, VpnServerDto vpnServer, ConnectionSettings? overrideSettings, string adapterName, SplitTunnelCapabilities? splitTunnelCapabilities = null)
	{
		SplitTunnelSnapshot splitTunnelSnapshot = settings.SplitTunnel.Snapshot();
		SplitTunnelCapabilities splitTunnelCapabilities2 = splitTunnelCapabilities ?? SplitTunnelCapabilities.Default;
		IpRangeOrderedList ipRangeOrderedList = IpNetwork.All.ToIpRanges();
		IpRangeOrderedList ipRangeOrderedList2 = IpNetwork.None.ToIpRanges();
		bool flag = splitTunnelSnapshot.UseSplitIpViaApp && HasText(splitTunnelSnapshot.SplitIpAppIncludes);
		bool flag2 = splitTunnelSnapshot.UseSplitIpViaDevice && HasText(splitTunnelSnapshot.SplitIpDeviceIncludes);
		bool flag3 = splitTunnelSnapshot.UseSplitIpViaApp && HasText(splitTunnelSnapshot.SplitIpAppExcludes);
		bool flag4 = splitTunnelSnapshot.UseSplitIpViaDevice && HasText(splitTunnelSnapshot.SplitIpDeviceExcludes);
		string text = (flag ? splitTunnelSnapshot.SplitIpAppIncludes : (flag2 ? splitTunnelSnapshot.SplitIpDeviceIncludes : null));
		string text2 = (flag3 ? splitTunnelSnapshot.SplitIpAppExcludes : (flag4 ? splitTunnelSnapshot.SplitIpDeviceExcludes : null));
		string source = (flag ? "SplitIpAppIncludes" : "SplitIpDeviceIncludes");
		string source2 = (flag3 ? "SplitIpAppExcludes" : "SplitIpDeviceExcludes");
		IpRange[] array = SafeParseIpRanges(text, source);
		IpRange[] array2 = SafeParseIpRanges(text2, source2);
		if (array.Length == 0 && flag && flag2)
		{
			array = SafeParseIpRanges(splitTunnelSnapshot.SplitIpDeviceIncludes, "SplitIpDeviceIncludes");
		}
		if (array2.Length == 0 && flag3 && flag4)
		{
			array2 = SafeParseIpRanges(splitTunnelSnapshot.SplitIpDeviceExcludes, "SplitIpDeviceExcludes");
		}
		bool flag5 = array.Length != 0;
		bool flag6 = array2.Length != 0;
		IpRange[] array3 = (splitTunnelSnapshot.UseSplitIpViaApp ? SafeParseIpRanges(splitTunnelSnapshot.SplitIpAppBlocks, "SplitIpAppBlocks") : Array.Empty<IpRange>());
		bool flag7 = array3.Length != 0;
		bool flag8 = splitTunnelSnapshot.UseSplitDomain && (HasContent(splitTunnelSnapshot.SplitDomainIncludes) || HasContent(splitTunnelSnapshot.SplitDomainExcludes) || HasContent(splitTunnelSnapshot.SplitDomainBlocks));
		bool flag9 = flag8 && splitTunnelCapabilities2.IsTcpProxySupported;
		if (flag8 && !flag9)
		{
			Log.Warning("Split tunneling: domain rules ignored because TCP proxy is not supported on this platform.");
		}
		DomainFilterPolicy domainFilterPolicy = (flag9 ? new DomainFilterPolicy
		{
			Includes = SafeParseDomainList(splitTunnelSnapshot.SplitDomainIncludes, "SplitDomainIncludes"),
			Excludes = SafeParseDomainList(splitTunnelSnapshot.SplitDomainExcludes, "SplitDomainExcludes"),
			Blocks = SafeParseDomainList(splitTunnelSnapshot.SplitDomainBlocks, "SplitDomainBlocks")
		} : new DomainFilterPolicy());
		bool flag10 = domainFilterPolicy.Includes.Count > 0;
		bool flag11 = domainFilterPolicy.Excludes.Count > 0;
		bool flag12 = domainFilterPolicy.Blocks.Count > 0;
		bool flag13 = flag10 || flag11 || flag12;
		string[] array4 = ((splitTunnelSnapshot.SplitAppMode == SplitAppMode.Include && splitTunnelCapabilities2.IsIncludeAppsSupported) ? splitTunnelSnapshot.SplitApps.Where((string x) => !string.IsNullOrWhiteSpace(x)).ToArray() : null);
		string[] array5 = ((splitTunnelSnapshot.SplitAppMode == SplitAppMode.Exclude && splitTunnelCapabilities2.IsExcludeAppsSupported) ? splitTunnelSnapshot.SplitApps.Where((string x) => !string.IsNullOrWhiteSpace(x)).ToArray() : null);
		string[] array6 = ((array4 != null && array4.Length > 0) ? array4 : null);
		string[] array7 = ((array5 != null && array5.Length > 0) ? array5 : null);
		if (splitTunnelSnapshot.SplitAppMode == SplitAppMode.Include && array6 == null && splitTunnelSnapshot.SplitApps.Length != 0)
		{
			Log.Warning("Split tunneling: app include rules ignored because this platform does not support app filtering.");
		}
		if (splitTunnelSnapshot.SplitAppMode == SplitAppMode.Exclude && array7 == null && splitTunnelSnapshot.SplitApps.Length != 0)
		{
			Log.Warning("Split tunneling: app exclude rules ignored because this platform does not support app filtering.");
		}
		bool flag14 = array6 != null && array6.Length > 0;
		if (flag14 && (flag5 || flag13))
		{
			Log.Warning("Split tunneling: app include list combined with IP/domain rules — only listed apps reach the tunnel; rules narrow further inside (AND, not OR).");
		}
		if (flag14 && flag7)
		{
			Log.Warning("Split tunneling: app include list combined with IP block list — apps not in the include list bypass the engine and are not subject to blocks.");
		}
		IpRangeOrderedList ipRangeOrderedList3 = ipRangeOrderedList;
		IpRangeOrderedList ipRangeOrderedList4 = ipRangeOrderedList;
		switch (splitTunnelSnapshot.SplitTunnelRoutingMode)
		{
		case RoutingMode.TunnelOnly:
		{
			bool flag15 = flag5 || flag10 || flag14;
			ipRangeOrderedList3 = (flag13 ? ipRangeOrderedList : (flag5 ? ipRangeOrderedList2.Union(array) : (flag14 ? ipRangeOrderedList : ((!flag7) ? ipRangeOrderedList2 : ipRangeOrderedList2.Union(array3)))));
			ipRangeOrderedList4 = (flag5 ? ipRangeOrderedList.Intersect(array) : ((!flag14 || flag10 || flag12) ? ipRangeOrderedList2 : ipRangeOrderedList));
			if (flag7 && !flag13)
			{
				ipRangeOrderedList3 = ipRangeOrderedList3.Union(array3);
			}
			break;
		}
		case RoutingMode.BypassThese:
			ipRangeOrderedList4 = (flag6 ? ipRangeOrderedList.Exclude(array2) : ipRangeOrderedList);
			ipRangeOrderedList3 = ((flag6 && !flag13 && !flag7) ? ipRangeOrderedList.Exclude(array2) : ipRangeOrderedList);
			break;
		default:
			ipRangeOrderedList3 = ipRangeOrderedList;
			ipRangeOrderedList4 = ipRangeOrderedList;
			break;
		}
		bool useTcpProxy = flag13 && splitTunnelCapabilities2.IsTcpProxySupported;
		ClientOptions clientOptions = new ClientOptions
		{
			AppName = adapterName,
			ClientId = settings.UserId,
			AccessKey = AccessKeySanitizer.Sanitize(vpnServer.AccessKey),
			AllowAnonymousTracker = false,
			AllowAlwaysOn = true,
			DropQuic = (overrideSettings?.DropQuic ?? settings.DropQuic),
			DropUdp = (overrideSettings?.DropUdp ?? settings.DropUdp),
			ChannelProtocol = (overrideSettings?.ChannelProtocol ?? settings.ChannelProtocol),
			IsTcpProxySupported = splitTunnelCapabilities2.IsTcpProxySupported,
			UseTcpProxy = useTcpProxy,
			SplitLocalNetwork = !splitTunnelSnapshot.UseSplitLocalNetwork,
			IncludeIpRangesByDevice = ipRangeOrderedList3.ToArray(),
			IncludeIpRangesByApp = ipRangeOrderedList4.ToArray(),
			BlockIpRangesByApp = array3,
			DomainFilterPolicy = domainFilterPolicy,
			ExcludeApps = array7,
			IncludeApps = array6
		};
		bool anyOn = splitTunnelSnapshot.UseSplitLocalNetwork || flag5 || flag6 || flag7 || flag13 || (array7 != null && array7.Length > 0) || (array6 != null && array6.Length > 0);
		LogSplitTunnelConfig(splitTunnelSnapshot, clientOptions, anyOn);
		return clientOptions;
	}

	private static bool HasText(string? text)
	{
		return !string.IsNullOrWhiteSpace(text);
	}

	private static bool HasContent(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = CommentStripperRegex.Replace(text, string.Empty);
		return text2.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Any((string line) => !string.IsNullOrWhiteSpace(line));
	}

	private static string[] SafeParseDomainList(string? text, string source)
	{
		try
		{
			return DomainTextFileParser.Parse(text) ?? Array.Empty<string>();
		}
		catch (FormatException ex)
		{
			Log.Warning<string, string>("Split tunneling: ignoring invalid domain rules in {Source}: {Message}", source, ex.Message);
			return Array.Empty<string>();
		}
	}

	private static IpRange[] SafeParseIpRanges(string? text, string source)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return Array.Empty<IpRange>();
		}
		try
		{
			return IpRangeTextFileParser.Parse(text) ?? Array.Empty<IpRange>();
		}
		catch (FormatException ex)
		{
			Log.Warning<string, string>("Split tunneling: ignoring invalid IP rules in {Source}: {Message}", source, ex.Message);
			return Array.Empty<IpRange>();
		}
	}

	private static string JoinDomainsForLog(IEnumerable<string> domains, int max = 20)
	{
		IList<string> list = (domains as IList<string>) ?? domains.ToList();
		if (list.Count <= max)
		{
			return string.Join(",", list);
		}
		return string.Join(",", list.Take(max)) + $"…(+{list.Count - max} more)";
	}

	private static void LogSplitTunnelConfig(SplitTunnelSnapshot st, ClientOptions options, bool anyOn)
	{
		if (!anyOn)
		{
			Log.Information("Split tunneling: OFF (full tunnel)");
			return;
		}
		Log.Information("Split tunneling: ON. Mode={Mode} BypassLan={Lan} Ip[Device={Device}, App={App}] Domain={Dom} TcpProxy={TcpProxy}", new object[6] { st.SplitTunnelRoutingMode, st.UseSplitLocalNetwork, st.UseSplitIpViaDevice, st.UseSplitIpViaApp, st.UseSplitDomain, options.UseTcpProxy });
		Log.Information<int, int, int>("  IP ranges: byDevice={DeviceCount}, byApp={AppCount}, blocks={BlockCount}", options.IncludeIpRangesByDevice.Length, options.IncludeIpRangesByApp.Length, options.BlockIpRangesByApp.Length);
		if (st.UseSplitDomain)
		{
			Log.Information("  Domain filter: includes={Inc}, excludes={Exc}, blocks={Blk}. Includes='{IncList}', Excludes='{ExcList}', Blocks='{BlkList}'", new object[6]
			{
				options.DomainFilterPolicy.Includes.Count,
				options.DomainFilterPolicy.Excludes.Count,
				options.DomainFilterPolicy.Blocks.Count,
				JoinDomainsForLog(options.DomainFilterPolicy.Includes),
				JoinDomainsForLog(options.DomainFilterPolicy.Excludes),
				JoinDomainsForLog(options.DomainFilterPolicy.Blocks)
			});
		}
		if (st.SplitAppMode != SplitAppMode.All)
		{
			string[] array = options.ExcludeApps ?? options.IncludeApps ?? Array.Empty<string>();
			Log.Information<SplitAppMode, int, string>("  Per-app filter: mode={Mode}, count={Count}, packages='{List}'", st.SplitAppMode, array.Length, JoinDomainsForLog(array));
		}
	}

	private void FlushRelayFailureLog()
	{
		if (_relayDomainManager == null || _analytics == null)
		{
			return;
		}
		try
		{
			List<RelayFailureEntry> list = _relayDomainManager.DrainFailureLog();
			foreach (RelayFailureEntry item in list)
			{
				_analytics.SendRelayFailureEvent(item);
			}
			if (list.Count > 0)
			{
				Log.Information<int>("Flushed {Count} relay failure entries to analytics.", list.Count);
			}
		}
		catch (Exception ex)
		{
			Log.Debug<string>(ex, "Failed to flush relay failure log: {Message}", ex.Message);
		}
	}
}
