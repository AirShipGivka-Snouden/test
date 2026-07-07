using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ga4.Trackers;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Client.Abstractions;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Common.Tokens;
using VpnHood.Core.Filtering.Abstractions;
using VpnHood.Core.Filtering.DomainFiltering;
using VpnHood.Core.Filtering.DomainFiltering.Observation;
using VpnHood.Core.Proxies.EndPointManagement;
using VpnHood.Core.Proxies.EndPointManagement.Abstractions.Options;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Monitoring;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Sockets;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling;
using VpnHood.Core.VpnAdapters.Abstractions;

namespace VpnHood.Core.Client;

public class VpnHoodClient : IDisposable, IAsyncDisposable
{
	private bool _disposed;

	private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

	private readonly IVpnAdapter _vpnAdapter;

	private readonly ISocketFactory _socketFactory;

	private ClientState _lastState;

	private readonly Lock _stateEventLock = new Lock();

	private readonly ServerFinder _serverFinder;

	private readonly AsyncLock _disposeLock = new AsyncLock();

	private readonly NetFilter _netFilter;

	private readonly DomainFilteringService _domainFilteringService;

	private readonly StaticIpFilter _staticIpFilter;

	private ClientSession? _session;

	[CompilerGenerated]
	private bool _003CUseTcpProxy_003Ek__BackingField;

	[CompilerGenerated]
	private bool _003CDropUdp_003Ek__BackingField;

	[CompilerGenerated]
	private bool _003CDropQuic_003Ek__BackingField;

	[CompilerGenerated]
	private ChannelProtocol _003CChannelProtocol_003Ek__BackingField;

	[CompilerGenerated]
	private Exception? _003CLastException_003Ek__BackingField;

	[CompilerGenerated]
	private ClientState _003CState_003Ek__BackingField;

	public DomainObserver DomainObserver => _domainFilteringService.DomainObserver;

	public Token Token { get; }

	public VpnHoodClientConfig Config { get; }

	public ProxyEndPointManager ProxyEndPointManager { get; }

	public IClientSession? Session => _session;

	public IClientSession RequiredSession => _session ?? throw new InvalidOperationException("Session is not created yet.");

	public ITracker? Tracker { get; }

	public IpRangeOrderedList SessionIncludeIpRangesByApp => _staticIpFilter.IncludeRanges;

	public DateTime StateChangedTime { get; private set; } = DateTime.Now;

	public bool UseTcpProxy
	{
		[CompilerGenerated]
		get
		{
			return _003CUseTcpProxy_003Ek__BackingField;
		}
		set
		{
			_003CUseTcpProxy_003Ek__BackingField = value;
			ClientSession? session = _session;
			if (session != null)
			{
				session.UseTcpProxy = value;
			}
		}
	}

	public bool DropUdp
	{
		[CompilerGenerated]
		get
		{
			return _003CDropUdp_003Ek__BackingField;
		}
		set
		{
			_003CDropUdp_003Ek__BackingField = value;
			ClientSession? session = _session;
			if (session != null)
			{
				session.DropUdp = value;
			}
		}
	}

	public bool DropQuic
	{
		[CompilerGenerated]
		get
		{
			return _003CDropQuic_003Ek__BackingField;
		}
		set
		{
			_003CDropQuic_003Ek__BackingField = value;
			ClientSession? session = _session;
			if (session != null)
			{
				session.DropQuic = value;
			}
		}
	}

	public ChannelProtocol ChannelProtocol
	{
		[CompilerGenerated]
		get
		{
			return _003CChannelProtocol_003Ek__BackingField;
		}
		set
		{
			_003CChannelProtocol_003Ek__BackingField = value;
			ClientSession? session = _session;
			if (session != null)
			{
				session.ChannelProtocol = value;
			}
		}
	}

	public Exception? LastException
	{
		get
		{
			Exception? ex = _003CLastException_003Ek__BackingField;
			if (ex == null)
			{
				ClientSession? session = _session;
				if (session == null)
				{
					return null;
				}
				ex = session.LastException;
			}
			return ex;
		}
		[CompilerGenerated]
		private set
		{
			_003CLastException_003Ek__BackingField = value;
		}
	}

	public ProgressStatus? StateProgress
	{
		get
		{
			switch (State)
			{
			case ClientState.FindingReachableServer:
			case ClientState.FindingBestServer:
				return _serverFinder.Progress;
			case ClientState.ValidatingProxies:
				return ProxyEndPointManager.Progress;
			default:
				return null;
			}
		}
	}

	public ClientState State
	{
		[CompilerGenerated]
		get
		{
			return _003CState_003Ek__BackingField;
		}
		private set
		{
			_003CState_003Ek__BackingField = value;
			FireStateChanged();
		}
	}

	public event EventHandler? StateChanged;

	public VpnHoodClient(IVpnAdapter vpnAdapter, ISocketFactory socketFactory, NetFilter? netFilter, string? storageFolder, ITracker? tracker, ClientOptions options)
	{
		if (!VhUtils.IsInfinite(options.MaxPacketChannelTimespan) && options.MaxPacketChannelTimespan < options.MinPacketChannelTimespan)
		{
			throw new ArgumentNullException("MaxPacketChannelTimespan", "MaxPacketChannelTimespan must be bigger or equal than MinPacketChannelTimespan.");
		}
		if (string.IsNullOrEmpty(storageFolder))
		{
			storageFolder = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "vpn-service");
		}
		Config = new VpnHoodClientConfig
		{
			TcpProxyCatcherAddressIpV6 = options.TcpProxyCatcherAddressIpV6,
			TcpProxyCatcherAddressIpV4 = options.TcpProxyCatcherAddressIpV4,
			AllowAnonymousTracker = options.AllowAnonymousTracker,
			MinPacketChannelLifespan = options.MinPacketChannelTimespan,
			MaxPacketChannelLifespan = options.MaxPacketChannelTimespan,
			AutoDisposeVpnAdapter = options.AutoDisposeVpnAdapter,
			MaxPacketChannelCount = options.MaxPacketChannelCount,
			TcpConnectTimeout = options.ConnectTimeout,
			PlanId = options.PlanId,
			AccessCode = options.AccessCode,
			ExcludeApps = options.ExcludeApps,
			IncludeApps = options.IncludeApps,
			IncludeIpRangesByApp = options.IncludeIpRangesByApp,
			BlockIpRangesByApp = options.BlockIpRangesByApp,
			IncludeIpRangesByDevice = options.IncludeIpRangesByDevice,
			DnsServers = options.DnsServers,
			SessionName = options.SessionName,
			AllowStreamReuse = options.AllowChannelReuse,
			UnstableTimeout = options.UnstableTimeout,
			AutoWaitTimeout = options.AutoWaitTimeout,
			Version = options.Version,
			UserAgent = options.UserAgent,
			ClientId = options.ClientId,
			SessionTimeout = options.SessionTimeout,
			IncludeLocalNetwork = options.SplitLocalNetwork,
			IsTcpProxySupported = options.IsTcpProxySupported,
			UseTcpProxy = options.UseTcpProxy,
			DropUdp = options.DropUdp,
			DropQuic = options.DropQuic,
			UserReview = options.UserReview,
			StreamProxyBufferSize = (options.StreamProxyBufferSize ?? TunnelDefaults.ClientStreamProxyBufferSize),
			UdpProxyBufferSize = (options.UdpProxyBufferSize ?? TunnelDefaults.ClientUdpProxyBufferSize),
			UseWebSocket = (((!(options.DebugData1?.Contains("/disable-WebSocket", StringComparison.OrdinalIgnoreCase))) ?? true) ? true : false),
			UseOsTcpStack = (options.DebugData1?.Contains("/os-tcp-stack", StringComparison.OrdinalIgnoreCase) ?? false)
		};
		Token = VpnHood.Core.Common.Tokens.Token.FromAccessKey(options.AccessKey);
		socketFactory = new AdapterSocketFactory(vpnAdapter, socketFactory);
		_socketFactory = socketFactory;
		_vpnAdapter = vpnAdapter;
		Tracker = tracker;
		ChannelProtocol = options.ChannelProtocol;
		_staticIpFilter = new StaticIpFilter(netFilter?.IpFilter);
		StaticDomainFilter staticDomainFilter = new StaticDomainFilter(netFilter?.DomainFilter)
		{
			Blocks = options.DomainFilterPolicy.Blocks,
			Excludes = options.DomainFilterPolicy.Excludes,
			Includes = options.DomainFilterPolicy.Includes
		};
		_netFilter = new NetFilter
		{
			IpFilter = new CachedIpFilter(_staticIpFilter, TimeSpan.FromMinutes(60L)),
			DomainFilter = new CachedDomainFilter(staticDomainFilter, TimeSpan.FromMinutes(60L)),
			IpMapper = netFilter?.IpMapper
		};
		ProxyEndPointManager = new ProxyEndPointManager(options.ProxyOptions ?? new ProxyOptions(), Path.Combine(storageFolder, "proxies"), socketFactory, options.ServerQueryTimeout);
		_serverFinder = new ServerFinder(socketFactory, options.ServerLocation, options.ServerQueryTimeout, options.EndPointStrategy, options.CustomServerEndpoints ?? Array.Empty<IPEndPoint>(), options.AllowEndPointTracker ? tracker : null, ProxyEndPointManager, _vpnAdapter.IsIpVersionSupported(IpVersion.IPv6));
		_domainFilteringService = new DomainFilteringService(_netFilter.DomainFilter, GeneralEventId.Sni, 4096);
		_domainFilteringService.IsEnabled |= options.ForceLogSni || !staticDomainFilter.IsEmpty;
		vpnAdapter.Disposed += delegate
		{
			DisposeAsync();
		};
	}

	private void FireStateChanged()
	{
		using (_stateEventLock.EnterScope())
		{
			if (_lastState == State)
			{
				return;
			}
			_lastState = State;
			StateChangedTime = FastDateTime.Now;
		}
		VhLogger.Instance.LogInformation("Client state is changed. NewState: {NewState}", State);
		Task.Run(delegate
		{
			this.StateChanged?.Invoke(this, EventArgs.Empty);
			if (State == ClientState.Disposed)
			{
				this.StateChanged = null;
			}
		}, CancellationToken.None);
	}

	public async Task Connect(CancellationToken cancellationToken = default(CancellationToken))
	{
		try
		{
			await Connect2(cancellationToken);
		}
		catch (Exception lastException)
		{
			LastException = lastException;
			await DisposeAsync();
			throw;
		}
	}

	public async Task Connect2(CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(VhLogger.FormatType(this));
		}
		using (VhLogger.Instance.BeginScope("Client"))
		{
			if (State != ClientState.None)
			{
				throw new Exception("Connection is already in progress.");
			}
			if (_vpnAdapter.IsStarted)
			{
				throw new InvalidOperationException("VpnAdapter should not be started before connect.");
			}
			State = ClientState.Connecting;
			_session = await new ClientSessionBuilder(_vpnAdapter, _socketFactory, Token, Config, Tracker, _serverFinder, ProxyEndPointManager, _domainFilteringService, _netFilter, _staticIpFilter, ChannelProtocol, delegate(ClientState state)
			{
				State = state;
			}).Build(_cancellationTokenSource.Token, cancellationToken).Vhc();
			_session.StateChanged += Session_StateChanged;
			await _session.Start(cancellationToken);
			State = ClientState.Connected;
		}
	}

	private void Session_StateChanged(object? sender, EventArgs e)
	{
		if (_session == null)
		{
			throw new InvalidOperationException("How a null session session can fire an event!");
		}
		ClientSession session = _session;
		if (session != null && session.State == ClientState.Disposed)
		{
			Dispose();
		}
		State = _session.State;
	}

	public Task UpdateSessionStatus(CancellationToken cancellationToken)
	{
		return _session?.UpdateStatus(cancellationToken) ?? Task.CompletedTask;
	}

	public async ValueTask DisposeAsync()
	{
		VhLogger.Instance.LogInformation("Client is shutting down asynchronously...");
		if (_session != null)
		{
			await _cancellationTokenSource.TryCancelAsync();
			await _session.DisposeAsync();
		}
		Dispose();
	}

	public void Dispose()
	{
		lock (_disposeLock)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
		}
		VhLogger.Instance.LogInformation("Client is shutting down...");
		_cancellationTokenSource.TryCancel();
		_cancellationTokenSource.Dispose();
		_session?.Dispose();
		ClientSession? session = _session;
		if (session != null)
		{
			session.StateChanged -= Session_StateChanged;
		}
		_netFilter.Dispose();
		VhLogger.Instance.LogDebug("Disposing ConnectorService...");
		VhLogger.Instance.LogDebug("Disposing ProxyEndPointManager...");
		ProxyEndPointManager.Dispose();
		if (Config.AutoDisposeVpnAdapter)
		{
			VhLogger.Instance.LogDebug("Disposing Adapter...");
			_vpnAdapter.Dispose();
		}
		State = ClientState.Disposed;
		VhLogger.Instance.LogInformation("Bye Bye!");
	}
}
