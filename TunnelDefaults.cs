using System;
using System.Net;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Tunneling;

public static class TunnelDefaults
{
	public const int MaxPacketSize = 1500;

	public const int MaxPacketChannelCount = 8;

	public const int MtuOverhead = 120;

	public const int MtuSafety = 100;

	public const int MtuServer = 1500;

	public const int MtuClient = 1400;

	public const string HttpPassCheck = "VpnHoodPassCheck";

	public const int StreamSmallReadCacheSize = 512;

	public const int ProxyPacketQueueCapacity = 200;

	public const int TunnelPacketQueueCapacity = 200;

	public const int MaxUdpClientCount = 500;

	public const int MaxPingClientCount = 10;

	public const int PrefetchStreamBufferSize = 4096;

	public static TransferBufferSize ClientStreamProxyBufferSize { get; } = new TransferBufferSize(8191, 8191);

	public static TransferBufferSize ServerStreamProxyBufferSize { get; } = new TransferBufferSize(8191, 8191);

	public static TransferBufferSize? ClientUdpProxyBufferSize { get; set; } = new TransferBufferSize(1048576, 1048576);

	public static TransferBufferSize? ServerUdpProxyBufferSize { get; set; } = null;

	public static TransferBufferSize ConnectionPacketBufferSize { get; } = new TransferBufferSize(262140, 262140);

	public static TransferBufferSize ServerStreamPacketBufferSize { get; } = new TransferBufferSize(16383, 16383);

	public static TransferBufferSize? ClientUdpChannelBufferSize { get; set; } = new TransferBufferSize(1048576, 1048576);

	public static TransferBufferSize? ServerUdpChannelBufferSize { get; set; } = null;

	public static TransferBufferSize? ServerTcpKernelBufferSize { get; set; } = null;

	public static TimeSpan PingTimeout { get; set; } = TimeSpan.FromSeconds(5L);

	public static TimeSpan UdpTimeout { get; set; } = TimeSpan.FromMinutes(2L);

	public static TimeSpan IcmpTimeout { get; set; } = TimeSpan.FromMinutes(1L);

	public static TimeSpan TcpCheckInterval { get; set; } = TimeSpan.FromMinutes(15L);

	public static TimeSpan TcpGracefulTimeout { get; set; } = TimeSpan.FromSeconds(15L);

	public static TimeSpan ByeTimeout { get; set; } = TimeSpan.FromSeconds(2L);

	public static TimeSpan ClientRequestTimeoutDelta { get; set; } = TimeSpan.FromSeconds(10L);

	public static IpNetwork VirtualIpNetworkV4 { get; } = new IpNetwork(IPAddress.Parse("10.240.0.1"), 12);

	public static IpNetwork VirtualIpNetworkV6 { get; } = new IpNetwork(IPAddress.Parse("fd12:2020::1"), 48);
}
