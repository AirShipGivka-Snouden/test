using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public static class WinTunAdapterCleaner
{
	private struct SP_DEVINFO_DATA
	{
		public uint cbSize;

		public Guid ClassGuid;

		public uint DevInst;

		public nint Reserved;
	}

	private struct SP_CLASSINSTALL_HEADER
	{
		public uint cbSize;

		public uint InstallFunction;
	}

	private struct SP_REMOVEDEVICE_PARAMS
	{
		public SP_CLASSINSTALL_HEADER ClassInstallHeader;

		public uint Scope;

		public uint HwProfile;
	}

	private static readonly Guid GUID_DEVCLASS_NET = new Guid(1295444338u, 58149, 4558, 191, 193, 8, 0, 43, 225, 3, 24);

	private const uint DIGCF_PRESENT = 2u;

	private const uint DIF_REMOVE = 5u;

	private const uint DI_REMOVEDEVICE_GLOBAL = 1u;

	private static readonly nint INVALID_HANDLE_VALUE = new IntPtr(-1);

	private const int CR_SUCCESS = 0;

	private const int CR_NO_SUCH_DEVNODE = 13;

	private const uint CM_LOCATE_DEVNODE_PHANTOM = 1u;

	internal static Guid BuildAdapterGuid(string adapterName)
	{
		string s = "VpnHood." + adapterName;
		byte[] sourceArray = SHA1.HashData(Encoding.UTF8.GetBytes(s));
		byte[] array = new byte[16];
		Array.Copy(sourceArray, 0, array, 0, 16);
		array[7] = (byte)((array[7] & 0xF) | 0x50);
		array[8] = (byte)((array[8] & 0x3F) | 0x80);
		return new Guid(array);
	}

	public static int TryKillOrphanedProcesses()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return 0;
		}
		int num = 0;
		try
		{
			Process currentProcess = Process.GetCurrentProcess();
			int id = currentProcess.Id;
			string processName = currentProcess.ProcessName;
			string text = null;
			try
			{
				text = currentProcess.MainModule?.FileName;
			}
			catch
			{
			}
			if (text == null)
			{
				Log.Debug("TryKillOrphanedProcesses: could not determine own executable path. Skipping.");
				return 0;
			}
			Process[] processesByName = Process.GetProcessesByName(processName);
			foreach (Process process in processesByName)
			{
				try
				{
					if (process.Id == id)
					{
						continue;
					}
					try
					{
						string text2 = process.MainModule?.FileName;
						if (text2 == null || !string.Equals(text2, text, StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						goto IL_00e2;
					}
					catch
					{
					}
					goto end_IL_008a;
					IL_00e2:
					try
					{
						if ((DateTime.Now - process.StartTime).TotalSeconds < 15.0)
						{
							Log.Debug<int, DateTime>("Skipping recently started process PID {Pid} (started {StartTime}).", process.Id, process.StartTime);
							continue;
						}
					}
					catch
					{
					}
					Log.Information<string, int>("Killing orphaned VPN process {ProcessName} (PID {Pid}) that may be holding a stale WinTun adapter handle.", process.ProcessName, process.Id);
					process.Kill();
					process.WaitForExit(3000);
					num++;
					end_IL_008a:;
				}
				catch (Exception ex)
				{
					Log.Debug<int>(ex, "Could not kill process PID {Pid}.", process.Id);
				}
				finally
				{
					process.Dispose();
				}
			}
		}
		catch (Exception ex2)
		{
			Log.Debug(ex2, "TryKillOrphanedProcesses failed.");
		}
		return num;
	}

	public static bool TryRemoveStaleAdapter(string adapterName)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return false;
		}
		try
		{
			return TryRemoveStaleAdapterCore(adapterName);
		}
		catch (Exception ex)
		{
			Log.Warning<string>(ex, "SetupDi stale adapter removal failed for '{AdapterName}'.", adapterName);
			return false;
		}
	}

	private static bool TryRemoveStaleAdapterCore(string adapterName)
	{
		string text = BuildAdapterGuid(adapterName).ToString("B").ToUpperInvariant();
		Guid ClassGuid = GUID_DEVCLASS_NET;
		nint num = SetupDiGetClassDevsW(ref ClassGuid, null, IntPtr.Zero, 0u);
		if (num == INVALID_HANDLE_VALUE)
		{
			Log.Warning<int>("SetupDiGetClassDevsW failed, Win32 error: {Error}.", Marshal.GetLastWin32Error());
			return false;
		}
		try
		{
			SP_DEVINFO_DATA DeviceInfoData = new SP_DEVINFO_DATA
			{
				cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>()
			};
			for (uint num2 = 0u; SetupDiEnumDeviceInfo(num, num2, ref DeviceInfoData); num2++)
			{
				char[] array = new char[256];
				if (!SetupDiGetDeviceInstanceIdW(num, ref DeviceInfoData, array, (uint)array.Length, out var _))
				{
					continue;
				}
				string text2 = new string(array).TrimEnd('\0');
				if (text2.StartsWith("SWD\\WINTUN\\", StringComparison.OrdinalIgnoreCase) && text2.Contains(text, StringComparison.OrdinalIgnoreCase))
				{
					Log.Information<string>("Found stale WinTun adapter: {InstanceId}. Removing via SetupDi.", text2);
					SP_REMOVEDEVICE_PARAMS sP_REMOVEDEVICE_PARAMS = new SP_REMOVEDEVICE_PARAMS
					{
						ClassInstallHeader = new SP_CLASSINSTALL_HEADER
						{
							cbSize = (uint)Marshal.SizeOf<SP_CLASSINSTALL_HEADER>(),
							InstallFunction = 5u
						},
						Scope = 1u,
						HwProfile = 0u
					};
					if (!SetupDiSetClassInstallParamsW(num, ref DeviceInfoData, ref sP_REMOVEDEVICE_PARAMS.ClassInstallHeader, (uint)Marshal.SizeOf<SP_REMOVEDEVICE_PARAMS>()))
					{
						Log.Warning<int>("SetupDiSetClassInstallParamsW failed, Win32 error: {Error}.", Marshal.GetLastWin32Error());
						return false;
					}
					if (!SetupDiCallClassInstaller(5u, num, ref DeviceInfoData))
					{
						Log.Warning<int>("SetupDiCallClassInstaller(DIF_REMOVE) failed, Win32 error: {Error}.", Marshal.GetLastWin32Error());
						return false;
					}
					Log.Information<string, string>("Removed stale WinTun adapter '{AdapterName}' ({InstanceId}).", adapterName, text2);
					return true;
				}
			}
			Log.Debug<string, string>("No stale WinTun adapter found for '{AdapterName}' (GUID: {Guid}).", adapterName, text);
			return false;
		}
		finally
		{
			SetupDiDestroyDeviceInfoList(num);
		}
	}

	public static IReadOnlyList<string> SweepOrphanedAdapters(IEnumerable<string> trackedNames, string? activeName)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return Array.Empty<string>();
		}
		List<string> list = new List<string>();
		foreach (string item in trackedNames ?? Enumerable.Empty<string>())
		{
			if (string.IsNullOrEmpty(item) || (!string.IsNullOrEmpty(activeName) && string.Equals(item, activeName, StringComparison.OrdinalIgnoreCase)))
			{
				continue;
			}
			try
			{
				if (TryUninstallByName(item))
				{
					list.Add(item);
				}
			}
			catch (Exception ex)
			{
				Log.Warning<string>(ex, "Sweep failed for WinTun adapter '{Name}'.", item);
			}
		}
		return list;
	}

	public static bool TryUninstallByName(string adapterName)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return false;
		}
		try
		{
			return TryUninstallByNameCore(adapterName);
		}
		catch (Exception ex) when (((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? 1 : 0) != 0)
		{
			Log.Debug<string>("DiUninstallDevice unavailable ({Type}); falling back to SetupDi DIF_REMOVE.", ex.GetType().Name);
			return TryRemoveStaleAdapter(adapterName);
		}
		catch (Exception ex2)
		{
			Log.Warning<string>(ex2, "TryUninstallByName('{Name}') failed.", adapterName);
			return false;
		}
	}

	private static bool TryUninstallByNameCore(string adapterName)
	{
		string text = BuildAdapterGuid(adapterName).ToString("B").ToUpperInvariant();
		Guid ClassGuid = GUID_DEVCLASS_NET;
		nint num = SetupDiGetClassDevsW(ref ClassGuid, null, IntPtr.Zero, 0u);
		if (num == INVALID_HANDLE_VALUE)
		{
			Log.Warning<int>("SetupDiGetClassDevsW failed, Win32 error: {Error}.", Marshal.GetLastWin32Error());
			return false;
		}
		try
		{
			SP_DEVINFO_DATA DeviceInfoData = new SP_DEVINFO_DATA
			{
				cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>()
			};
			for (uint num2 = 0u; SetupDiEnumDeviceInfo(num, num2, ref DeviceInfoData); num2++)
			{
				char[] array = new char[256];
				if (!SetupDiGetDeviceInstanceIdW(num, ref DeviceInfoData, array, (uint)array.Length, out var _))
				{
					continue;
				}
				string text2 = new string(array).TrimEnd('\0');
				if (text2.StartsWith("SWD\\WINTUN\\", StringComparison.OrdinalIgnoreCase) && text2.Contains(text, StringComparison.OrdinalIgnoreCase))
				{
					Log.Information<string>("Uninstalling WinTun adapter: {InstanceId}.", text2);
					if (!DiUninstallDevice(IntPtr.Zero, num, ref DeviceInfoData, 0u, out var NeedReboot))
					{
						Log.Warning<string, string, int>("DiUninstallDevice failed for '{Name}' ({InstanceId}), Win32 error: {Error}.", adapterName, text2, Marshal.GetLastWin32Error());
						return false;
					}
					Log.Information<string, string, bool>("Uninstalled WinTun adapter '{Name}' ({InstanceId}). NeedReboot={NeedReboot}.", adapterName, text2, NeedReboot);
					return true;
				}
			}
			Log.Debug<string, string>("No WinTun adapter found for '{Name}' (GUID: {Guid}) — already absent.", adapterName, text);
			return true;
		}
		finally
		{
			SetupDiDestroyDeviceInfoList(num);
		}
	}

	public static async Task<bool> WaitUntilDevnodeGoneAsync(Guid adapterGuid, TimeSpan maxWait, CancellationToken ct)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return true;
		}
		string instanceId = "SWD\\WINTUN\\" + adapterGuid.ToString("B").ToUpperInvariant();
		DateTime deadline = DateTime.UtcNow + maxWait;
		while (DateTime.UtcNow < deadline)
		{
			int cr;
			try
			{
				cr = CM_Locate_DevNodeW(out var _, instanceId, 1u);
			}
			catch (Exception ex)
			{
				Log.Debug<string>(ex, "CM_Locate_DevNodeW threw for '{InstanceId}'.", instanceId);
				return false;
			}
			if (cr == 13)
			{
				return true;
			}
			await Task.Delay(TimeSpan.FromMilliseconds(250L), ct);
		}
		return false;
	}

	[DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint SetupDiGetClassDevsW(ref Guid ClassGuid, [MarshalAs(UnmanagedType.LPWStr)] string? Enumerator, nint hwndParent, uint Flags);

	[DllImport("setupapi.dll", SetLastError = true)]
	private static extern bool SetupDiEnumDeviceInfo(nint DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

	[DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool SetupDiGetDeviceInstanceIdW(nint DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, [Out] char[] DeviceInstanceId, uint DeviceInstanceIdSize, out uint RequiredSize);

	[DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool SetupDiSetClassInstallParamsW(nint DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, ref SP_CLASSINSTALL_HEADER ClassInstallParams, uint ClassInstallParamsSize);

	[DllImport("setupapi.dll", SetLastError = true)]
	private static extern bool SetupDiCallClassInstaller(uint InstallFunction, nint DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData);

	[DllImport("setupapi.dll", SetLastError = true)]
	private static extern bool SetupDiDestroyDeviceInfoList(nint DeviceInfoSet);

	[DllImport("newdev.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool DiUninstallDevice(nint hwndParent, nint DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, uint Flags, [MarshalAs(UnmanagedType.Bool)] out bool NeedReboot);

	[DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern int CM_Locate_DevNodeW(out uint pdnDevInst, [MarshalAs(UnmanagedType.LPWStr)] string pDeviceID, uint ulFlags);
}
