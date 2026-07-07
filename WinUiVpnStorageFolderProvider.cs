using System;
using System.IO;
using Ciphra.VPN.Common.Services.Interfaces;

namespace Ciphra.VPN.WinUI.Services;

public class WinUiVpnStorageFolderProvider : IVpnStorageFolderProvider
{
	public string GetVpnStorageFolderPath()
	{
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VpnServiceManager");
	}
}
