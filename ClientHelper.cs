using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Client.Abstractions;
using VpnHood.Core.Filtering.Abstractions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Client;

internal static class ClientHelper
{
	private static bool IsIncluded(IIpFilter clientIpFilter, IPAddress ipAddress)
	{
		return clientIpFilter.Process(IpProtocol.Udp, new IpEndPointValue(ipAddress, 53)) == FilterAction.Include;
	}

	public static DnsConfig GetDnsServers(IReadOnlyList<IPAddress>? userDnsAddresses, IReadOnlyList<IPAddress> serverDnsAddresses, IpRangeOrderedList serverIncludeIpRanges, IIpFilter ipFilter)
	{
		bool isUserSuppressed = false;
		IEnumerable<IPAddress> enumerable;
		if (userDnsAddresses != null && userDnsAddresses.Any())
		{
			enumerable = userDnsAddresses.Where((IPAddress x) => !IsIncluded(ipFilter, x));
			if (enumerable.Any())
			{
				VhLogger.Instance.LogInformation("Using User's DNS servers, but they are not excluded from VPN because of IP filters. DnsServers: {DnsServers}", VhLogger.Format(enumerable));
				return new DnsConfig
				{
					DnsServers = enumerable.ToArray(),
					IsIncludedInVpn = false,
					IsUserSuppressed = isUserSuppressed,
					DnsSelection = DnsSelection.UserDns
				};
			}
			enumerable = userDnsAddresses.Where(serverIncludeIpRanges.Contains);
			if (enumerable.Any())
			{
				VhLogger.Instance.LogInformation("Using User's DNS servers. DnsServers: {DnsServers}", VhLogger.Format(enumerable));
				return new DnsConfig
				{
					DnsServers = enumerable.ToArray(),
					IsIncludedInVpn = true,
					IsUserSuppressed = isUserSuppressed,
					DnsSelection = DnsSelection.UserDns
				};
			}
			isUserSuppressed = true;
			VhLogger.Instance.LogWarning("Client DNS servers have been ignored because the server does not route them.");
		}
		if (serverDnsAddresses.Any())
		{
			enumerable = serverDnsAddresses.Where((IPAddress x) => IsIncluded(ipFilter, x));
			if (enumerable.Any())
			{
				VhLogger.Instance.LogInformation("Using Server default DNS servers. DnsServers: {DnsServers}", VhLogger.Format(enumerable));
				return new DnsConfig
				{
					DnsServers = enumerable.ToArray(),
					IsIncludedInVpn = true,
					IsUserSuppressed = isUserSuppressed,
					DnsSelection = DnsSelection.ServerDns
				};
			}
		}
		enumerable = IPAddressUtil.GoogleDnsServers.Where((IPAddress x) => IsIncluded(ipFilter, x)).Where(serverIncludeIpRanges.Contains);
		if (enumerable.Any())
		{
			VhLogger.Instance.LogInformation("Using Google DNS servers as default. DnsServers: {DnsServers}", VhLogger.Format(enumerable));
			return new DnsConfig
			{
				DnsServers = enumerable.ToArray(),
				IsIncludedInVpn = true,
				IsUserSuppressed = isUserSuppressed,
				DnsSelection = DnsSelection.GoogleDns
			};
		}
		enumerable = IPAddressUtil.GoogleDnsServers;
		VhLogger.Instance.LogWarning("Using Google DNS servers, but they are not excluded from VPN because of IP filters. DnsServers: {DnsServers}", VhLogger.Format(enumerable));
		return new DnsConfig
		{
			DnsServers = enumerable.ToArray(),
			IsIncludedInVpn = false,
			IsUserSuppressed = isUserSuppressed,
			DnsSelection = DnsSelection.GoogleDns
		};
	}

	public static IpRangeOrderedList BuildIncludeIpRangesByDevice(IpRangeOrderedList includeIpRanges, IReadOnlyList<IPAddress> catcherIps, bool canProtectSocket, bool includeLocalNetwork, IPAddress hostIpAddress)
	{
		includeIpRanges = includeIpRanges.Exclude(hostIpAddress);
		if (!includeLocalNetwork)
		{
			includeIpRanges = includeIpRanges.Exclude(IpNetwork.LocalNetworks.ToIpRanges()).Exclude(IpNetwork.MulticastNetworks.ToIpRanges()).Exclude(IPAddress.Broadcast);
		}
		includeIpRanges = includeIpRanges.Union(catcherIps.ToIpRanges());
		return includeIpRanges;
	}
}
