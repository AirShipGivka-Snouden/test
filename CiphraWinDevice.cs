using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Client.Device;
using VpnHood.Core.Client.Device.UiContexts;

namespace Ciphra.VPN.WinUI.Services;

internal class CiphraWinDevice : IDevice, IDisposable
{
	private readonly bool _isDebugMode;

	private CiphraWinVpnService? _vpnService;

	public bool IsBindProcessToVpnSupported => false;

	public string OsInfo => Environment.OSVersion?.ToString() + ", " + (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");

	public string VpnServiceConfigFolder { get; }

	public bool IsExcludeAppsSupported => _isDebugMode;

	public bool IsIncludeAppsSupported => _isDebugMode;

	public bool IsTcpProxySupported => true;

	public bool IsTv => false;

	public DeviceMemInfo MemInfo
	{
		get
		{
			GCMemoryInfo gCMemoryInfo = GC.GetGCMemoryInfo();
			return new DeviceMemInfo
			{
				TotalMemory = gCMemoryInfo.TotalAvailableMemoryBytes,
				AvailableMemory = gCMemoryInfo.TotalAvailableMemoryBytes - gCMemoryInfo.MemoryLoadBytes
			};
		}
	}

	public DeviceAppInfo[] InstalledApps
	{
		get
		{
			if (!_isDebugMode)
			{
				throw new NotSupportedException();
			}
			return Array.Empty<DeviceAppInfo>();
		}
	}

	public CiphraWinDevice(string storageFolder, bool isDebugMode)
	{
		_isDebugMode = isDebugMode;
		VpnServiceConfigFolder = Path.Combine(storageFolder, "vpn-service");
	}

	public Task RequestVpnService(IUiContext? uiContext, TimeSpan timeout, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	public Task StartVpnService(CancellationToken cancellationToken)
	{
		if (_vpnService == null || _vpnService.IsDisposed)
		{
			_vpnService = new CiphraWinVpnService(VpnServiceConfigFolder);
		}
		_vpnService.OnConnect();
		return Task.CompletedTask;
	}

	public void BindProcessToVpn(bool value)
	{
		throw new NotSupportedException();
	}

	public void Dispose()
	{
		_vpnService?.Dispose();
		_vpnService = null;
	}
}
