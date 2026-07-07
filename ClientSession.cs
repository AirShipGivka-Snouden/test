using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Client.Abstractions;
using VpnHood.Core.Client.ConnectorServices;
using VpnHood.Core.Common.Exceptions;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Filtering.Abstractions;
using VpnHood.Core.Filtering.DomainFiltering;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Jobs;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Sockets;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling;
using VpnHood.Core.Tunneling.Channels;
using VpnHood.Core.Tunneling.Connections;
using VpnHood.Core.Tunneling.Exceptions;
using VpnHood.Core.Tunneling.Messaging;
using VpnHood.Core.Tunneling.Proxies;
using VpnHood.Core.Tunneling.Utils;
using VpnHood.Core.VpnAdapters.Abstractions;

namespace VpnHood.Core.Client;

internal class ClientSession : IClientSession, IDisposable, IAsyncDisposable
{
	private readonly ISocketFactory _socketFactory;

	private readonly IVpnAdapter _vpnAdapter;

	private readonly NetFilter _netFilter;

	private readonly Tunnel _tunnel;

	private readonly ClientUsageTracker? _clientUsageTracker;

	private readonly ProxyManager _proxyManager;

	private readonly ClientPacketHandler _packetHandler;

	private readonly IClientTcpHost _clientTcpHost;

	private readonly RequestSender _requestSender;

	private readonly ClientSessionStatus _status;

	private readonly Job _cleanupJob;

	private readonly CancellationTokenSource _cancellationTokenSource;

	private readonly AsyncLock _disposeLock = new AsyncLock();

	private readonly AsyncLock _packetChannelLock = new AsyncLock();

	private readonly DomainFilteringService _domainFilteringService;

	private DateTime? _autoWaitTime;

	private ClientUdpChannelTransmitter? _udpTransmitter;

	private bool _disposed;

	private ChannelProtocol _channelProtocol;

	private ChannelProtocol _oldChannelProtocol;

	private bool _useTcpProxy;

	private bool _dropQuic;

	private bool _dropUdp;

	private DateTime? _lastConnectionErrorTime;

	[CompilerGenerated]
	private ClientState _003CState_003Ek__BackingField;

	public ISessionStatus Status => _status;

	public ClientSessionConfig Config { get; }

	public SessionInfo Info { get; }

	public ISessionAdHandler AdHandler { get; }

	public Exception? LastException { get; private set; }

	public int CreatedPacketChannelCount { get; private set; }

	internal bool IsAdapterStarted => _vpnAdapter.IsStarted;

	internal PassthroughState PassthroughState { get; } = new PassthroughState();

	private bool ShouldManagePacketChannels => _tunnel.PacketChannelCount < _tunnel.MaxPacketChannelCount;

	public ClientState State
	{
		get
		{
			ClientState clientState = _003CState_003Ek__BackingField;
			if ((uint)(clientState - 10) <= 1u)
			{
				return _003CState_003Ek__BackingField;
			}
			if (AdHandler.IsWaitingForAd)
			{
				return ClientState.WaitingForAd;
			}
			return _003CState_003Ek__BackingField;
		}
		private set
		{
			if (_003CState_003Ek__BackingField != value)
			{
				_003CState_003Ek__BackingField = value;
				this.StateChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public bool DropUdp
	{
		get
		{
			return _dropUdp;
		}
		set
		{
			if (_dropUdp != value)
			{
				_dropUdp = value;
				UpdateConfig();
			}
		}
	}

	public bool DropQuic
	{
		get
		{
			return _dropQuic;
		}
		set
		{
			if (_dropQuic != value)
			{
				_dropQuic = value;
				UpdateConfig();
			}
		}
	}

	public bool UseTcpProxy
	{
		get
		{
			return _useTcpProxy;
		}
		set
		{
			if (_useTcpProxy != value)
			{
				_useTcpProxy = value;
				UpdateConfig();
			}
		}
	}

	public ChannelProtocol ChannelProtocol
	{
		get
		{
			return _channelProtocol;
		}
		set
		{
			if (_channelProtocol != value)
			{
				_channelProtocol = ChannelProtocolValidator.Validate(value, Info);
				UpdateConfig();
			}
		}
	}

	public event EventHandler? StateChanged;

	public ClientSession(ClientSessionOptions options, ClientSessionConfig config)
	{
		_netFilter = options.NetFilter;
		_requestSender = options.RequestSender;
		_channelProtocol = options.ChannelProtocol;
		_oldChannelProtocol = options.ChannelProtocol;
		_useTcpProxy = options.UseTcpProxy;
		_dropQuic = options.DropQuic;
		_dropUdp = options.DropUdp;
		_socketFactory = options.SocketFactory;
		_domainFilteringService = options.DomainFilteringService;
		Config = config;
		Info = options.SessionInfo;
		_vpnAdapter = options.VpnAdapter;
		_vpnAdapter.PrimaryAdapterIpChanged += VpnAdapter_PrimaryAdapterIpChanged;
		_vpnAdapter.PacketReceived += VpnAdapter_PacketReceived;
		_tunnel = new Tunnel(new TunnelOptions
		{
			AutoDisposePackets = true,
			PacketQueueCapacity = 200,
			MaxPacketChannelCount = ((_channelProtocol == ChannelProtocol.Udp) ? 1 : Config.MaxPacketChannelCount),
			Mtu = config.Mtu
		});
		Traffic? maxSpeedMbps = config.MaxSpeedMbps;
		if (!maxSpeedMbps.HasValue || maxSpeedMbps.GetValueOrDefault().Sent <= 0)
		{
			maxSpeedMbps = config.MaxSpeedMbps;
			if (!maxSpeedMbps.HasValue || maxSpeedMbps.GetValueOrDefault().Received <= 0)
			{
				goto IL_01d0;
			}
		}
		_tunnel.TrafficMeter.MaxSpeed = new Traffic(config.MaxSpeedMbps.Value.Sent * 1000000 / 8, config.MaxSpeedMbps.Value.Received * 1000000 / 8);
		goto IL_01d0;
		IL_01d0:
		_tunnel.PacketReceived += Tunnel_PacketReceived;
		_proxyManager = new ProxyManager(_socketFactory, new ProxyManagerOptions
		{
			IsPingSupported = false,
			PacketProxyCallbacks = null,
			UdpTimeout = TunnelDefaults.UdpTimeout,
			MaxUdpClientCount = 500,
			MaxPingClientCount = 10,
			PacketQueueCapacity = 200,
			IcmpTimeout = TunnelDefaults.IcmpTimeout,
			UdpBufferSize = (Config.UdpProxyBufferSize ?? TunnelDefaults.ClientUdpProxyBufferSize),
			LogScope = null,
			UseUdpProxy2 = true,
			AutoDisposePackets = true
		});
		_proxyManager.PacketReceived += Proxy_PacketReceived;
		ulong sessionId = Config.SessionId;
		ReadOnlyMemory<byte> sessionKey = Config.SessionKey;
		DomainFilteringService domainFilteringService = options.DomainFilteringService;
		ClientStreamHandler streamHandler = new ClientStreamHandler(socketFactory: _socketFactory, domainFilterService: domainFilteringService, tunnel: _tunnel, tcpConnectTimeout: Config.TcpConnectTimeout, session: this, sessionId: sessionId, sessionKey: sessionKey, proxyManager: _proxyManager, netFilter: _netFilter, streamProxyBufferSize: Config.StreamProxyBufferSize, passthroughState: PassthroughState);
		IClientTcpHost clientTcpHost2;
		if (!options.UseOsTcpStack)
		{
			IClientTcpHost clientTcpHost = new ClientTcpHost(streamHandler);
			clientTcpHost2 = clientTcpHost;
		}
		else
		{
			IClientTcpHost clientTcpHost = new ClientTcpLocalHost(streamHandler, config.TcpProxyCatcherAddressIpV4, config.TcpProxyCatcherAddressIpV6);
			clientTcpHost2 = clientTcpHost;
		}
		_clientTcpHost = clientTcpHost2;
		_clientTcpHost.PacketReceived += ClientTcpHostPacketReceived;
		_packetHandler = new ClientPacketHandler(_tunnel, _clientTcpHost, options.DomainFilteringService, _netFilter, _proxyManager, Config.DnsConfig.DnsServers, Config.IsIpV6SupportedByServer, PassthroughState);
		_status = new ClientSessionStatus(this, _tunnel, domainFilteringService: options.DomainFilteringService, requestSender: _requestSender, proxyManager: _proxyManager, streamHandler: streamHandler, packetHandler: _packetHandler, accessUsage: options.AccessUsage);
		if (options.Tracker != null)
		{
			_clientUsageTracker = new ClientUsageTracker(_status, options.Tracker);
		}
		AdHandler = new SessionAdHandler(this);
		AdHandler.IsWaitingForChanged += delegate
		{
			this.StateChanged?.Invoke(this, EventArgs.Empty);
		};
		_cancellationTokenSource = new CancellationTokenSource();
		_cleanupJob = new Job(Cleanup, "VpnHoodClient");
		UpdateConfig();
	}

	public async Task Start(CancellationToken cancellationToken)
	{
		if (State != ClientState.None)
		{
			throw new InvalidOperationException("Client has been already started.");
		}
		State = ClientState.Connecting;
		_clientTcpHost.Start();
		ValueTask manageChannelsTask = ManagePacketChannels(cancellationToken);
		bool retryAd = false;
		if (Config.AdRequirement != AdRequirement.None)
		{
			retryAd = await AdHandler.TryWaitForAd(cancellationToken) != null;
		}
		await manageChannelsTask.Vhc();
		await _vpnAdapter.Start(Config.AdapterOptions, cancellationToken);
		if (retryAd)
		{
			Exception ex = await AdHandler.TryWaitForAd(cancellationToken);
			if (ex != null && Config.AdRequirement != AdRequirement.Flexible)
			{
				throw ex;
			}
		}
		State = ClientState.Connected;
	}

	private bool CalcUseTcpProxy()
	{
		if (!Config.IsTcpProxySupported)
		{
			return false;
		}
		SessionInfo info = Info;
		if (info != null && !info.IsTcpProxySupported)
		{
			return false;
		}
		info = Info;
		if (info != null && !info.IsTcpPacketSupported)
		{
			return true;
		}
		if (_domainFilteringService.IsEnabled)
		{
			return true;
		}
		return _useTcpProxy;
	}

	private void UpdateConfig()
	{
		bool flag = CalcUseTcpProxy();
		bool flag2 = _dropQuic && flag;
		bool flag3 = _dropUdp && flag;
		_channelProtocol = ChannelProtocolValidator.Validate(_channelProtocol, Info);
		if (_channelProtocol != _oldChannelProtocol)
		{
			VhLogger.Instance.LogInformation("VpnProtocol is changed to {VpnProtocol}.", _channelProtocol);
			_tunnel.MaxPacketChannelCount = ((_channelProtocol == ChannelProtocol.Udp) ? 1 : Config.MaxPacketChannelCount);
			_oldChannelProtocol = _channelProtocol;
			_requestSender.ConnectorService.UseQuic = _channelProtocol == ChannelProtocol.Quic && Config.HostQuicEndPoint != null;
			_requestSender.ConnectorService.QuicEndPoint = Config.HostQuicEndPoint;
			_tunnel.RemoveChannels<IPacketChannel>();
			Task.Run(() => ManagePacketChannels(_cancellationTokenSource.Token));
		}
		if (flag != _useTcpProxy)
		{
			VhLogger.Instance.LogWarning("UseTcpProxy is changed to {UseTcpProxy} because of config or capability change.", _packetHandler.UseTcpProxy);
		}
		if (flag2 != _dropQuic)
		{
			VhLogger.Instance.LogWarning("DropQuic is changed to {DropQuic} because client can not use TcpProxy.", _packetHandler.DropQuic);
		}
		if (flag3 != _dropUdp)
		{
			VhLogger.Instance.LogWarning("DropUdp is changed to {DropUdp} because client can not use TcpProxy.", flag3);
		}
		_packetHandler.UseTcpProxy = flag;
		_packetHandler.DropQuic = flag2;
		_packetHandler.DropUdp = flag3;
		_dropQuic = flag2;
		_dropUdp = flag3;
		bool flag4 = _vpnAdapter.IsIpVersionSupported(IpVersion.IPv6);
		_packetHandler.IsIpV6SupportedByClient = flag4;
		_proxyManager.IsIpV6Supported = flag4;
	}

	private void VpnAdapter_PrimaryAdapterIpChanged(object? sender, EventArgs e)
	{
		UpdateConfig();
	}

	private void VpnAdapter_PacketReceived(object? sender, IpPacket ipPacket)
	{
		if (_disposed)
		{
			return;
		}
		if (_autoWaitTime.HasValue)
		{
			if (FastDateTime.Now - _autoWaitTime.Value < Config.AutoWaitTimeout)
			{
				throw new PacketDropException("Connection is paused. The packet has been dropped.");
			}
			_autoWaitTime = null;
			State = ClientState.Unstable;
		}
		if (ShouldManagePacketChannels && !_packetChannelLock.IsLocked)
		{
			ManagePacketChannels(_cancellationTokenSource.Token);
		}
		_packetHandler.ProcessOutgoingPacket(ipPacket);
	}

	private void ClientTcpHostPacketReceived(object? sender, IpPacket ipPacket)
	{
		ProcessIncomingPacket(ipPacket, useMapper: true);
	}

	private void Proxy_PacketReceived(object? sender, IpPacket ipPacket)
	{
		ProcessIncomingPacket(ipPacket, useMapper: true);
	}

	private void Tunnel_PacketReceived(object? sender, IpPacket ipPacket)
	{
		ProcessIncomingPacket(ipPacket, useMapper: false);
	}

	private void ProcessIncomingPacket(IpPacket ipPacket, bool useMapper)
	{
		if (useMapper)
		{
			IIpMapper? ipMapper = _netFilter.IpMapper;
			if (ipMapper != null && ipMapper.FromHost(ipPacket.Protocol, ipPacket.GetSourceEndPoint(), out var newEndPoint))
			{
				ipPacket.SetSourceEndPoint(newEndPoint);
				ipPacket.UpdateAllChecksums();
			}
		}
		_vpnAdapter.SendPacketQueued(ipPacket);
	}

	private async ValueTask ManagePacketChannels(CancellationToken cancellationToken)
	{
		using AsyncLock.ILockAsyncResult lockResult = await _packetChannelLock.LockAsync(TimeSpan.Zero, cancellationToken);
		if (!lockResult.Succeeded)
		{
			return;
		}
		try
		{
			if (ShouldManagePacketChannels)
			{
				if (_channelProtocol == ChannelProtocol.Udp)
				{
					AddUdpChannel();
				}
				else
				{
					await AddTcpPacketChannel(cancellationToken).Vhc();
				}
			}
		}
		catch (Exception ex)
		{
			if (!_disposed)
			{
				VhLogger.LogError(GeneralEventId.PacketChannel, ex, "Could not Manage PacketChannels.");
			}
		}
	}

	private async Task AddTcpPacketChannel(CancellationToken cancellationToken)
	{
		string text = UniqueIdFactory.Create();
		TcpPacketChannelRequest request = new TcpPacketChannelRequest
		{
			RequestId = text,
			ChannelId = text,
			SessionId = Config.SessionId,
			SessionKey = Config.SessionKey,
			ActiveChannelIds = _tunnel.PacketChannels.Select((IPacketChannel c) => c.ChannelId).ToArray()
		};
		ConnectorRequestResult<SessionResponse> connectorRequestResult = await SendRequest<SessionResponse>(request, cancellationToken).Vhc();
		try
		{
			TimeSpan? lifespan = (VhUtils.IsInfinite(Config.MaxPacketChannelLifespan) ? ((TimeSpan?)null) : new TimeSpan?(TimeSpan.FromSeconds(new Random().Next((int)Config.MinPacketChannelLifespan.TotalSeconds, (int)Config.MaxPacketChannelLifespan.TotalSeconds))));
			if (lifespan.HasValue)
			{
				connectorRequestResult.StreamConnection.PreventReuse();
			}
			StreamPacketChannel channel = new StreamPacketChannel(new StreamPacketChannelOptions
			{
				StreamConnection = connectorRequestResult.StreamConnection,
				BufferSize = TunnelDefaults.ConnectionPacketBufferSize,
				ChannelId = request.ChannelId,
				RequestTime = request.RequestTime,
				Blocking = true,
				AutoDisposePackets = true,
				Lifespan = lifespan,
				TrafficMeter = _tunnel.TrafficMeter
			});
			_tunnel.AddChannel(channel, disposeIfFailed: true);
			CreatedPacketChannelCount++;
		}
		catch
		{
			connectorRequestResult.Dispose();
			throw;
		}
	}

	private void AddUdpChannel()
	{
		if (Config.SessionKey.IsEmpty)
		{
			throw new Exception("Server UdpKey has not been set.");
		}
		if (Config.HostUdpEndPoint == null)
		{
			throw new Exception("Server does not serve any UDP endpoint.");
		}
		if (_udpTransmitter == null)
		{
			_udpTransmitter = new ClientUdpChannelTransmitter(_socketFactory, Config.SessionId, Config.SessionKey.Span, Config.HostUdpEndPoint, TunnelDefaults.ClientUdpChannelBufferSize);
		}
		UdpChannel udpChannel = new UdpChannel(_udpTransmitter.UdpTransport, new UdpChannelOptions
		{
			AutoDisposePackets = true,
			LeaveUdpTransportOpen = true,
			Blocking = true,
			ChannelId = Guid.NewGuid().ToString(),
			Lifespan = null,
			TrafficMeter = _tunnel.TrafficMeter
		});
		try
		{
			_tunnel.AddChannel(udpChannel);
		}
		catch
		{
			udpChannel.Dispose();
			throw;
		}
	}

	internal Task<ConnectorRequestResult<T>> SendRequest<T>(ClientRequest request, CancellationToken cancellationToken) where T : SessionResponse
	{
		return SendRequest<T>(new ClientRequestEx
		{
			Request = request
		}, cancellationToken);
	}

	internal async Task<ConnectorRequestResult<T>> SendRequest<T>(ClientRequestEx request, CancellationToken cancellationToken) where T : SessionResponse
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		try
		{
			ConnectorRequestResult<T> connectorRequestResult = await _requestSender.SendRequest<T>(request, cancellationToken).Vhc();
			_status.Update(connectorRequestResult.Response.AccessUsage);
			if (_disposed)
			{
				connectorRequestResult.Dispose();
				ObjectDisposedException.ThrowIf(_disposed, this);
			}
			_lastConnectionErrorTime = null;
			State = ClientState.Connected;
			return connectorRequestResult;
		}
		catch (SessionException ex)
		{
			_status.Update(ex.SessionResponse.AccessUsage);
			_lastConnectionErrorTime = null;
			if (ex.SessionResponse.ErrorCode != SessionErrorCode.GeneralError && ex.SessionResponse.ErrorCode != SessionErrorCode.RewardedAdRejected)
			{
				await DisposeAsync(ex);
			}
			throw;
		}
		catch (Exception ex2)
		{
			if (!_disposed)
			{
				DateTime now = FastDateTime.Now;
				_lastConnectionErrorTime.GetValueOrDefault();
				if (!_lastConnectionErrorTime.HasValue)
				{
					DateTime value = now;
					_lastConnectionErrorTime = value;
				}
				if (now - _lastConnectionErrorTime.Value > Config.SessionTimeout)
				{
					await DisposeAsync(ex2);
				}
				else if (now - _lastConnectionErrorTime.Value > Config.UnstableTimeout)
				{
					_autoWaitTime = now;
					_status.WaitingCount++;
					State = ClientState.Waiting;
					VhLogger.Instance.LogWarning(ex2, "Client is paused because of too many connection errors.");
				}
				else if (State == ClientState.Connected)
				{
					_status.UnstableCount++;
					State = ClientState.Unstable;
				}
				throw;
			}
			throw;
		}
	}

	private ValueTask Cleanup(CancellationToken cancellationToken)
	{
		DateTime utcNow = FastDateTime.UtcNow;
		DateTime? sessionExpirationTime = _status.SessionExpirationTime;
		if (utcNow > sessionExpirationTime)
		{
			SessionException ex = new SessionException(SessionErrorCode.SessionExpired);
			VhLogger.Instance.LogError(GeneralEventId.Session, ex, "Session has been expired.");
			return DisposeAsync(ex);
		}
		return default(ValueTask);
	}

	public void DropCurrentConnections()
	{
		_clientTcpHost.DropCurrentConnections();
		_tunnel.RemoveChannels<IProxyChannel>();
	}

	public async Task UpdateStatus(CancellationToken cancellationToken)
	{
		using (await SendRequest<SessionResponse>(new SessionStatusRequest
		{
			RequestId = UniqueIdFactory.Create(),
			SessionId = Config.SessionId,
			SessionKey = Config.SessionKey
		}, cancellationToken).Vhc())
		{
		}
	}

	private ValueTask DisposeAsync(Exception ex)
	{
		if (_disposed || _disposeLock.IsLocked)
		{
			return ValueTask.CompletedTask;
		}
		VhLogger.Instance.LogDebug(ex, "Session is closing due to an error.");
		LastException = ex;
		return DisposeAsync();
	}

	public async ValueTask DisposeAsync()
	{
		using (await _disposeLock.LockAsync())
		{
			if (_disposed)
			{
				return;
			}
			VhLogger.Instance.LogInformation("Session is closing...");
			State = ClientState.Disconnecting;
			if (_vpnAdapter.IsStarted)
			{
				VhUtils.TryInvoke("Stop the VpnAdapter", delegate
				{
					_vpnAdapter.Stop();
				});
			}
			if (LastException == null)
			{
				VhLogger.Instance.LogInformation("Sending bye to the server...");
				try
				{
					TimeSpan delay = TunnelDefaults.ByeTimeout.WhenNoDebugger();
					using CancellationTokenSource byteCts = new CancellationTokenSource(delay);
					using ConnectorRequestResult<SessionResponse> connectorRequestResult = await _requestSender.SendRequest<SessionResponse>(new ByeRequest
					{
						RequestId = UniqueIdFactory.Create(),
						SessionId = Config.SessionId,
						SessionKey = Config.SessionKey
					}, byteCts.Token).Vhc();
					connectorRequestResult.StreamConnection.PreventReuse();
					connectorRequestResult.StreamConnection.Dispose();
					VhLogger.Instance.LogInformation("Session has been closed on the server successfully.");
				}
				catch (Exception exception)
				{
					VhLogger.Instance.LogDebug(GeneralEventId.Request, exception, "Could not send the bye to the server..");
				}
			}
			await _cancellationTokenSource.TryCancelAsync();
			Dispose();
		}
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
		State = ClientState.Disconnecting;
		_cancellationTokenSource.TryCancel();
		_cancellationTokenSource.Dispose();
		if (_vpnAdapter.IsStarted)
		{
			VhUtils.TryInvoke("Stop the VpnAdapter", delegate
			{
				_vpnAdapter.Stop();
			});
		}
		_vpnAdapter.PacketReceived -= VpnAdapter_PacketReceived;
		_vpnAdapter.PrimaryAdapterIpChanged -= VpnAdapter_PrimaryAdapterIpChanged;
		_tunnel.PacketReceived -= Tunnel_PacketReceived;
		_clientTcpHost.PacketReceived -= ClientTcpHostPacketReceived;
		_proxyManager.PacketReceived -= Proxy_PacketReceived;
		PassthroughState.PassthroughForAd = false;
		_requestSender.ConnectorService.AllowChannelReuse = false;
		VhLogger.Instance.LogDebug("Disposing ClientHost...");
		_clientTcpHost.Dispose();
		VhLogger.Instance.LogDebug("Disposing Tunnel...");
		_tunnel.Dispose();
		VhLogger.Instance.LogDebug("Disposing UdpTransmitter...");
		_udpTransmitter?.Dispose();
		VhLogger.Instance.LogDebug("Disposing ProxyManager...");
		_proxyManager.Dispose();
		VhLogger.Instance.LogDebug("Disposing ConnectorService...");
		_requestSender.Dispose();
		_cleanupJob.Dispose();
		_clientUsageTracker?.Dispose();
		AdHandler.Dispose();
		State = ClientState.Disposed;
	}
}
