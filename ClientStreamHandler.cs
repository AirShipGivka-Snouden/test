using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Client.ConnectorServices;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Filtering.Abstractions;
using VpnHood.Core.Filtering.DomainFiltering;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Sockets;
using VpnHood.Core.Toolkit.Streams;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling;
using VpnHood.Core.Tunneling.Channels;
using VpnHood.Core.Tunneling.Connections;
using VpnHood.Core.Tunneling.Exceptions;
using VpnHood.Core.Tunneling.Messaging;
using VpnHood.Core.Tunneling.Proxies;

namespace VpnHood.Core.Client;

internal class ClientStreamHandler(ClientSession session, ulong sessionId, ReadOnlyMemory<byte> sessionKey, ISocketFactory socketFactory, DomainFilteringService domainFilterService, Tunnel tunnel, ProxyManager proxyManager, TimeSpan tcpConnectTimeout, NetFilter netFilter, TransferBufferSize streamProxyBufferSize, PassthroughState passthroughState)
{
	private class ClientHostStat : IClientHostStat
	{
		public int TcpTunnelledCount { get; set; }

		public int TcpPassthruCount { get; set; }
	}

	private int _processingCount;

	private readonly ClientHostStat _stat = new ClientHostStat();

	public IClientHostStat Stat => _stat;

	public async Task ProcessConnection(IStreamConnection streamConnection, IPEndPoint hostEndPoint, CancellationToken cancellationToken)
	{
		_ = 2;
		try
		{
			Interlocked.Increment(ref _processingCount);
			cancellationToken.ThrowIfCancellationRequested();
			VhLogger.Instance.LogDebug(GeneralEventId.ProxyChannel, "New TcpProxy Request. LocalPort: {LocalPort}, HostEp: {RemoteEp}", streamConnection.RemoteEndPoint.Port, hostEndPoint);
			FilterAction filterAction = FilterAction.Default;
			if (domainFilterService.IsEnabled)
			{
				(streamConnection, filterAction) = await ApplySniFiltering(streamConnection, hostEndPoint, cancellationToken).Vhc();
			}
			if (filterAction == FilterAction.Default && netFilter.IpFilter != null)
			{
				filterAction = netFilter.IpFilter.Process(IpProtocol.Tcp, hostEndPoint.ToValue());
			}
			if (passthroughState.PassthroughForAd)
			{
				filterAction = FilterAction.Exclude;
			}
			switch (filterAction)
			{
			case FilterAction.Block:
				throw new NetFilterException("A host has been blocked.");
			case FilterAction.Include:
				VhLogger.Instance.LogDebug(GeneralEventId.ProxyChannel, "Include a Host to VPN. HostEp: {HostEp}", VhLogger.Format(hostEndPoint));
				await AddTunnelChannel(streamConnection, hostEndPoint, cancellationToken).Vhc();
				_stat.TcpTunnelledCount++;
				break;
			default:
				VhLogger.Instance.LogDebug(GeneralEventId.ProxyChannel, "Exclude a Host from VPN. HostEp: {HostEp}", VhLogger.Format(hostEndPoint));
				await AddPassthruChannel(streamConnection, hostEndPoint, cancellationToken).Vhc();
				_stat.TcpPassthruCount++;
				break;
			}
		}
		finally
		{
			Interlocked.Decrement(ref _processingCount);
		}
	}

	private async Task AddPassthruChannel(IStreamConnection streamConnection, IPEndPoint hostEndPoint, CancellationToken cancellationToken)
	{
		IpEndPointValue newEndPoint = default(IpEndPointValue);
		if (netFilter.IpMapper?.ToHost(IpProtocol.Tcp, hostEndPoint.ToValue(), out newEndPoint) ?? false)
		{
			hostEndPoint = newEndPoint.ToIPEndPoint();
		}
		if (hostEndPoint.IsV6() && !session.Status.IsIpV6SupportedByClient)
		{
			throw new Exception("IPv6 is not supported by client.");
		}
		VhLogger.Instance.LogDebug(GeneralEventId.ProxyChannel, "Adding a new stream channel to bypass. HostEp: {HostEp}, ConnectionId: {ConnectionId}", VhLogger.Format(hostEndPoint), streamConnection.ConnectionId);
		using CancellationTokenSource timeoutCts = new CancellationTokenSource(tcpConnectTimeout);
		using CancellationTokenSource connectCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
		TcpClient tcpClient = socketFactory.CreateTcpClient(hostEndPoint);
		await tcpClient.ConnectAsync(hostEndPoint.Address, hostEndPoint.Port, connectCts.Token).Vhc();
		TcpStreamConnection tcpStreamConnection = new TcpStreamConnection(tcpClient, "host", isServer: false, streamConnection.ConnectionId);
		try
		{
			ProxyChannel channel = new ProxyChannel(tcpStreamConnection.ToString(), streamConnection, tcpStreamConnection, streamProxyBufferSize);
			proxyManager.AddChannel(channel, disposeOnFail: true);
		}
		catch
		{
			await tcpStreamConnection.DisposeAsync();
			throw;
		}
	}

	private async Task AddTunnelChannel(IStreamConnection streamConnection, IPEndPoint hostEndPoint, CancellationToken cancellationToken)
	{
		if (hostEndPoint.IsV6() && !session.Status.IsIpV6SupportedByServer)
		{
			throw new Exception("IPv6 is not supported by server.");
		}
		VhLogger.Instance.LogDebug(GeneralEventId.ProxyChannel, "Adding a new stream channel to tunnel. HostEp: {HostEp}, ConnectionId: {ConnectionId}", VhLogger.Format(hostEndPoint), streamConnection.ConnectionId);
		StreamProxyChannelRequest request = new StreamProxyChannelRequest
		{
			RequestId = streamConnection.ConnectionId,
			SessionId = sessionId,
			SessionKey = sessionKey,
			DestinationEndPoint = hostEndPoint
		};
		ReadOnlyMemory<byte> initContents = await ReadInitContents(streamConnection.Stream, TimeSpan.FromMilliseconds(100L), cancellationToken);
		ClientRequestEx request2 = new ClientRequestEx
		{
			Request = request,
			PostBuffer = initContents
		};
		ConnectorRequestResult<SessionResponse> connectorRequestResult = await session.SendRequest<SessionResponse>(request2, cancellationToken).Vhc();
		try
		{
			IStreamConnection streamConnection2 = connectorRequestResult.StreamConnection;
			if (initContents.Length > 0)
			{
				tunnel.TrafficMeter.OnSent(initContents.Length);
			}
			ProxyChannel channel = new ProxyChannel(streamConnection2.ToString(), streamConnection, streamConnection2, streamProxyBufferSize, tunnel.TrafficMeter);
			tunnel.AddChannel(channel, disposeIfFailed: true);
		}
		catch
		{
			connectorRequestResult.Dispose();
			throw;
		}
	}

	private static async Task<ReadOnlyMemory<byte>> ReadInitContents(Stream stream, TimeSpan initTimeout, CancellationToken cancellationToken)
	{
		using CancellationTokenSource prefetchCts = new CancellationTokenSource(initTimeout);
		using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(prefetchCts.Token, cancellationToken);
		try
		{
			Memory<byte> memory = new Memory<byte>(new byte[4096]);
			return memory.Slice(0, await stream.ReadAsync(memory, linkedCts.Token));
		}
		catch (OperationCanceledException) when (prefetchCts.IsCancellationRequested)
		{
			return ReadOnlyMemory<byte>.Empty;
		}
	}

	private async Task<(IStreamConnection Connection, FilterAction filterAction)> ApplySniFiltering(IStreamConnection streamConnection, IPEndPoint hostEndPoint, CancellationToken cancellationToken)
	{
		StreamSniFilterResult streamSniFilterResult = await domainFilterService.ProcessStream(streamConnection.Stream, hostEndPoint, cancellationToken).Vhc();
		if (streamSniFilterResult.ReadData.Length > 0)
		{
			streamConnection = new StreamConnectionDecorator(streamConnection, new ReadBufferedStream(streamConnection.Stream, leaveOpen: false, streamSniFilterResult.ReadData.Span)
			{
				AllowBufferRefill = false
			});
		}
		return (Connection: streamConnection, filterAction: streamSniFilterResult.Action);
	}
}
