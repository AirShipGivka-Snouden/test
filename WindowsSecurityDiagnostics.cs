using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Serilog;

namespace Ciphra.VPN.Common.Utils;

public static class WindowsSecurityDiagnostics
{
	private struct TOKEN_ELEVATION
	{
		public int TokenIsElevated;
	}

	private const uint TOKEN_QUERY = 8u;

	private const int TokenElevation = 20;

	public static bool? TryIsProcessElevated()
	{
		if (!OperatingSystem.IsWindows())
		{
			return null;
		}
		nint tokenHandle = IntPtr.Zero;
		try
		{
			if (!OpenProcessToken(GetCurrentProcess(), 8u, out tokenHandle))
			{
				return null;
			}
			int num = Marshal.SizeOf<TOKEN_ELEVATION>();
			nint num2 = Marshal.AllocHGlobal(num);
			try
			{
				if (!GetTokenInformation(tokenHandle, 20, num2, num, out var _))
				{
					return null;
				}
				return Marshal.PtrToStructure<TOKEN_ELEVATION>(num2).TokenIsElevated != 0;
			}
			finally
			{
				Marshal.FreeHGlobal(num2);
			}
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Process elevation check failed.");
			return null;
		}
		finally
		{
			if (tokenHandle != IntPtr.Zero)
			{
				CloseHandle(tokenHandle);
			}
		}
	}

	public static bool? TryIsMemoryIntegrityEnabled()
	{
		if (!OperatingSystem.IsWindows())
		{
			return null;
		}
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity");
			return (registryKey?.GetValue("Enabled") is int num) ? new bool?(num != 0) : ((bool?)null);
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Memory Integrity check failed.");
			return null;
		}
	}

	[DllImport("kernel32.dll")]
	private static extern nint GetCurrentProcess();

	[DllImport("advapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool OpenProcessToken(nint processHandle, uint desiredAccess, out nint tokenHandle);

	[DllImport("advapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetTokenInformation(nint tokenHandle, int tokenInformationClass, nint tokenInformation, int tokenInformationLength, out int returnLength);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CloseHandle(nint handle);
}
