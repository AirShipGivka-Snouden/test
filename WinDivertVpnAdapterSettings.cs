using VpnHood.Core.VpnAdapters.Abstractions;

namespace VpnHood.Core.VpnAdapters.WinDivert;

public class WinDivertVpnAdapterSettings : VpnAdapterSettings
{
	public new bool AutoMetric => false;

	public bool ExcludeLocalNetwork { get; set; } = true;

	public bool SimulateDns { get; set; } = true;

	public WinDivertVpnAdapterSettings()
	{
		base.AutoMetric = false;
	}
}
