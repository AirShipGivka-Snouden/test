using System.Runtime.InteropServices;

namespace VpnHood.Core.VpnAdapters.WinTun.WinNative;

internal static class Kernel32
{
	public const uint WaitObject0 = 0u;

	public const uint Infinite = uint.MaxValue;

	public const uint WaitFailed = uint.MaxValue;

	[DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
	public static extern uint WaitForSingleObject(nint handle, int milliseconds);

	[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
	public static extern bool CloseHandle(nint hObject);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern nint LoadLibrary(string lpFileName);
}
