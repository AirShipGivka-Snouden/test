using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Ciphra.VPN.WinUI.Extensions;

public static class PlatformHelpers
{
	private enum TOKEN_INFORMATION_CLASS
	{
		TokenUser = 1,
		TokenElevationType = 18,
		TokenElevation = 20
	}

	private struct TOKEN_ELEVATION
	{
		public int TokenIsElevated;
	}

	private const uint PROCESS_QUERY_LIMITED_INFORMATION = 4096u;

	private const uint TOKEN_QUERY = 8u;

	public static bool IsRunningElevated()
	{
		using WindowsIdentity ntIdentity = WindowsIdentity.GetCurrent();
		WindowsPrincipal windowsPrincipal = new WindowsPrincipal(ntIdentity);
		return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint OpenProcess(uint access, bool inherit, uint processId);

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern bool OpenProcessToken(nint hProcess, uint desiredAccess, out nint hToken);

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern bool GetTokenInformation(nint token, TOKEN_INFORMATION_CLASS infoClass, nint buffer, int length, out int returnLength);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CloseHandle(nint h);

	public static bool? TryIsProcessElevated(uint pid)
	{
		nint num = OpenProcess(4096u, inherit: false, pid);
		if (num == IntPtr.Zero)
		{
			return null;
		}
		try
		{
			if (!OpenProcessToken(num, 8u, out var hToken))
			{
				return null;
			}
			try
			{
				int num2 = Marshal.SizeOf<TOKEN_ELEVATION>();
				nint num3 = Marshal.AllocHGlobal(num2);
				try
				{
					if (!GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenElevation, num3, num2, out var _))
					{
						return null;
					}
					return Marshal.PtrToStructure<TOKEN_ELEVATION>(num3).TokenIsElevated != 0;
				}
				finally
				{
					Marshal.FreeHGlobal(num3);
				}
			}
			finally
			{
				CloseHandle(hToken);
			}
		}
		finally
		{
			CloseHandle(num);
		}
	}
}
