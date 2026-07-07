using VpnHood.Core.VpnAdapters.Abstractions;

namespace VpnHood.Core.VpnAdapters.WinTun;

public class WinVpnAdapterSettings : VpnAdapterSettings
{
	public int RingCapacity { get; init; } = 4194304;
}
