using System;
using VpnHood.Core.Client.VpnServices.Abstractions;
using VpnHood.Core.Client.VpnServices.Host;
using VpnHood.Core.Tunneling.Sockets;
using VpnHood.Core.VpnAdapters.Abstractions;
using VpnHood.Core.VpnAdapters.WinDivert;
using VpnHood.Core.VpnAdapters.WinTun;

namespace VpnHood.Core.Client.Device.Win;

public class WinVpnService : IVpnServiceHandler, IDisposable
{
	private readonly VpnServiceHost _vpnServiceHost;

	public bool IsDisposed { get; private set; }

	public WinVpnService(string configFolder)
	{
		_vpnServiceHost = new VpnServiceHost(configFolder, this, new SocketFactory(), null, withLogger: false);
	}

	public void OnConnect()
	{
		_vpnServiceHost.TryConnect();
	}

	public void OnDisconnect()
	{
		_vpnServiceHost.TryDisconnect();
	}

	public IVpnAdapter CreateAdapter(VpnAdapterSettings adapterSettings, string? debugData)
	{
		if ((!(debugData?.Contains("/windivert", StringComparison.OrdinalIgnoreCase))) ?? true)
		{
			return new WinTunVpnAdapter(new WinVpnAdapterSettings
			{
				AdapterName = adapterSettings.AdapterName,
				AutoRestart = adapterSettings.AutoRestart,
				MaxPacketSendDelay = adapterSettings.MaxPacketSendDelay,
				Blocking = adapterSettings.Blocking,
				AutoDisposePackets = adapterSettings.AutoDisposePackets,
				AutoMetric = adapterSettings.AutoMetric,
				QueueCapacity = adapterSettings.QueueCapacity
			});
		}
		return new WinDivertVpnAdapter(new WinDivertVpnAdapterSettings
		{
			AdapterName = adapterSettings.AdapterName,
			AutoRestart = adapterSettings.AutoRestart,
			MaxPacketSendDelay = adapterSettings.MaxPacketSendDelay,
			Blocking = adapterSettings.Blocking,
			AutoDisposePackets = adapterSettings.AutoDisposePackets,
			QueueCapacity = adapterSettings.QueueCapacity,
			ExcludeLocalNetwork = true
		});
	}

	public void ShowNotification(ConnectionInfo connectionInfo)
	{
	}

	public void StopNotification()
	{
	}

	public void StopSelf()
	{
		Dispose();
	}

	public void Dispose()
	{
		if (!IsDisposed)
		{
			IsDisposed = true;
			_vpnServiceHost.Dispose();
		}
	}
}
