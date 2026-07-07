using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.VpnAdapters.Abstractions;

public class VpnAdapterOptions
{
	public static readonly IReadOnlyList<IpNetwork> AllVRoutesIpV4 = new _003C_003Ez__ReadOnlyArray<IpNetwork>(new IpNetwork[2]
	{
		IpNetwork.Parse("0.0.0.0/1"),
		IpNetwork.Parse("128.0.0.0/1")
	});

	public static readonly IReadOnlyList<IpNetwork> AllVRoutesIpV6 = new _003C_003Ez__ReadOnlyArray<IpNetwork>(new IpNetwork[2]
	{
		IpNetwork.Parse("::/1"),
		IpNetwork.Parse("8000::/1")
	});

	public static readonly IReadOnlyList<IpNetwork> AllVRoutes = AllVRoutesIpV4.Concat(AllVRoutesIpV6).ToArray();

	public required string? SessionName { get; init; }

	public IpNetwork? VirtualIpNetworkV4 { get; init; }

	public IpNetwork? VirtualIpNetworkV6 { get; init; }

	public IReadOnlyList<IPAddress> DnsServers { get; init; } = Array.Empty<IPAddress>();

	public IEnumerable<IpNetwork> IncludeNetworks { get; init; } = Array.Empty<IpNetwork>();

	public IEnumerable<string>? ExcludeApps { get; init; }

	public IEnumerable<string>? IncludeApps { get; init; }

	public bool UseNat { get; init; }

	public int? Mtu { get; init; }

	public int? Metric { get; init; }
}
