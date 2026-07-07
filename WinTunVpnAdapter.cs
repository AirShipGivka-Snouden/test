using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Exceptions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.VpnAdapters.Abstractions;
using VpnHood.Core.VpnAdapters.WinTun.WinNative;

namespace VpnHood.Core.VpnAdapters.WinTun;

public class WinTunVpnAdapter(WinVpnAdapterSettings adapterSettings) : TunVpnAdapter(adapterSettings)
{
	private uint _adapterIndex;

	private readonly int _ringCapacity = adapterSettings.RingCapacity;

	private nint _tunAdapter;

	private nint _tunSession;

	private nint _readEvent;

	private readonly byte[] _writeBuffer = new byte[65535];

	public const int MinRingCapacity = 131072;

	public const int MaxRingCapacity = 67108864;

	protected override bool IsSocketProtectedByBind => true;

	public override bool IsNatSupported => true;

	public override bool IsAppFilterSupported => false;

	protected override string? AppPackageId => null;

	protected override bool RestartAfterNetworkAddressChanged => true;

	protected override Task SetAllowedApps(IEnumerable<string> packageIds, CancellationToken cancellationToken)
	{
		throw new NotSupportedException("App filtering is not supported on WinTun.");
	}

	protected override Task SetDisallowedApps(IEnumerable<string> packageIds, CancellationToken cancellationToken)
	{
		throw new NotSupportedException("App filtering is not supported on WinTun.");
	}

	private static Guid BuildGuidFromName(string adapterName)
	{
		adapterName = "VpnHood." + adapterName;
		using SHA1 sHA = SHA1.Create();
		byte[] sourceArray = sHA.ComputeHash(Encoding.UTF8.GetBytes(adapterName));
		byte[] array = new byte[16];
		Array.Copy(sourceArray, 0, array, 0, 16);
		array[7] = (byte)((array[7] & 0xF) | 0x50);
		array[8] = (byte)((array[8] & 0x3F) | 0x80);
		return new Guid(array);
	}

	protected override Task AdapterAdd(CancellationToken cancellationToken)
	{
		LoadWinTunDll();
		_tunAdapter = WinTunApi.WintunCreateAdapter(base.AdapterName, "VPN", BuildGuidFromName(base.AdapterName));
		int lastWin32Error = Marshal.GetLastWin32Error();
		if (_tunAdapter == IntPtr.Zero && lastWin32Error == 183)
		{
			nint num = WinTunApi.WintunOpenAdapter(base.AdapterName);
			if (num != IntPtr.Zero)
			{
				WinTunApi.WintunCloseAdapter(num);
			}
			_tunAdapter = WinTunApi.WintunCreateAdapter(base.AdapterName, "VPN", BuildGuidFromName(base.AdapterName));
		}
		if (_tunAdapter == IntPtr.Zero)
		{
			throw new PInvokeException($"Failed to create WinTun adapter. Make sure the app is running with admin privilege. ErrorCode: {lastWin32Error}", lastWin32Error);
		}
		_adapterIndex = GetAdapterIndex(base.AdapterName);
		return Task.CompletedTask;
	}

	protected override void AdapterRemove()
	{
		if (_tunAdapter != IntPtr.Zero)
		{
			WinTunApi.WintunCloseAdapter(_tunAdapter);
			_tunAdapter = IntPtr.Zero;
		}
		if (base.UseNat)
		{
			VhLogger.Instance.LogDebug("Removing previous NAT iptables record for {AdapterName} TUN adapter...", base.AdapterName);
			if (base.AdapterIpNetworkV4 != null)
			{
				TryRemoveNat(base.AdapterIpNetworkV4);
			}
			if (base.AdapterIpNetworkV6 != null)
			{
				TryRemoveNat(base.AdapterIpNetworkV6);
			}
		}
		_adapterIndex = 0u;
	}

	protected override Task AdapterOpen(CancellationToken cancellationToken)
	{
		VhLogger.Instance.LogInformation("Starting WinTun session...");
		_tunSession = WinTunApi.WintunStartSession(_tunAdapter, _ringCapacity);
		if (_tunSession == IntPtr.Zero)
		{
			throw new Win32Exception("Failed to start WinTun session.");
		}
		VhLogger.Instance.LogDebug("Creating event object for WinTun...");
		_readEvent = WinTunApi.WintunGetReadWaitEvent(_tunSession);
		return Task.CompletedTask;
	}

	protected override void AdapterClose()
	{
		nint num = Interlocked.Exchange(ref _tunSession, IntPtr.Zero);
		_readEvent = IntPtr.Zero;
		if (num != IntPtr.Zero)
		{
			WinTunApi.WintunEndSession(num);
		}
	}

	protected override Task SetSessionName(string sessionName, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	protected override async Task SetMetric(int metric, bool ipV4, bool ipV6, CancellationToken cancellationToken)
	{
		if (ipV4)
		{
			await OsUtils.ExecuteCommandAsync("netsh", $"interface ipv4 set interface \"{base.AdapterName}\" metric={metric}", cancellationToken);
		}
		if (ipV6)
		{
			await OsUtils.ExecuteCommandAsync("netsh", $"interface ipv6 set interface \"{base.AdapterName}\" metric={metric}", cancellationToken);
		}
	}

	protected override async Task AddAddress(IpNetwork ipNetwork, CancellationToken cancellationToken)
	{
		string command = (ipNetwork.IsV4 ? $"interface ipv4 set address \"{base.AdapterName}\" static {ipNetwork}" : $"interface ipv6 set address \"{base.AdapterName}\" {ipNetwork}");
		await OsUtils.ExecuteCommandAsync("netsh", command, cancellationToken);
	}

	private async Task AddRouteUsingNetsh(IpNetwork ipNetwork, CancellationToken cancellationToken)
	{
		string command = (ipNetwork.IsV4 ? $"interface ipv4 add route {ipNetwork} \"{base.AdapterName}\"" : $"interface ipv6 add route {ipNetwork} \"{base.AdapterName}\"");
		await OsUtils.ExecuteCommandAsync("netsh", command, cancellationToken);
	}

	protected override Task AddRoute(IpNetwork ipNetwork, CancellationToken cancellationToken)
	{
		if (_adapterIndex == 0)
		{
			throw new InvalidOperationException("Adapter index is not set. Call AdapterOpen() first.");
		}
		Console.WriteLine($"Adding {ipNetwork}");
		Win32IpHelper.AddRoute(_adapterIndex, ipNetwork, cancellationToken);
		return Task.CompletedTask;
	}

	private static uint GetAdapterIndex(string adapterName)
	{
		NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault((NetworkInterface x) => x.Name == adapterName);
		if (networkInterface == null)
		{
			throw new InvalidOperationException("Could not find network adapter with name '" + adapterName + "'.");
		}
		if (networkInterface.Supports(NetworkInterfaceComponent.IPv4))
		{
			uint index = (uint)networkInterface.GetIPProperties().GetIPv4Properties().Index;
			if (index != 0)
			{
				return index;
			}
		}
		if (networkInterface.Supports(NetworkInterfaceComponent.IPv6))
		{
			uint index2 = (uint)networkInterface.GetIPProperties().GetIPv6Properties().Index;
			if (index2 != 0)
			{
				return index2;
			}
		}
		throw new InvalidOperationException("Adapter '" + adapterName + "' does not support IPv4 or IPv6.");
	}

	protected override async Task SetMtu(int mtu, bool ipV4, bool ipV6, CancellationToken cancellationToken)
	{
		if (ipV4)
		{
			await OsUtils.ExecuteCommandAsync("netsh", $"interface ipv4 set subinterface \"{base.AdapterName}\" mtu={mtu}", cancellationToken);
		}
		if (ipV6)
		{
			await OsUtils.ExecuteCommandAsync("netsh", $"interface ipv6 set subinterface \"{base.AdapterName}\" mtu={mtu}", cancellationToken);
		}
	}

	protected override async Task SetDnsServers(IReadOnlyList<IPAddress> dnsServers, CancellationToken cancellationToken)
	{
		IPAddress[] array = dnsServers.Where((IPAddress ip) => ip.AddressFamily == AddressFamily.InterNetwork).ToArray();
		IPAddress[] ipv6 = dnsServers.Where((IPAddress ip) => ip.AddressFamily == AddressFamily.InterNetworkV6).ToArray();
		VhLogger.Instance.LogDebug("Setting DNS via PowerShell...");
		if (array.Length != 0)
		{
			string value = string.Join(",", array.Select((IPAddress ip) => $"'{ip}'"));
			string cmd = $"Set-DnsClientServerAddress -InterfaceAlias '{base.AdapterName}' -ServerAddresses @({value})";
			await VhUtils.TryInvokeAsync((VhLogger.MinLogLevel == LogLevel.Trace) ? "Set IPv4 DNS" : "", () => OsUtils.ExecuteCommandAsync("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"" + cmd + "\"", cancellationToken));
		}
		if (ipv6.Length != 0)
		{
			string value2 = string.Join(",", ipv6.Select((IPAddress ip) => $"'{ip}'"));
			string cmd2 = $"Set-DnsClientServerAddress -InterfaceAlias '{base.AdapterName}' -ServerAddresses @({value2})";
			await VhUtils.TryInvokeAsync((VhLogger.MinLogLevel == LogLevel.Trace) ? "Set IPv6 DNS" : "", () => OsUtils.ExecuteCommandAsync("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"" + cmd2 + "\"", cancellationToken));
		}
	}

	protected override async Task AddNat(IpNetwork ipNetwork, CancellationToken cancellationToken)
	{
		TryRemoveNat(ipNetwork);
		if (ipNetwork.IsV4)
		{
			string value = base.AdapterName + "Nat";
			await ExecutePowerShellCommandAsync($"New-NetNat -Name {value} -InternalIPInterfaceAddressPrefix {ipNetwork}", cancellationToken).Vhc();
		}
		if (ipNetwork.IsV6)
		{
			string natName = base.AdapterName + "NatIpV6";
			await VhUtils.TryInvokeAsync("Configuring NAT for IPv6", () => ExecutePowerShellCommandAsync($"New-NetNat -Name {natName} -InternalIPInterfaceAddressPrefix {ipNetwork}", cancellationToken));
		}
	}

	private static void TryRemoveNat(IpNetwork ipNetwork)
	{
		VhUtils.TryInvoke("Remove NAT rule", () => ExecutePowerShellCommand($"Get-NetNat | Where-Object {{ $_.InternalIPInterfaceAddressPrefix -eq '{ipNetwork}' }} | Remove-NetNat -Confirm:$false"));
	}

	private static Task<string> ExecutePowerShellCommandAsync(string command, CancellationToken cancellationToken)
	{
		string command2 = "-NoProfile -ExecutionPolicy Bypass -Command \"" + command + "\"";
		return OsUtils.ExecuteCommandAsync("powershell.exe", command2, cancellationToken);
	}

	private static string ExecutePowerShellCommand(string command)
	{
		string command2 = "-NoProfile -ExecutionPolicy Bypass -Command \"" + command + "\"";
		return OsUtils.ExecuteCommand("powershell.exe", command2);
	}

	protected override void WaitForTunRead()
	{
		nint readEvent = _readEvent;
		if (readEvent == IntPtr.Zero)
		{
			throw new IOException("WinTun session is closed.");
		}
		uint num = Kernel32.WaitForSingleObject(readEvent, uint.MaxValue);
		object ex;
		switch (num)
		{
		case 0u:
			return;
		default:
			ex = new PInvokeException("Unexpected result from WaitForSingleObject", (int)num);
			break;
		case uint.MaxValue:
			ex = new Win32Exception();
			break;
		}
		throw ex;
	}

	protected override bool ReadPacket(byte[] buffer)
	{
		nint tunSession = _tunSession;
		if (tunSession == IntPtr.Zero)
		{
			throw new IOException("WinTun session is closed.");
		}
		int size;
		nint num = WinTunApi.WintunReceivePacket(tunSession, out size);
		if (num != IntPtr.Zero)
		{
			try
			{
				Marshal.Copy(num, buffer, 0, size);
				return true;
			}
			finally
			{
				WinTunApi.WintunReleaseReceivePacket(tunSession, num);
			}
		}
		WintunReceivePacketError lastWin32Error = (WintunReceivePacketError)Marshal.GetLastWin32Error();
		return lastWin32Error switch
		{
			WintunReceivePacketError.NoMoreItems => false, 
			WintunReceivePacketError.HandleEof => throw new IOException("WinTun adapter has been closed."), 
			WintunReceivePacketError.InvalidData => throw new InvalidOperationException("Invalid data received from WinTun adapter."), 
			_ => throw new PInvokeException($"Unknown error in reading packet from WinTun. LastError: {lastWin32Error}"), 
		};
	}

	protected override void WaitForTunWrite()
	{
		Thread.Sleep(1);
	}

	protected override bool WritePacket(IpPacket ipPacket)
	{
		nint tunSession = _tunSession;
		if (tunSession == IntPtr.Zero)
		{
			return false;
		}
		nint num = WinTunApi.WintunAllocateSendPacket(tunSession, ipPacket.Buffer.Length);
		if (num == IntPtr.Zero)
		{
			return false;
		}
		Marshal.Copy(ipPacket.GetUnderlyingBufferUnsafe(_writeBuffer, out var offset, out var length), offset, num, length);
		WinTunApi.WintunSendPacket(tunSession, num);
		return true;
	}

	private static void LoadWinTunDll()
	{
		bool flag = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		Architecture oSArchitecture = RuntimeInformation.OSArchitecture;
		string text;
		if (oSArchitecture != Architecture.X64)
		{
			if (oSArchitecture != Architecture.Arm64 || !flag)
			{
				goto IL_0038;
			}
			text = "arm64";
		}
		else
		{
			if (!flag)
			{
				goto IL_0038;
			}
			text = "x64";
		}
		string text2 = text;
		string destinationFolder = Path.Combine(Path.GetTempPath(), "VpnHood", "WinTun", "0.14.1");
		if (new string[1] { Path.Combine("bin", text2, "wintun.dll") }.Select((string x) => Path.Combine(destinationFolder, x)).Any((string x) => !File.Exists(x)))
		{
			using MemoryStream stream = new MemoryStream(Resources.WinTunZip);
			using ZipArchive source = new ZipArchive(stream);
			source.ExtractToDirectory(destinationFolder, overwriteFiles: true);
		}
		if (Kernel32.LoadLibrary(Path.Combine(destinationFolder, "bin", text2, "wintun.dll")) == IntPtr.Zero)
		{
			throw new Win32Exception("Failed to load WinTun DLL.");
		}
		return;
		IL_0038:
		throw new NotSupportedException("WinTun is not supported on this OS.");
	}

	protected override void DisposeUnmanaged()
	{
		if (_tunAdapter != IntPtr.Zero)
		{
			AdapterRemove();
		}
		base.DisposeUnmanaged();
	}

	~WinTunVpnAdapter()
	{
		Dispose(disposing: false);
	}
}
