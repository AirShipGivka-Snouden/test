using System;
using System.Runtime.InteropServices;

namespace VpnHood.Core.VpnAdapters.WinTun.WinNative;

internal static class WinTunApi
{
	[DllImport("wintun.dll", SetLastError = true)]
	public static extern nint WintunCreateAdapter([MarshalAs(UnmanagedType.LPWStr)] string adapterName, [MarshalAs(UnmanagedType.LPWStr)] string tunnelType, in Guid requestedGuid);

	[DllImport("wintun.dll", SetLastError = true)]
	public static extern nint WintunCreateAdapter([MarshalAs(UnmanagedType.LPWStr)] string adapterName, [MarshalAs(UnmanagedType.LPWStr)] string tunnelType, nint requestedGuid);

	[DllImport("wintun.dll", SetLastError = true)]
	public static extern nint WintunOpenAdapter([MarshalAs(UnmanagedType.LPWStr)] string adapterName);

	[DllImport("wintun.dll")]
	public static extern void WintunCloseAdapter(nint adapter);

	[DllImport("wintun.dll", SetLastError = true)]
	public static extern bool WintunDeleteDriver();

	[DllImport("wintun.dll")]
	public static extern void WintunGetAdapterLUID(nint adapter, ref Luid luid);

	[DllImport("wintun.dll")]
	public static extern int WintunGetRunningDriverVersion();

	[DllImport("wintun.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern void WintunSetLogger(WintunLoggerCallback newLogger);

	[DllImport("wintun.dll", SetLastError = true)]
	public static extern void WintunSetLogger(nint mustBeNull);

	[DllImport("wintun.dll", SetLastError = true)]
	public static extern nint WintunStartSession(nint adapter, int capacity);

	[DllImport("wintun.dll", SetLastError = true)]
	public static extern nint WintunAllocateSendPacket(nint session, int packetSize);

	[DllImport("wintun.dll")]
	public static extern void WintunEndSession(nint session);

	[DllImport("wintun.dll")]
	public static extern nint WintunGetReadWaitEvent(nint session);

	[DllImport("wintun.dll", SetLastError = true)]
	public static extern nint WintunReceivePacket(nint session, out int size);

	[DllImport("wintun.dll")]
	public static extern void WintunReleaseReceivePacket(nint session, nint packet);

	[DllImport("wintun.dll", SetLastError = true)]
	public static extern void WintunSendPacket(nint session, nint packet);
}
