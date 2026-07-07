using System.Collections.Generic;
using System.Threading.Tasks;
using Ciphra.VPN.Common.Models;
using VpnHood.Core.Client.VpnServices.Manager;

namespace Ciphra.VPN.Common.Services.Interfaces;

public interface IVpnServiceManagerFactory
{
	SplitTunnelCapabilities SplitTunnelCapabilities => Ciphra.VPN.Common.Models.SplitTunnelCapabilities.Default;

	string WinTunAdapterBaseName => "Ciphra.VPN";

	IReadOnlyList<string> WinTunAdapterSeedNames => new string[1] { WinTunAdapterBaseName };

	VpnServiceManager CreateVpnServiceManager();

	VpnServiceManager CreateVpnServiceManager(bool winTunOnly)
	{
		return CreateVpnServiceManager();
	}

	void DisposeCurrentDevice();

	Task StopVpnTunnelAsync()
	{
		return Task.CompletedTask;
	}

	bool TryCleanupStaleAdapter(string adapterName)
	{
		return false;
	}

	bool IsBatteryOptimizationIgnored()
	{
		return true;
	}

	bool TryRequestBatteryOptimizationExemption()
	{
		return false;
	}
}
