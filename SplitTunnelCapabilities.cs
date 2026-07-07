namespace Ciphra.VPN.Common.Models;

public sealed record SplitTunnelCapabilities(bool IsTcpProxySupported = true, bool IsIncludeAppsSupported = false, bool IsExcludeAppsSupported = false)
{
	public static SplitTunnelCapabilities Default { get; } = new SplitTunnelCapabilities();
}
