using System;
using System.Collections.Generic;
using Ciphra.VPN.Common.Services;
using Ciphra.VPN.Common.Services.Interfaces;
using VpnHood.Core.Client.Device;
using VpnHood.Core.Client.Device.Win;
using VpnHood.Core.Client.VpnServices.Manager;

namespace Ciphra.VPN.WinUI.Services;

internal class WinUiVpnServiceManagerFactory : IVpnServiceManagerFactory, IDisposable
{
	private IDevice? _currentDevice;

	private readonly object _deviceLock = new object();

	public string WinTunAdapterBaseName => "Ciphra.VPN.Store";

	public IReadOnlyList<string> WinTunAdapterSeedNames => new string[1] { "Ciphra.VPN.Store" };

	public VpnServiceManager CreateVpnServiceManager()
	{
		return CreateVpnServiceManager(winTunOnly: false);
	}

	public VpnServiceManager CreateVpnServiceManager(bool winTunOnly)
	{
		WinUiVpnStorageFolderProvider winUiVpnStorageFolderProvider = new WinUiVpnStorageFolderProvider();
		string vpnStorageFolderPath = winUiVpnStorageFolderProvider.GetVpnStorageFolderPath();
		lock (_deviceLock)
		{
			DisposeCurrentDevice();
			IDevice currentDevice;
			if (!winTunOnly)
			{
				IDevice device = new WinDevice(vpnStorageFolderPath, isDebugMode: false);
				currentDevice = device;
			}
			else
			{
				IDevice device = new CiphraWinDevice(vpnStorageFolderPath, isDebugMode: false);
				currentDevice = device;
			}
			_currentDevice = currentDevice;
			return new VpnServiceManager(_currentDevice, TimeSpan.FromSeconds(1L));
		}
	}

	public void DisposeCurrentDevice()
	{
		try
		{
			_currentDevice?.Dispose();
		}
		catch
		{
		}
		finally
		{
			_currentDevice = null;
		}
	}

	public bool TryCleanupStaleAdapter(string adapterName)
	{
		return WinTunAdapterCleaner.TryRemoveStaleAdapter(adapterName);
	}

	public void Dispose()
	{
		lock (_deviceLock)
		{
			DisposeCurrentDevice();
		}
	}
}
