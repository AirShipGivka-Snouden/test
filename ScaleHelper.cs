using System.Runtime.InteropServices;

namespace Ciphra.VPN.WinUI.Extensions;

public static class ScaleHelper
{
	[DllImport("Shcore.dll", SetLastError = true)]
	private static extern int GetScaleFactorForMonitor(nint hMonitor, out int pScale);

	public static double GetScaleFactor()
	{
		nint hMonitor = MonitorFromWindow(GetDesktopWindow(), 0u);
		int pScale = 0;
		GetScaleFactorForMonitor(hMonitor, out pScale);
		return (double)pScale / 100.0;
	}

	[DllImport("user32.dll")]
	private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint GetDesktopWindow();
}
