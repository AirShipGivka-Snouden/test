using System;
using Ciphra.VPN.Common.Models;

namespace Ciphra.VPN.Common.App;

public class SplitTunnelSettings
{
	private readonly JsonAppSettingsTransport _transport;

	private readonly object _stateLock = new object();

	public bool UseSplitLocalNetwork
	{
		get
		{
			return _transport.LoadOrInitProperty(defaultValue: false, "UseSplitLocalNetwork");
		}
		set
		{
			_transport.Save(value, "UseSplitLocalNetwork");
		}
	}

	public RoutingMode SplitTunnelRoutingMode
	{
		get
		{
			return _transport.LoadOrInitProperty(RoutingMode.TunnelAll, "SplitTunnelRoutingMode");
		}
		set
		{
			_transport.Save(value, "SplitTunnelRoutingMode");
		}
	}

	public bool UseSplitIpViaDevice
	{
		get
		{
			return _transport.LoadOrInitProperty(defaultValue: false, "UseSplitIpViaDevice");
		}
		set
		{
			_transport.Save(value, "UseSplitIpViaDevice");
		}
	}

	public bool UseSplitIpViaApp
	{
		get
		{
			return _transport.LoadOrInitProperty(defaultValue: false, "UseSplitIpViaApp");
		}
		set
		{
			_transport.Save(value, "UseSplitIpViaApp");
		}
	}

	public bool UseSplitDomain
	{
		get
		{
			return _transport.LoadOrInitProperty(defaultValue: false, "UseSplitDomain");
		}
		set
		{
			_transport.Save(value, "UseSplitDomain");
		}
	}

	public SplitAppMode SplitAppMode
	{
		get
		{
			return _transport.LoadOrInitProperty(SplitAppMode.All, "SplitAppMode");
		}
		set
		{
			_transport.Save(value, "SplitAppMode");
		}
	}

	public string[] SplitApps
	{
		get
		{
			return _transport.LoadOrInitProperty(Array.Empty<string>(), "SplitApps") ?? Array.Empty<string>();
		}
		set
		{
			_transport.Save(value, "SplitApps");
		}
	}

	public string SplitIpDeviceIncludes
	{
		get
		{
			return _transport.LoadOrInitProperty(string.Empty, "SplitIpDeviceIncludes") ?? string.Empty;
		}
		set
		{
			_transport.Save(value, "SplitIpDeviceIncludes");
		}
	}

	public string SplitIpDeviceExcludes
	{
		get
		{
			return _transport.LoadOrInitProperty(string.Empty, "SplitIpDeviceExcludes") ?? string.Empty;
		}
		set
		{
			_transport.Save(value, "SplitIpDeviceExcludes");
		}
	}

	public string SplitIpAppIncludes
	{
		get
		{
			return _transport.LoadOrInitProperty(string.Empty, "SplitIpAppIncludes") ?? string.Empty;
		}
		set
		{
			_transport.Save(value, "SplitIpAppIncludes");
		}
	}

	public string SplitIpAppExcludes
	{
		get
		{
			return _transport.LoadOrInitProperty(string.Empty, "SplitIpAppExcludes") ?? string.Empty;
		}
		set
		{
			_transport.Save(value, "SplitIpAppExcludes");
		}
	}

	public string SplitIpAppBlocks
	{
		get
		{
			return _transport.LoadOrInitProperty(string.Empty, "SplitIpAppBlocks") ?? string.Empty;
		}
		set
		{
			_transport.Save(value, "SplitIpAppBlocks");
		}
	}

	public string SplitDomainIncludes
	{
		get
		{
			return _transport.LoadOrInitProperty(string.Empty, "SplitDomainIncludes") ?? string.Empty;
		}
		set
		{
			_transport.Save(value, "SplitDomainIncludes");
		}
	}

	public string SplitDomainExcludes
	{
		get
		{
			return _transport.LoadOrInitProperty(string.Empty, "SplitDomainExcludes") ?? string.Empty;
		}
		set
		{
			_transport.Save(value, "SplitDomainExcludes");
		}
	}

	public string SplitDomainBlocks
	{
		get
		{
			return _transport.LoadOrInitProperty(string.Empty, "SplitDomainBlocks") ?? string.Empty;
		}
		set
		{
			_transport.Save(value, "SplitDomainBlocks");
		}
	}

	public SplitTunnelSettings(JsonAppSettingsTransport transport)
	{
		_transport = transport ?? throw new ArgumentNullException("transport");
	}

	public SplitTunnelSnapshot Snapshot()
	{
		lock (_stateLock)
		{
			string[] splitApps = SplitApps;
			return new SplitTunnelSnapshot(UseSplitLocalNetwork, SplitTunnelRoutingMode, UseSplitIpViaDevice, UseSplitIpViaApp, UseSplitDomain, SplitAppMode, (splitApps.Length == 0) ? Array.Empty<string>() : ((string[])splitApps.Clone()), SplitIpDeviceIncludes, SplitIpDeviceExcludes, SplitIpAppIncludes, SplitIpAppExcludes, SplitIpAppBlocks, SplitDomainIncludes, SplitDomainExcludes, SplitDomainBlocks);
		}
	}

	public void RunBatched(Action<SplitTunnelSettings> action)
	{
		ArgumentNullException.ThrowIfNull(action, "action");
		lock (_stateLock)
		{
			action(this);
		}
	}
}
