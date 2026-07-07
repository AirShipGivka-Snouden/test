using System;
using System.Net;
using System.Text.Json.Serialization;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Toolkit.Converters;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Tunneling.Messaging;

public class HelloResponse : SessionResponse
{
	[JsonConverter(typeof(ArrayConverter<IPAddress, IPAddressConverter>))]
	public IPAddress[]? DnsServers { get; set; }

	[JsonConverter(typeof(IpNetworkConverter))]
	public IpNetwork? VirtualIpNetworkV4 { get; init; }

	[JsonConverter(typeof(IpNetworkConverter))]
	public IpNetwork? VirtualIpNetworkV6 { get; init; }

	public int? UdpPort { get; set; }

	public int? QuicPort { get; set; }

	public string ServerVersion { get; set; }

	public int ProtocolVersion { get; set; }

	public byte[] ServerSecret { get; set; }

	public ulong SessionId { get; set; }

	public byte[] SessionKey { get; set; } = Array.Empty<byte>();

	public SessionSuppressType SuppressedTo { get; set; }

	public int MaxPacketChannelCount { get; set; }

	public bool IsIpV6Supported { get; set; }

	public IpRange[]? IncludeIpRanges { get; set; }

	public IpRange[]? VpnAdapterIncludeIpRanges { get; set; }

	public string? GaMeasurementId { get; init; }

	public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(60L);

	public TimeSpan ChannelIdleTimeout { get; init; } = TimeSpan.FromSeconds(60L);

	public AdRequirement AdRequirement { get; set; }

	public string? ServerLocation { get; set; }

	public string[] ServerTags { get; set; } = Array.Empty<string>();

	public AccessInfo? AccessInfo { get; set; }

	public bool IsTcpProxySupported { get; set; } = true;

	public bool IsTcpPacketSupported { get; set; }

	public int Mtu { get; set; } = 1500;

	[Obsolete("Use IsTcpPacketSupported")]
	public bool IsTunProviderSupported
	{
		get
		{
			return IsTcpPacketSupported;
		}
		init
		{
			IsTcpPacketSupported = value;
		}
	}
}
