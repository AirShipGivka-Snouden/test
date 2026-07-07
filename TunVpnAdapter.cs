using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.PacketTransports;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.VpnAdapters.Abstractions;

public abstract class TunVpnAdapter : PacketTransport, IVpnAdapter, IPacketTransport, IDisposable
{
	private readonly int _maxPacketSendDelayMs;

	private const int MaxIoErrorCount = 10;

	private readonly bool _autoRestart;

	private int _mtu = 65535;

	private int _ioErrorCount;

	private readonly bool _autoMetric;

	private HashSet<IPAddress> _lastPrimaryAdapterAddresses = new HashSet<IPAddress>();

	private static readonly IpNetwork[] WebDeadNetworks = new IpNetwork[2]
	{
		IpNetwork.Parse("203.0.113.1/24"),
		IpNetwork.Parse("2001:4860:ffff::1234/48")
	};

	private readonly Lock _stopLock = new Lock();

	private bool _isStarting;

	private bool _isRestartingAdapter;

	private bool _isStopping;

	private VpnAdapterOptions? _startOptions;

	private readonly AsyncLock _restartLock = new AsyncLock();

	protected bool UseNat { get; private set; }

	public abstract bool IsAppFilterSupported { get; }

	public abstract bool IsNatSupported { get; }

	public virtual bool CanProtectSocket => true;

	protected abstract bool IsSocketProtectedByBind { get; }

	protected abstract string? AppPackageId { get; }

	protected abstract bool RestartAfterNetworkAddressChanged { get; }

	public string AdapterName { get; }

	public IPAddress? PrimaryAdapterIpV4 { get; private set; } = DiscoverPrimaryAdapterIp(AddressFamily.InterNetwork);

	public IPAddress? PrimaryAdapterIpV6 { get; private set; } = DiscoverPrimaryAdapterIp(AddressFamily.InterNetworkV6);

	public IpNetwork? AdapterIpNetworkV4 { get; private set; }

	public IpNetwork? AdapterIpNetworkV6 { get; private set; }

	public IPAddress? GatewayIpV4 { get; private set; }

	public IPAddress? GatewayIpV6 { get; private set; }

	public bool IsStarted { get; private set; }

	private bool IsReady
	{
		get
		{
			if (IsStarted && !_isStopping && !base.IsDisposed)
			{
				return !base.IsDisposing;
			}
			return false;
		}
	}

	public event EventHandler? Disposed;

	public event EventHandler? PrimaryAdapterIpChanged;

	protected abstract Task SetMtu(int mtu, bool ipV4, bool ipV6, CancellationToken cancellationToken);

	protected abstract Task SetMetric(int metric, bool ipV4, bool ipV6, CancellationToken cancellationToken);

	protected abstract Task SetDnsServers(IReadOnlyList<IPAddress> dnsServers, CancellationToken cancellationToken);

	protected abstract Task AddRoute(IpNetwork ipNetwork, CancellationToken cancellationToken);

	protected abstract Task AddAddress(IpNetwork ipNetwork, CancellationToken cancellationToken);

	protected abstract Task AddNat(IpNetwork ipNetwork, CancellationToken cancellationToken);

	protected abstract Task SetSessionName(string sessionName, CancellationToken cancellationToken);

	protected abstract Task SetAllowedApps(IEnumerable<string> packageIds, CancellationToken cancellationToken);

	protected abstract Task SetDisallowedApps(IEnumerable<string> packageIds, CancellationToken cancellationToken);

	protected abstract Task AdapterAdd(CancellationToken cancellationToken);

	protected abstract void AdapterRemove();

	protected abstract Task AdapterOpen(CancellationToken cancellationToken);

	protected abstract void AdapterClose();

	protected abstract void WaitForTunWrite();

	protected abstract void WaitForTunRead();

	protected abstract bool ReadPacket(byte[] buffer);

	protected abstract bool WritePacket(IpPacket ipPacket);

	public bool IsIpVersionSupported(IpVersion ipVersion)
	{
		return GetPrimaryAdapterAddress(ipVersion) != null;
	}

	protected TunVpnAdapter(VpnAdapterSettings adapterSettings)
		: base(adapterSettings)
	{
		_maxPacketSendDelayMs = (int)adapterSettings.MaxPacketSendDelay.TotalMilliseconds;
		_autoRestart = adapterSettings.AutoRestart;
		_autoMetric = adapterSettings.AutoMetric;
		AdapterName = adapterSettings.AdapterName;
		NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
	}

	public IPAddress? GetPrimaryAdapterAddress(IpVersion ipVersion)
	{
		if (ipVersion != IpVersion.IPv4)
		{
			return PrimaryAdapterIpV6;
		}
		return PrimaryAdapterIpV4;
	}

	public IPAddress? GetGatewayIp(IpVersion ipVersion)
	{
		if (ipVersion != IpVersion.IPv4)
		{
			return GatewayIpV6;
		}
		return GatewayIpV4;
	}

	public IpNetwork? GetIpNetwork(IpVersion ipVersion)
	{
		if (ipVersion != IpVersion.IPv4)
		{
			return AdapterIpNetworkV6;
		}
		return AdapterIpNetworkV4;
	}

	public async Task Start(VpnAdapterOptions options, CancellationToken cancellationToken)
	{
		ObjectDisposedException.ThrowIf(base.IsDisposed, this);
		_startOptions = options;
		if (UseNat && !IsNatSupported)
		{
			throw new NotSupportedException("NAT is not supported by this adapter.");
		}
		try
		{
			VhLogger.Instance.LogInformation("Starting the VPN adapter. AdapterName: {AdapterName}", AdapterName);
			_isStarting = true;
			IsStarted = true;
			PrimaryAdapterIpV4 = DiscoverPrimaryAdapterIp(AddressFamily.InterNetwork);
			PrimaryAdapterIpV6 = DiscoverPrimaryAdapterIp(AddressFamily.InterNetworkV6);
			AdapterIpNetworkV4 = options.VirtualIpNetworkV4;
			AdapterIpNetworkV6 = options.VirtualIpNetworkV6;
			UseNat = options.UseNat;
			_mtu = options.Mtu ?? _mtu;
			_lastPrimaryAdapterAddresses = GetPrimaryAdapterAddresses();
			VhLogger.Instance.LogInformation("TunAdapterInfo. AdapterType: {AdapterType}, UseNat: {UseNat}, MTU: {MTU}, PrimaryAdapterIpV4: {PrimaryAdapterIpV4}, PrimaryAdapterIpV6: {PrimaryAdapterIpV6}, AdapterIpNetworkV4: {AdapterIpNetworkV4}, AdapterIpNetworkV6: {AdapterIpNetworkV6}", VhLogger.FormatType(this), UseNat, _mtu, VhLogger.Format(PrimaryAdapterIpV4), VhLogger.Format(PrimaryAdapterIpV6), AdapterIpNetworkV4, AdapterIpNetworkV6);
			VhLogger.Instance.LogInformation("Adding TUN adapter...");
			await AdapterAdd(cancellationToken).Vhc();
			if (!string.IsNullOrEmpty(options.SessionName))
			{
				await SetSessionName(options.SessionName, cancellationToken);
			}
			if (AdapterIpNetworkV4 != null)
			{
				VhLogger.Instance.LogDebug("Adding IPv4 address to adapter ...");
				GatewayIpV4 = BuildGatewayFromFromNetwork(AdapterIpNetworkV4);
				await AddAddress(AdapterIpNetworkV4, cancellationToken).Vhc();
			}
			if (AdapterIpNetworkV6 != null)
			{
				VhLogger.Instance.LogDebug("Adding IPv6 address to adapter ...");
				try
				{
					GatewayIpV6 = BuildGatewayFromFromNetwork(AdapterIpNetworkV6);
					await AddAddress(AdapterIpNetworkV6, cancellationToken).Vhc();
				}
				catch (Exception exception)
				{
					VhLogger.Instance.LogError(exception, "Failed to add IPv6 address to TUN adapter. AdapterIpNetworkV6: {AdapterIpNetworkV6}", AdapterIpNetworkV6);
					AdapterIpNetworkV6 = null;
				}
			}
			if (options.Metric.HasValue)
			{
				VhLogger.Instance.LogDebug("Setting metric...");
				await SetMetric(options.Metric.Value, AdapterIpNetworkV4 != null, AdapterIpNetworkV6 != null, cancellationToken).Vhc();
			}
			if (options.Mtu.HasValue)
			{
				VhLogger.Instance.LogDebug("Setting MTU...");
				await SetMtu(options.Mtu.Value, AdapterIpNetworkV4 != null, AdapterIpNetworkV6 != null, cancellationToken).Vhc();
			}
			VhLogger.Instance.LogDebug("Setting DNS servers...");
			IReadOnlyList<IPAddress> readOnlyList = options.DnsServers;
			if (AdapterIpNetworkV4 == null)
			{
				readOnlyList = readOnlyList.Where((IPAddress x) => !x.IsV4()).ToArray();
			}
			if (AdapterIpNetworkV6 == null)
			{
				readOnlyList = readOnlyList.Where((IPAddress x) => !x.IsV6()).ToArray();
			}
			await SetDnsServers(readOnlyList, cancellationToken).Vhc();
			IpNetwork[] includeNetworks = options.IncludeNetworks.ToArray();
			if (IsSocketProtectedByBind)
			{
				includeNetworks = includeNetworks.ToIpRanges().Exclude(WebDeadNetworks.ToIpRanges()).ToIpNetworks()
					.ToArray();
			}
			VhLogger.Instance.LogDebug("Adding routes...");
			if (AdapterIpNetworkV4 != null)
			{
				await AddRouteHelper(includeNetworks, AddressFamily.InterNetwork, cancellationToken).Vhc();
			}
			if (AdapterIpNetworkV6 != null)
			{
				await AddRouteHelper(includeNetworks, AddressFamily.InterNetworkV6, cancellationToken).Vhc();
			}
			if (UseNat)
			{
				VhLogger.Instance.LogDebug("Adding NAT...");
				if (AdapterIpNetworkV4 != null && PrimaryAdapterIpV4 != null)
				{
					await AddNat(AdapterIpNetworkV4, cancellationToken).Vhc();
				}
				if (AdapterIpNetworkV6 != null && PrimaryAdapterIpV6 != null)
				{
					await AddNat(AdapterIpNetworkV6, cancellationToken).Vhc();
				}
			}
			if (IsAppFilterSupported)
			{
				await SetAppFilters(options.IncludeApps, options.ExcludeApps, cancellationToken);
			}
			VhLogger.Instance.LogInformation("Opening TUN adapter...");
			await AdapterOpen(cancellationToken).Vhc();
			Task.Run((Action)StartReadingPackets, CancellationToken.None);
			VhLogger.Instance.LogInformation("TUN adapter started.");
		}
		catch (Exception ex)
		{
			VhLogger.Instance.Log((!(ex is OperationCanceledException)) ? LogLevel.Error : LogLevel.Trace, ex, "Failed to start TUN adapter.");
			Stop(throwException: false);
			throw;
		}
		finally
		{
			_isStarting = false;
		}
	}

	private async Task AddRouteHelper(IEnumerable<IpNetwork> ipNetworks, AddressFamily addressFamily, CancellationToken cancellationToken)
	{
		ipNetworks = ipNetworks.Where((IpNetwork x) => x.AddressFamily == addressFamily);
		if (_autoMetric)
		{
			IOrderedEnumerable<IpNetwork> ipNetworks2 = ipNetworks.Sort();
			if (addressFamily.IsV4() && ipNetworks2.IsAllV4())
			{
				ipNetworks = VpnAdapterOptions.AllVRoutesIpV4;
			}
			if (addressFamily.IsV6() && ipNetworks2.IsAllV6())
			{
				ipNetworks = VpnAdapterOptions.AllVRoutesIpV6;
			}
		}
		foreach (IpNetwork network in ipNetworks)
		{
			try
			{
				await AddRoute(network, cancellationToken).Vhc();
			}
			catch (Exception ex)
			{
				throw new Exception($"Could not add {network} to route. {ex.Message}");
			}
		}
	}

	private async Task SetAppFilters(IEnumerable<string>? includeApps, IEnumerable<string>? excludeApps, CancellationToken cancellationToken)
	{
		string appPackageId = AppPackageId;
		if (appPackageId == null)
		{
			throw new InvalidOperationException("AppPackageId must be available when AppFilter is supported.");
		}
		if (!VhUtils.IsNullOrEmpty(includeApps) && !VhUtils.IsNullOrEmpty(excludeApps))
		{
			throw new InvalidOperationException("Both include and exclude apps cannot be set at the same time.");
		}
		if (includeApps != null)
		{
			includeApps = includeApps.Concat(new global::_003C_003Ez__ReadOnlySingleElementList<string>(appPackageId)).Distinct();
			await SetAllowedApps(includeApps, cancellationToken);
		}
		if (excludeApps != null)
		{
			excludeApps = excludeApps.Where((string x) => x != appPackageId).Distinct();
			await SetDisallowedApps(excludeApps, cancellationToken);
		}
	}

	public void Stop()
	{
		ObjectDisposedException.ThrowIf(base.IsDisposed, this);
		Stop(throwException: true);
	}

	private void Stop(bool throwException)
	{
		using (_stopLock.EnterScope())
		{
			if (!IsStarted || _isStopping)
			{
				return;
			}
			try
			{
				VhLogger.Instance.LogInformation("Stopping {AdapterName} adapter.", AdapterName);
				_isStopping = true;
				AdapterClose();
				AdapterRemove();
				PrimaryAdapterIpV4 = null;
				PrimaryAdapterIpV6 = null;
				AdapterIpNetworkV4 = null;
				AdapterIpNetworkV6 = null;
				GatewayIpV4 = null;
				GatewayIpV6 = null;
				IsStarted = false;
				VhLogger.Instance.LogInformation("TUN adapter stopped.");
			}
			catch (Exception exception)
			{
				if (throwException)
				{
					throw;
				}
				VhLogger.Instance.LogError(exception, "Failed to stop the TUN adapter. AdapterName: {AdapterName}", AdapterName);
			}
			finally
			{
				_isStopping = false;
			}
		}
	}

	protected static void BindToAny(Socket socket)
	{
		IPAddress address = (socket.AddressFamily.IsV4() ? IPAddress.Any : IPAddress.IPv6Any);
		socket.Bind(new IPEndPoint(address, 0));
	}

	protected virtual void BindSocketToIp(Socket socket, IPAddress address)
	{
		socket.Bind(new IPEndPoint(address, 0));
	}

	public virtual bool ProtectSocket(Socket socket)
	{
		ObjectDisposedException.ThrowIf(base.IsDisposed, this);
		if (socket.LocalEndPoint != null)
		{
			throw new InvalidOperationException("Could not protect an already bound socket.");
		}
		IPAddress primaryAdapterAddress = GetPrimaryAdapterAddress(socket.AddressFamily.IpVersion());
		if (primaryAdapterAddress == null)
		{
			BindToAny(socket);
			return false;
		}
		BindSocketToIp(socket, primaryAdapterAddress);
		return true;
	}

	public virtual bool ProtectSocket(Socket socket, IPAddress remoteAddress)
	{
		ObjectDisposedException.ThrowIf(base.IsDisposed, this);
		if (socket.LocalEndPoint != null)
		{
			throw new InvalidOperationException("Could not protect an already bound socket.");
		}
		IPAddress primaryAdapterAddress = GetPrimaryAdapterAddress(socket.AddressFamily.IpVersion());
		if (primaryAdapterAddress == null)
		{
			BindToAny(socket);
			return false;
		}
		if (IPAddress.IsLoopback(primaryAdapterAddress) != IPAddress.IsLoopback(remoteAddress))
		{
			BindToAny(socket);
			return false;
		}
		BindSocketToIp(socket, primaryAdapterAddress);
		return true;
	}

	private void DiscoverPrimaryAdapterIps(bool protect)
	{
		IPAddress iPAddress = DiscoverPrimaryAdapterIpViaProtect(AddressFamily.InterNetwork, protect);
		IPAddress iPAddress2 = DiscoverPrimaryAdapterIpViaProtect(AddressFamily.InterNetworkV6, protect);
		if (!object.Equals(iPAddress, PrimaryAdapterIpV4) || !object.Equals(iPAddress2, PrimaryAdapterIpV6))
		{
			PrimaryAdapterIpV4 = iPAddress;
			PrimaryAdapterIpV6 = iPAddress2;
			this.PrimaryAdapterIpChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private IPAddress? DiscoverPrimaryAdapterIpViaProtect(AddressFamily addressFamily, bool protect)
	{
		using UdpClient udpClient = new UdpClient(addressFamily);
		if (protect)
		{
			try
			{
				IPAddress prefix = WebDeadNetworks.First((IpNetwork x) => x.AddressFamily == addressFamily).Prefix;
				ProtectSocket(udpClient.Client, prefix);
			}
			catch (Exception exception)
			{
				VhLogger.Instance.LogError(exception, "Failed to protect the socket for discovering primary adapter IP. AddressFamily: {AddressFamily}", addressFamily);
				return null;
			}
		}
		return DiscoverPrimaryAdapterIp(udpClient);
	}

	private static IPAddress? DiscoverPrimaryAdapterIp(AddressFamily addressFamily)
	{
		using UdpClient protectedUdpClient = new UdpClient(addressFamily);
		return DiscoverPrimaryAdapterIp(protectedUdpClient);
	}

	private static IPAddress? DiscoverPrimaryAdapterIp(UdpClient protectedUdpClient)
	{
		AddressFamily addressFamily = protectedUdpClient.Client.AddressFamily;
		IPEndPoint iPEndPoint = new IPEndPoint(WebDeadNetworks.First((IpNetwork x) => x.AddressFamily == addressFamily).Prefix, 53);
		try
		{
			protectedUdpClient.Connect(iPEndPoint);
			IPEndPoint localEndPoint = protectedUdpClient.Client.GetLocalEndPoint();
			if (addressFamily.IsV6() && !IpNetwork.AllGlobalUnicastV6.Contains(localEndPoint.Address))
			{
				throw new Exception("The discovered primary adapter IP is not assigned to any local interface.");
			}
			VhLogger.Instance.LogDebug("Primary adapter IP discovered. PrimaryAdapterIp: {PrimaryAdapterIp}", VhLogger.Format(localEndPoint.Address));
			return localEndPoint.Address;
		}
		catch (Exception)
		{
			VhLogger.Instance.LogDebug("Failed to get primary adapter IP. RemoteEndPoint: {RemoteEndPoint}", iPEndPoint);
			return null;
		}
	}

	private static IPAddress? BuildGatewayFromFromNetwork(IpNetwork ipNetwork)
	{
		if (ipNetwork != null)
		{
			if (ipNetwork.IsV4)
			{
				if (ipNetwork.PrefixLength >= 31)
				{
					goto IL_002c;
				}
			}
			else if (ipNetwork.IsV6 && ipNetwork.PrefixLength == 128)
			{
				goto IL_002c;
			}
		}
		bool flag = false;
		goto IL_0032;
		IL_002c:
		flag = true;
		goto IL_0032;
		IL_0032:
		if (!flag)
		{
			return IPAddressUtil.Increment(ipNetwork.FirstIpAddress);
		}
		return null;
	}

	protected override ValueTask SendPacketsAsync(IReadOnlyList<IpPacket> ipPackets)
	{
		for (int i = 0; i < ipPackets.Count; i++)
		{
			SendPacket(ipPackets[i]);
		}
		return default(ValueTask);
	}

	protected void SendPacket(IpPacket ipPacket)
	{
		if (!IsReady)
		{
			throw new InvalidOperationException("TUN adapter is not in ready state.");
		}
		try
		{
			SendPacketInternal(ipPacket);
			_ioErrorCount = 0;
		}
		catch (Exception) when (!_isRestartingAdapter)
		{
			_ioErrorCount++;
			if (_ioErrorCount < 10 || _isRestartingAdapter)
			{
				throw;
			}
			if (_autoRestart)
			{
				RestartAdapter(CancellationToken.None);
				throw;
			}
			Stop(throwException: false);
			throw;
		}
	}

	private void SendPacketInternal(IpPacket ipPacket)
	{
		bool flag = false;
		int num = 5;
		while (true)
		{
			if (WritePacket(ipPacket))
			{
				flag = true;
				break;
			}
			if (num > _maxPacketSendDelayMs)
			{
				break;
			}
			num *= 2;
			Task.Delay(num).Wait();
			WaitForTunWrite();
		}
		if (!flag)
		{
			VhLogger.Instance.LogWarning("Failed to send packet via WinTun adapter.");
		}
	}

	protected virtual void StartReadingPackets()
	{
		while (IsReady)
		{
			try
			{
				IpPacket ipPacket = ReadPacket(_mtu);
				_ioErrorCount = 0;
				if (ipPacket == null)
				{
					WaitForTunRead();
				}
				else
				{
					OnPacketReceived(ipPacket);
				}
			}
			catch (Exception) when (!IsReady)
			{
				break;
			}
			catch (Exception exception)
			{
				VhLogger.Instance.LogError(exception, "Error in reading packets from TUN adapter.");
				_ioErrorCount++;
				if (_ioErrorCount >= 10 && !_isRestartingAdapter)
				{
					if (!_autoRestart)
					{
						break;
					}
					RestartAdapter(CancellationToken.None);
				}
			}
		}
		VhLogger.Instance.LogDebug("Finish reading the packets from the TUN adapter.");
		Stop(throwException: false);
	}

	protected virtual IpPacket? ReadPacket(int mtu)
	{
		IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(mtu);
		if (!MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)memoryOwner.Memory, out ArraySegment<byte> segment))
		{
			throw new InvalidOperationException("Could not get array from memory owner.");
		}
		try
		{
			if (segment.Array == null)
			{
				throw new InvalidOperationException("Memory owner's segment returned a null array.");
			}
			if (ReadPacket(segment.Array))
			{
				return PacketBuilder.Attach(memoryOwner);
			}
			memoryOwner.Dispose();
			return null;
		}
		catch
		{
			memoryOwner.Dispose();
			throw;
		}
	}

	private void NetworkChange_NetworkAddressChanged(object? sender, EventArgs e)
	{
		if (_isStarting || _isStopping || base.IsDisposing || _restartLock.IsLocked || _isRestartingAdapter || base.IsDisposed)
		{
			return;
		}
		HashSet<IPAddress> primaryAdapterAddresses = GetPrimaryAdapterAddresses();
		if (primaryAdapterAddresses.SetEquals(_lastPrimaryAdapterAddresses))
		{
			return;
		}
		_lastPrimaryAdapterAddresses = primaryAdapterAddresses;
		if (primaryAdapterAddresses.Count == 0)
		{
			return;
		}
		VhLogger.Instance.LogInformation("Network address changed.");
		if (RestartAfterNetworkAddressChanged)
		{
			Task.Run(() => Restart(CancellationToken.None));
		}
		else
		{
			DiscoverPrimaryAdapterIps(protect: true);
		}
	}

	private static bool IsPrimaryAdapter(NetworkInterface networkInterface)
	{
		if (networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
		{
			return !networkInterface.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private HashSet<IPAddress> GetPrimaryAdapterAddresses()
	{
		return (from a in (from ni in NetworkInterface.GetAllNetworkInterfaces().Where(IsPrimaryAdapter)
				where ni.OperationalStatus == OperationalStatus.Up
				where !ni.Name.Equals(AdapterName, StringComparison.OrdinalIgnoreCase)
				select ni).SelectMany((NetworkInterface ni) => ni.GetIPProperties().UnicastAddresses)
			select a.Address).ToHashSet();
	}

	public async Task Restart(CancellationToken cancellationToken)
	{
		ObjectDisposedException.ThrowIf(base.IsDisposed, this);
		ArgumentNullException.ThrowIfNull(_startOptions, "_startOptions");
		using AsyncLock.ILockAsyncResult lockScope = await _restartLock.LockAsync(TimeSpan.Zero, cancellationToken);
		if (lockScope.Succeeded)
		{
			VhLogger.Instance.LogInformation("Restarting VPN Adapter");
			Stop();
			await Task.Delay(TimeSpan.FromSeconds(2L), cancellationToken);
			DiscoverPrimaryAdapterIps(protect: false);
			await Start(_startOptions, cancellationToken);
		}
	}

	private async Task RestartAdapter(CancellationToken cancellationToken)
	{
		VhLogger.Instance.LogWarning("Restarting the adapter.");
		ObjectDisposedException.ThrowIf(base.IsDisposed, this);
		if (!IsStarted)
		{
			throw new InvalidOperationException("Cannot restart the adapter when it is stopped.");
		}
		try
		{
			_isRestartingAdapter = true;
			AdapterClose();
			await AdapterOpen(cancellationToken);
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogError(exception, "Failed to restart the adapter.");
			throw;
		}
		finally
		{
			_isRestartingAdapter = false;
		}
	}

	protected sealed override void PreDispose()
	{
		Stop(throwException: false);
		base.PreDispose();
	}

	protected override void DisposeManaged()
	{
		NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;
		this.Disposed?.Invoke(this, EventArgs.Empty);
		this.Disposed = null;
		base.DisposeManaged();
	}
}
