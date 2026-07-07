using Ciphra.VPN.Common.Models;

namespace Ciphra.VPN.Common.App;

public sealed record SplitTunnelSnapshot(bool UseSplitLocalNetwork, RoutingMode SplitTunnelRoutingMode, bool UseSplitIpViaDevice, bool UseSplitIpViaApp, bool UseSplitDomain, SplitAppMode SplitAppMode, string[] SplitApps, string SplitIpDeviceIncludes, string SplitIpDeviceExcludes, string SplitIpAppIncludes, string SplitIpAppExcludes, string SplitIpAppBlocks, string SplitDomainIncludes, string SplitDomainExcludes, string SplitDomainBlocks);
