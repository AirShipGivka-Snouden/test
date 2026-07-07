using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharpPcap;
using SharpPcap.WinDivert;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Collections;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.VpnAdapters.Abstractions;

namespace VpnHood.Core.VpnAdapters.WinDivert;

public class WinDivertVpnAdapter(WinDivertVpnAdapterSettings adapterSettings) : TunVpnAdapter(adapterSettings)
{
	private WinDivertDevice? _device;

	private WinDivertHeader? _lastCaptureHeader;

	private readonly List<IpNetwork> _includeIpNetworks = new List<IpNetwork>();

	private IReadOnlyList<IPAddress> _dnsServers = Array.Empty<IPAddress>();

	private readonly TimeoutDictionary<ushort, TimeoutItem<IPAddress>> _lastDnsServersV4 = new TimeoutDictionary<ushort, TimeoutItem<IPAddress>>(TimeSpan.FromSeconds(30L));

	private readonly TimeoutDictionary<ushort, TimeoutItem<IPAddress>> _lastDnsServersV6 = new TimeoutDictionary<ushort, TimeoutItem<IPAddress>>(TimeSpan.FromSeconds(30L));

	private readonly bool _excludeLocalNetwork = adapterSettings.ExcludeLocalNetwork;

	private readonly bool _simulateDns = adapterSettings.SimulateDns;

	public const short ProtectedTtl = 111;

	public override bool IsAppFilterSupported => false;

	protected override bool IsSocketProtectedByBind => false;

	public override bool IsNatSupported => false;

	protected override string? AppPackageId => null;

	protected override bool RestartAfterNetworkAddressChanged => false;

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint LoadLibrary(string lpFileName);

	protected override Task AdapterAdd(CancellationToken cancellationToken)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		_device = new WinDivertDevice
		{
			Flags = 0uL
		};
		_includeIpNetworks.Clear();
		SetWinDivertDllFolder();
		return Task.CompletedTask;
	}

	protected override void AdapterRemove()
	{
		AdapterClose();
		WinDivertDevice? device = _device;
		if (device != null)
		{
			device.Dispose();
		}
		_device = null;
	}

	private static string Ip(IpRange ipRange)
	{
		if (ipRange.AddressFamily != AddressFamily.InterNetworkV6)
		{
			return "ip";
		}
		return "ipv6";
	}

	protected override Task AdapterOpen(CancellationToken cancellationToken)
	{
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Expected O, but got Unknown
		if (_device == null)
		{
			throw new InvalidOperationException("Device is not initialized.");
		}
		IpRangeOrderedList ipRangeOrderedList = _includeIpNetworks.ToIpRanges();
		if (_excludeLocalNetwork)
		{
			ipRangeOrderedList = ipRangeOrderedList.Exclude(IpNetwork.LocalNetworks.ToIpRanges());
		}
		string text = "true";
		if (!ipRangeOrderedList.IsAll())
		{
			IEnumerable<string> values = ipRangeOrderedList.Select((IpRange x) => x.FirstIpAddress.Equals(x.LastIpAddress) ? $"{Ip(x)}.DstAddr=={x.FirstIpAddress}" : $"({Ip(x)}.DstAddr>={x.FirstIpAddress} and {Ip(x)}.DstAddr<={x.LastIpAddress})");
			string text2 = string.Join(" or ", values);
			text = text + " and (" + text2 + ")";
			text = "(" + text + ")";
		}
		string value = (_simulateDns ? "(udp.DstPort==53)" : "false");
		string text3 = $"(ip.TTL!={111} or ipv6.HopLimit!={111}) and outbound and !loopback and ({value} or {text})";
		text3 = text3.Replace("ipv6.DstAddr>=::", "ipv6");
		try
		{
			_device.Filter = text3;
			_device.Open(new DeviceConfiguration());
		}
		catch (Exception ex)
		{
			if (ex.Message.IndexOf("access is denied", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				throw new Exception("Access denied! Could not open WinDivert driver! Make sure the app is running with admin privilege.", ex);
			}
			throw;
		}
		return Task.CompletedTask;
	}

	protected override void AdapterClose()
	{
		WinDivertDevice? device = _device;
		if (device != null)
		{
			device.Close();
		}
	}

	protected override Task SetDnsServers(IReadOnlyList<IPAddress> dnsServers, CancellationToken cancellationToken)
	{
		_dnsServers = dnsServers.ToList();
		return Task.CompletedTask;
	}

	protected override Task AddRoute(IpNetwork ipNetwork, CancellationToken cancellationToken)
	{
		_includeIpNetworks.Add(ipNetwork);
		return Task.CompletedTask;
	}

	protected override Task SetAllowedApps(IEnumerable<string> packageIds, CancellationToken cancellationToken)
	{
		throw new NotSupportedException("App filtering is not supported on LinuxTun.");
	}

	protected override Task SetDisallowedApps(IEnumerable<string> packageIds, CancellationToken cancellationToken)
	{
		throw new NotSupportedException("App filtering is not supported on LinuxTun.");
	}

	protected override Task AddAddress(IpNetwork ipNetwork, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	protected override Task SetMetric(int metric, bool ipV4, bool ipV6, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	protected override Task SetSessionName(string sessionName, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	protected override Task AddNat(IpNetwork ipNetwork, CancellationToken cancellationToken)
	{
		throw new NotSupportedException("NAT is not supported on LinuxTun.");
	}

	protected override Task SetMtu(int mtu, bool ipV4, bool ipV6, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	protected override bool ReadPacket(byte[] buffer)
	{
		throw new InvalidOperationException("ReadPacket with buffer should not be called when ReadPacket has the override.");
	}

	protected override IpPacket? ReadPacket(int mtu)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		if (_device == null)
		{
			throw new InvalidOperationException("Device is not initialized.");
		}
		PacketCapture val = default(PacketCapture);
		if ((int)_device.GetNextPacket(ref val) != 1)
		{
			return null;
		}
		_lastCaptureHeader = (WinDivertHeader)((PacketCapture)(ref val)).Header;
		IpPacket ipPacket = PacketBuilder.Attach(((PacketCapture)(ref val)).GetPacket().Data);
		ProcessReadPacket(ipPacket);
		return ipPacket;
	}

	public override bool ProtectSocket(Socket socket)
	{
		IPAddress address = (socket.AddressFamily.IsV4() ? IPAddress.Any : IPAddress.IPv6Any);
		socket.Bind(new IPEndPoint(address, 0));
		socket.Ttl = 111;
		return true;
	}

	public override bool ProtectSocket(Socket socket, IPAddress ipAddress)
	{
		base.ProtectSocket(socket, ipAddress);
		socket.Ttl = 111;
		return true;
	}

	protected virtual void ProcessReadPacket(IpPacket ipPacket)
	{
		SimulateAdapterNetwork(ipPacket, read: true);
		SimulateDnsServers(ipPacket, read: true);
	}

	protected override bool WritePacket(IpPacket ipPacket)
	{
		SimulateAdapterNetwork(ipPacket, read: false);
		SimulateDnsServers(ipPacket, read: false);
		WritePacketToAdapter(ipPacket, outbound: false);
		return true;
	}

	protected void WritePacketToAdapter(IpPacket ipPacket, bool outbound)
	{
		if (_lastCaptureHeader == null)
		{
			throw new InvalidOperationException("Could not send any data without receiving a packet.");
		}
		if (_device == null)
		{
			throw new InvalidOperationException("Device is not initialized.");
		}
		_lastCaptureHeader.Flags = (WinDivertPacketFlags)(outbound ? 2 : 0);
		_device.SendPacket((ReadOnlySpan<byte>)ipPacket.Buffer.Span, (ICaptureHeader)(object)_lastCaptureHeader);
	}

	protected override void WaitForTunRead()
	{
	}

	protected override void WaitForTunWrite()
	{
	}

	private void SimulateAdapterNetwork(IpPacket ipPacket, bool read)
	{
		if (read)
		{
			IPAddress iPAddress = GetIpNetwork(ipPacket.Version)?.Prefix;
			if (iPAddress == null)
			{
				VhLogger.Instance.LogDebug("The arrival packet is not supported : {Packet}", VhLogger.FormatIpPacket(ipPacket.ToString()));
				return;
			}
			ipPacket.SourceAddress = iPAddress;
		}
		else
		{
			IPAddress primaryAdapterAddress = GetPrimaryAdapterAddress(ipPacket.Version);
			if (primaryAdapterAddress == null)
			{
				throw new InvalidOperationException("Could not send packet to inbound. there is no internal IP.");
			}
			ipPacket.DestinationAddress = primaryAdapterAddress;
		}
		ipPacket.UpdateAllChecksums();
	}

	private void SimulateDnsServers(IpPacket ipPacket, bool read)
	{
		if (!_simulateDns || ipPacket.Protocol != IpProtocol.Udp || _dnsServers.Any())
		{
			return;
		}
		UdpPacket udpPacket = ipPacket.ExtractUdp();
		TimeoutDictionary<ushort, TimeoutItem<IPAddress>> timeoutDictionary = ((ipPacket.Version == IpVersion.IPv4) ? _lastDnsServersV4 : _lastDnsServersV6);
		if (read)
		{
			if (udpPacket.DestinationPort == 53)
			{
				timeoutDictionary.AddOrUpdate(udpPacket.SourcePort, new TimeoutItem<IPAddress>(ipPacket.DestinationAddress));
				IPAddress[] array = _dnsServers.Where((IPAddress dns) => dns.AddressFamily == ipPacket.SourceAddress.AddressFamily).ToArray();
				IPAddress iPAddress = ((array.Length != 0) ? array[new Random().Next(array.Length)] : null);
				if (iPAddress != null)
				{
					ipPacket.DestinationAddress = iPAddress;
					ipPacket.UpdateAllChecksums();
				}
			}
		}
		else if (udpPacket.SourcePort == 53)
		{
			if (timeoutDictionary.TryGetValue(udpPacket.DestinationPort, out var value))
			{
				ipPacket.SourceAddress = value.Value;
			}
			ipPacket.UpdateAllChecksums();
		}
	}

	private static void SetWinDivertDllFolder()
	{
		string destinationFolder = Path.Combine(Path.GetTempPath(), "VpnHood", "WinDivert", "2.2.2");
		if (new string[2] { "WinDivert.dll", "WinDivert64.sys" }.Select((string x) => Path.Combine(destinationFolder, x)).Any((string x) => !File.Exists(x)))
		{
			using MemoryStream stream = new MemoryStream(Resources.WinDivertLibZip);
			using ZipArchive source = new ZipArchive(stream);
			source.ExtractToDirectory(destinationFolder, overwriteFiles: true);
		}
		LoadLibrary(Path.Combine(destinationFolder, "WinDivert.dll"));
	}

	protected override void DisposeManaged()
	{
		_lastDnsServersV4.Dispose();
		_lastDnsServersV6.Dispose();
		base.DisposeManaged();
	}

	~WinDivertVpnAdapter()
	{
		Dispose(disposing: false);
	}
}
