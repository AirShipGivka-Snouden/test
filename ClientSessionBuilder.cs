using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Ga4.Trackers;
using Ga4.Trackers.Ga4Tags;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Client.Abstractions;
using VpnHood.Core.Client.Abstractions.Exceptions;
using VpnHood.Core.Client.ConnectorServices;
using VpnHood.Core.Client.Exceptions;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Common.Tokens;
using VpnHood.Core.Filtering.Abstractions;
using VpnHood.Core.Filtering.DomainFiltering;
using VpnHood.Core.Proxies.EndPointManagement;
using VpnHood.Core.Proxies.EndPointManagement.Abstractions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Sockets;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling;
using VpnHood.Core.Tunneling.Connections;
using VpnHood.Core.Tunneling.Messaging;
using VpnHood.Core.Tunneling.Utils;
using VpnHood.Core.VpnAdapters.Abstractions;

namespace VpnHood.Core.Client;

internal class ClientSessionBuilder(IVpnAdapter vpnAdapter, ISocketFactory socketFactory, Token token, VpnHoodClientConfig config, ITracker? tracker, ServerFinder serverFinder, ProxyEndPointManager proxyEndPointManager, DomainFilteringService domainFilteringService, NetFilter netFilter, StaticIpFilter staticIpFilter, ChannelProtocol channelProtocol, Action<ClientState> setState)
{
	public async Task<ClientSession> Build(CancellationToken disposeCancellationToken, CancellationToken cancellationToken = default(CancellationToken))
	{
		using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(disposeCancellationToken, cancellationToken);
		setState(ClientState.Connecting);
		ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
		VhLogger.Instance.LogInformation("DropUdp: {DropUdp}, VpnProtocol: {VpnProtocol}, IncludeLocalNetwork: {IncludeLocalNetwork}, MinWorkerThreads: {WorkerThreads}, CompletionPortThreads: {CompletionPortThreads}, ClientIpV6: {ClientIpV6}, ProcessId: {ProcessId}", config.DropUdp, channelProtocol, config.IncludeLocalNetwork, workerThreads, completionPortThreads, vpnAdapter.IsIpVersionSupported(IpVersion.IPv6), Process.GetCurrentProcess().Id);
		VhLogger.Instance.LogInformation("ClientVersion: {ClientVersion}, ClientMinProtocolVersion: {ClientMinProtocolVersion}, ClientMaxProtocolVersion: {ClientMaxProtocolVersion}, ClientId: {ClientId}", config.Version, VpnHoodClientConfig.MinProtocolVersion, VpnHoodClientConfig.MaxProtocolVersion, VhLogger.FormatId(config.ClientId));
		if (proxyEndPointManager.IsEnabled)
		{
			setState(ClientState.ValidatingProxies);
			await proxyEndPointManager.CheckServers(linkedCts.Token).Vhc();
			VhLogger.Instance.LogInformation("Proxy servers: {Count}", proxyEndPointManager.Status.ProxyEndPointInfos.Count((ProxyEndPointInfo x) => x.Status.ErrorMessage == null));
			if (!proxyEndPointManager.Status.IsAnySucceeded)
			{
				throw new UnreachableProxyServerException();
			}
		}
		setState(ClientState.FindingReachableServer);
		VpnEndPoint vpnEndPoint = await serverFinder.FindReachableServerAsync(new global::_003C_003Ez__ReadOnlySingleElementList<ServerToken>(token.ServerToken), linkedCts.Token).Vhc();
		bool allowRedirect = !serverFinder.CustomServerEndpoints.Any();
		return await Connect(vpnEndPoint, allowRedirect, linkedCts.Token).Vhc();
	}

	private async Task<ClientSession> Connect(VpnEndPoint vpnEndPoint, bool allowRedirect, CancellationToken cancellationToken)
	{
		RequestSender requestSender = null;
		try
		{
			VhLogger.Instance.LogInformation("Connecting to the server... EndPoint: {hostEndPoint}", VhLogger.Format(vpnEndPoint.TcpEndPoint));
			setState(ClientState.Connecting);
			ConnectorService connectorService = new ConnectorService(new ConnectorServiceOptions
			{
				ProxyEndPointManager = proxyEndPointManager,
				SocketFactory = socketFactory,
				VpnEndPoint = vpnEndPoint,
				RequestTimeout = config.TcpConnectTimeout,
				AllowChannelReuse = false
			});
			requestSender = new RequestSender(connectorService);
			ClientInfo clientInfo = new ClientInfo
			{
				ClientId = config.ClientId,
				ClientVersion = config.Version.ToString(3),
				MinProtocolVersion = requestSender.ConnectorService.ProtocolVersion,
				MaxProtocolVersion = VpnHoodClientConfig.MaxProtocolVersion,
				UserAgent = config.UserAgent
			};
			HelloRequest helloRequest = new HelloRequest
			{
				RequestId = UniqueIdFactory.Create(),
				EncryptedClientId = VhUtils.EncryptClientId(clientInfo.ClientId, token.Secret),
				ClientInfo = clientInfo,
				TokenId = token.TokenId,
				ServerLocation = serverFinder.ServerLocation,
				PlanId = config.PlanId,
				AccessCode = config.AccessCode,
				AllowRedirect = allowRedirect,
				IsIpV6Supported = vpnAdapter.IsIpVersionSupported(IpVersion.IPv6),
				UserReview = config.UserReview,
				Mtu = 1400
			};
			using ConnectorRequestResult<HelloResponse> connectorRequestResult = await requestSender.SendRequest<HelloResponse>(helloRequest, cancellationToken).Vhc();
			connectorRequestResult.StreamConnection.PreventReuse();
			connectorService.AllowChannelReuse = config.AllowStreamReuse;
			HelloResponse response = connectorRequestResult.Response;
			if (response.ClientPublicAddress == null)
			{
				throw new NotSupportedException("Server must returns ClientPublicAddress.");
			}
			IpRangeOrderedList ipRangeOrderedList = response.IncludeIpRanges?.ToOrderedList() ?? IpNetwork.All.ToIpRanges();
			IpRangeOrderedList ipRangeOrderedList2 = response.VpnAdapterIncludeIpRanges?.ToOrderedList() ?? IpNetwork.All.ToIpRanges();
			IpRangeOrderedList ipRanges = ipRangeOrderedList.Intersect(ipRangeOrderedList2);
			IpRangeOrderedList source = IpNetwork.LocalNetworks.ToIpRanges().Intersect(ipRanges);
			VhLogger.Instance.LogInformation(GeneralEventId.Session, "Hurray! Client has been connected! SessionId: " + VhLogger.FormatId(response.SessionId) + ", ServerVersion: " + response.ServerVersion + ", " + $"ProtocolVersion: {response.ProtocolVersion}, " + $"CurrentProtocolVersion: {connectorService.ProtocolVersion}, " + "ClientIp: " + VhLogger.Format(response.ClientPublicAddress) + ", " + $"UdpPort: {response.UdpPort}, " + $"QuicPort: {response.QuicPort}, " + $"IsTcpPacketSupported: {response.IsTcpPacketSupported}, " + $"IsTcpProxySupported: {response.IsTcpProxySupported}, " + $"IsLocalNetworkAllowed: {source.Any()}, " + $"NetworkV4: {response.VirtualIpNetworkV4}, " + $"NetworkV6: {response.VirtualIpNetworkV6}, " + "ClientCountry: " + response.ClientCountry + ", " + $"MaxSpeedMbps: {response.AccessInfo?.MaxSpeedMbps}");
			ulong sessionId = response.SessionId;
			byte[] sessionKey = response.SessionKey;
			object obj;
			if (response != null)
			{
				int? udpPort = response.UdpPort;
				if (udpPort.HasValue && udpPort.GetValueOrDefault() > 0 && response.ProtocolVersion >= 11)
				{
					obj = new IPEndPoint(connectorService.VpnEndPoint.TcpEndPoint.Address, response.UdpPort.Value);
					goto IL_0654;
				}
			}
			obj = null;
			goto IL_0654;
			IL_0654:
			IPEndPoint iPEndPoint = (IPEndPoint)obj;
			IPEndPoint iPEndPoint2 = ((response.QuicPort > 0) ? new IPEndPoint(connectorService.VpnEndPoint.TcpEndPoint.Address, response.QuicPort.Value) : null);
			connectorService.Init(response.ProtocolVersion, response.ServerSecret, response.ChannelIdleTimeout, config.UseWebSocket, response.RequestTimeout.WhenNoDebugger(), channelProtocol == ChannelProtocol.Quic && iPEndPoint2 != null, iPEndPoint2);
			staticIpFilter.IncludeRanges = config.IncludeIpRangesByApp.ToOrderedList();
			staticIpFilter.BlockedRanges = config.BlockIpRangesByApp.ToOrderedList();
			DnsConfig dnsServers = ClientHelper.GetDnsServers(config.DnsServers, response.DnsServers ?? Array.Empty<IPAddress>(), ipRangeOrderedList, staticIpFilter);
			IpRangeOrderedList includeIpRanges = ipRangeOrderedList2.Intersect(config.IncludeIpRangesByDevice);
			bool canProtectSocket = vpnAdapter.CanProtectSocket;
			bool includeLocalNetwork = config.IncludeLocalNetwork;
			IReadOnlyList<IPAddress> catcherIps;
			if (!config.UseOsTcpStack)
			{
				IReadOnlyList<IPAddress> readOnlyList = Array.Empty<IPAddress>();
				catcherIps = readOnlyList;
			}
			else
			{
				IReadOnlyList<IPAddress> readOnlyList = new global::_003C_003Ez__ReadOnlyArray<IPAddress>(new IPAddress[2] { config.TcpProxyCatcherAddressIpV4, config.TcpProxyCatcherAddressIpV6 });
				catcherIps = readOnlyList;
			}
			IpRangeOrderedList ipRangeOrderedList3 = ClientHelper.BuildIncludeIpRangesByDevice(includeIpRanges, catcherIps, canProtectSocket, includeLocalNetwork, connectorService.VpnEndPoint.TcpEndPoint.Address);
			staticIpFilter.IncludeRanges = staticIpFilter.IncludeRanges.Intersect(ipRanges).Intersect(ipRangeOrderedList3);
			if (response.SuppressedTo == SessionSuppressType.YourSelf)
			{
				VhLogger.Instance.LogWarning("You suppressed a session of yourself!");
			}
			else if (response.SuppressedTo == SessionSuppressType.Other)
			{
				VhLogger.Instance.LogWarning("You suppressed a session of another client!");
			}
			if (iPEndPoint == null)
			{
				VhLogger.Instance.LogWarning("The server does not support UDP channel.");
			}
			if (response != null && !response.IsTcpPacketSupported && !response.IsTcpProxySupported)
			{
				throw new NotSupportedException("The server does not support any protocol to support TCP. Please contact support.");
			}
			if (!response.IsTcpPacketSupported && !config.IsTcpProxySupported)
			{
				throw new NotSupportedException("The server does not support any protocol to support your client. Please contact support.");
			}
			if (!response.IsTcpPacketSupported && !config.UseTcpProxy)
			{
				VhLogger.Instance.LogWarning("TCP Proxy enabled because the server does not support TCP packets.");
			}
			if (!response.IsTcpProxySupported && config.UseTcpProxy)
			{
				VhLogger.Instance.LogWarning("TCP Proxy disabled because the server does not support it.");
			}
			SessionInfo sessionInfo = new SessionInfo
			{
				SessionId = response.SessionId.ToString(),
				ClientPublicIpAddress = response.ClientPublicAddress,
				ClientCountry = response.ClientCountry,
				AccessInfo = (response.AccessInfo ?? new AccessInfo()),
				IsLocalNetworkAllowed = source.Any(),
				DnsConfig = dnsServers,
				IsPremiumSession = (response.AccessUsage?.IsPremium ?? false),
				IsUdpChannelSupported = (iPEndPoint != null),
				AccessKey = response.AccessKey,
				ServerVersion = Version.Parse(response.ServerVersion),
				SuppressedTo = response.SuppressedTo,
				AdRequirement = response.AdRequirement,
				CreatedTime = DateTime.UtcNow,
				IsTcpPacketSupported = response.IsTcpPacketSupported,
				IsTcpProxySupported = response.IsTcpProxySupported,
				IsQuicChannelSupported = (iPEndPoint2 != null),
				ChannelProtocols = ChannelProtocolValidator.GetChannelProtocols(response),
				ServerLocationInfo = ((response.ServerLocation != null) ? ServerLocationInfo.Parse(response.ServerLocation) : null)
			};
			if (config.AllowAnonymousTracker)
			{
				if (!string.IsNullOrEmpty(response.GaMeasurementId))
				{
					new Ga4TagTracker
					{
						SessionCount = 1,
						MeasurementId = response.GaMeasurementId,
						ClientId = config.ClientId,
						SessionId = response.SessionId.ToString(),
						UserAgent = config.UserAgent,
						UserProperties = new Dictionary<string, object> { 
						{
							"client_version",
							config.Version.ToString(3)
						} }
					}.TryTrack(new Ga4TagEvent
					{
						EventName = "session_start"
					}, VhLogger.Instance);
				}
				if (tracker != null)
				{
					tracker.TryTrack(ClientTrackerBuilder.BuildConnectionSucceeded(serverFinder.ServerLocation, vpnAdapter.IsIpVersionSupported(IpVersion.IPv6), !allowRedirect, connectorService.VpnEndPoint.TcpEndPoint, null));
				}
			}
			IpNetwork virtualIpNetworkV = response.VirtualIpNetworkV4 ?? new IpNetwork(IPAddress.Parse("10.255.0.2"), 32);
			IpNetwork virtualIpNetworkV2 = response.VirtualIpNetworkV6 ?? new IpNetwork(IPAddressUtil.GenerateUlaAddress(4097), 128);
			VhLogger.Instance.LogInformation("Starting VpnAdapter... DnsServers: {DnsServers}, IncludeNetworks: {longIncludeNetworks}", sessionInfo.DnsConfig, VhLogger.Format(ipRangeOrderedList3.ToIpNetworks()));
			int num = Math.Min(helloRequest.Mtu, response.Mtu);
			if (num < 1000)
			{
				throw new InvalidOperationException($"The server MTU is too small. MTU: {num}");
			}
			VpnAdapterOptions adapterOptions = new VpnAdapterOptions
			{
				Mtu = num - 120,
				DnsServers = dnsServers.DnsServers,
				VirtualIpNetworkV4 = virtualIpNetworkV,
				VirtualIpNetworkV6 = virtualIpNetworkV2,
				IncludeNetworks = ipRangeOrderedList3.ToIpNetworks(),
				SessionName = config.SessionName,
				ExcludeApps = config.ExcludeApps,
				IncludeApps = config.IncludeApps
			};
			return new ClientSession(new ClientSessionOptions
			{
				SessionInfo = sessionInfo,
				VpnAdapter = vpnAdapter,
				SocketFactory = socketFactory,
				Tracker = tracker,
				AccessUsage = (response.AccessUsage ?? new AccessUsage()),
				RequestSender = requestSender,
				DomainFilteringService = domainFilteringService,
				NetFilter = netFilter,
				ChannelProtocol = channelProtocol,
				DropQuic = config.DropQuic,
				DropUdp = config.DropUdp,
				UseTcpProxy = config.UseTcpProxy,
				UseOsTcpStack = config.UseOsTcpStack
			}, new ClientSessionConfig
			{
				AdapterOptions = adapterOptions,
				SessionId = sessionId,
				SessionKey = sessionKey,
				TcpProxyCatcherAddressIpV4 = config.TcpProxyCatcherAddressIpV4,
				TcpProxyCatcherAddressIpV6 = config.TcpProxyCatcherAddressIpV6,
				Mtu = num,
				MaxSpeedMbps = response.AccessInfo?.MaxSpeedMbps,
				MaxPacketChannelLifespan = config.MaxPacketChannelLifespan,
				MinPacketChannelLifespan = config.MinPacketChannelLifespan,
				SessionTimeout = config.SessionTimeout,
				TcpConnectTimeout = config.TcpConnectTimeout,
				StreamProxyBufferSize = config.StreamProxyBufferSize,
				UdpProxyBufferSize = config.UdpProxyBufferSize,
				UnstableTimeout = config.UnstableTimeout,
				AutoWaitTimeout = config.AutoWaitTimeout,
				DnsConfig = dnsServers,
				IsTcpProxySupported = config.IsTcpProxySupported,
				HostTcpEndPoint = connectorService.VpnEndPoint.TcpEndPoint,
				HostUdpEndPoint = iPEndPoint,
				HostQuicEndPoint = iPEndPoint2,
				IsIpV6SupportedByServer = response.IsIpV6Supported,
				AdRequirement = response.AdRequirement,
				MaxPacketChannelCount = ((response.MaxPacketChannelCount != 0) ? Math.Min(config.MaxPacketChannelCount, response.MaxPacketChannelCount) : config.MaxPacketChannelCount)
			});
		}
		catch (TimeoutException)
		{
			requestSender?.Dispose();
			throw new ConnectionTimeoutException("Could not connect to the server in the given time.");
		}
		catch (RedirectHostException ex2)
		{
			requestSender?.Dispose();
			if (!allowRedirect)
			{
				VhLogger.Instance.LogError(ex2, "The server replies with a redirect to another server again. We already redirected earlier. This is unexpected.");
				throw;
			}
			setState(ClientState.FindingBestServer);
			ServerToken[] array = ex2.RedirectServerTokens;
			if (array == null)
			{
				ServerToken serverToken = JsonUtils.JsonClone(token.ServerToken);
				serverToken.HostEndPoints = ex2.RedirectHostEndPoints;
				array = new ServerToken[1] { serverToken };
			}
			return await Connect(await serverFinder.FindBestRedirectedServerAsync(array, cancellationToken).Vhc(), allowRedirect: false, cancellationToken).Vhc();
		}
		catch
		{
			requestSender?.Dispose();
			throw;
		}
	}
}
