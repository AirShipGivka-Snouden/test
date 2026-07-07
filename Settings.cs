using System;
using System.Collections.Generic;
using System.Linq;
using Ciphra.VPN.Common.Models;
using VpnHood.Core.Common.Messaging;

namespace Ciphra.VPN.Common.App;

public class Settings
{
	private readonly JsonAppSettingsTransport _transport;

	private readonly object _wintunNamesLock = new object();

	public SplitTunnelSettings SplitTunnel { get; }

	public string? UserId => _transport.LoadOrInitProperty(Guid.NewGuid().ToString(), "UserId");

	public DateTime UserCreatedAt => _transport.LoadOrInitProperty(DateTime.UtcNow, "UserCreatedAt");

	public string? LastUsedServerId
	{
		get
		{
			return _transport.LoadOrInitProperty<string>(null, "LastUsedServerId");
		}
		set
		{
			_transport.Save(value, "LastUsedServerId");
		}
	}

	public string? UserToken
	{
		get
		{
			return _transport.LoadOrInitProperty<string>(null, "UserToken");
		}
		set
		{
			_transport.Save(value, "UserToken");
		}
	}

	public string? AnonymousUserToken
	{
		get
		{
			return _transport.LoadOrInitProperty<string>(null, "AnonymousUserToken");
		}
		set
		{
			_transport.Save(value, "AnonymousUserToken");
		}
	}

	public bool IsUserTokenValid
	{
		get
		{
			return _transport.LoadOrInitProperty(defaultValue: false, "IsUserTokenValid");
		}
		set
		{
			_transport.Save(value, "IsUserTokenValid");
		}
	}

	public string? StripeCustomerId
	{
		get
		{
			return _transport.LoadOrInitProperty<string>(null, "StripeCustomerId");
		}
		set
		{
			_transport.Save(value, "StripeCustomerId");
		}
	}

	public bool AutoConnect
	{
		get
		{
			return _transport.LoadOrInitProperty(defaultValue: false, "AutoConnect");
		}
		set
		{
			_transport.Save(value, "AutoConnect");
		}
	}

	public string? Language
	{
		get
		{
			return _transport.LoadOrInitProperty<string>(null, "Language");
		}
		set
		{
			_transport.Save(value, "Language");
		}
	}

	public bool UseWinTunOnlyAdapter
	{
		get
		{
			return _transport.LoadOrInitProperty(defaultValue: false, "UseWinTunOnlyAdapter");
		}
		set
		{
			_transport.Save(value, "UseWinTunOnlyAdapter");
		}
	}

	public bool UserDisconnected
	{
		get
		{
			return _transport.LoadOrInitProperty(defaultValue: false, "UserDisconnected");
		}
		set
		{
			_transport.Save(value, "UserDisconnected");
		}
	}

	public bool IsRated
	{
		get
		{
			return _transport.LoadOrInitProperty(defaultValue: false, "IsRated");
		}
		set
		{
			_transport.Save(value, "IsRated");
		}
	}

	public bool? AnalyticsConsent
	{
		get
		{
			return _transport.LoadOrInitProperty<bool?>(null, "AnalyticsConsent");
		}
		set
		{
			_transport.Save(value, "AnalyticsConsent");
		}
	}

	public bool DropQuic
	{
		get
		{
			return _transport.LoadOrInitProperty(defaultValue: false, "DropQuic");
		}
		set
		{
			_transport.Save(value, "DropQuic");
		}
	}

	public bool DropUdp
	{
		get
		{
			return _transport.LoadOrInitProperty(defaultValue: false, "DropUdp");
		}
		set
		{
			_transport.Save(value, "DropUdp");
		}
	}

	public ChannelProtocol ChannelProtocol
	{
		get
		{
			return _transport.LoadOrInitProperty(ChannelProtocol.Tcp, "ChannelProtocol");
		}
		set
		{
			_transport.Save(value, "ChannelProtocol");
		}
	}

	public bool IsSmartHealEnabled
	{
		get
		{
			return _transport.LoadOrInitProperty(defaultValue: false, "IsSmartHealEnabled");
		}
		set
		{
			_transport.Save(value, "IsSmartHealEnabled");
		}
	}

	public int RatingRetryCounter
	{
		get
		{
			return _transport.LoadOrInitProperty(0, "RatingRetryCounter");
		}
		set
		{
			_transport.Save(value, "RatingRetryCounter");
		}
	}

	public int RatingCounter
	{
		get
		{
			return _transport.LoadOrInitProperty(0, "RatingCounter");
		}
		set
		{
			_transport.Save(value, "RatingCounter");
		}
	}

	public int PromoCounter
	{
		get
		{
			return _transport.LoadOrInitProperty(0, "PromoCounter");
		}
		set
		{
			_transport.Save(value, "PromoCounter");
		}
	}

	public DateTime TrialExpiryDate
	{
		get
		{
			return _transport.LoadOrInitProperty(DateTime.MinValue, "TrialExpiryDate");
		}
		set
		{
			_transport.Save(value, "TrialExpiryDate");
		}
	}

	public DateTime TrialDialogDismissedDate
	{
		get
		{
			return _transport.LoadOrInitProperty(DateTime.MinValue, "TrialDialogDismissedDate");
		}
		set
		{
			_transport.Save(value, "TrialDialogDismissedDate");
		}
	}

	public long LastMaxTraffic
	{
		get
		{
			return _transport.LoadOrInitProperty(0L, "LastMaxTraffic");
		}
		set
		{
			_transport.Save(value, "LastMaxTraffic");
		}
	}

	public long AggregateTrafficUsed
	{
		get
		{
			return _transport.LoadOrInitProperty(0L, "AggregateTrafficUsed");
		}
		set
		{
			_transport.Save(value, "AggregateTrafficUsed");
		}
	}

	public DateTime TrafficCycleExpiration
	{
		get
		{
			return _transport.LoadOrInitProperty(DateTime.MinValue, "TrafficCycleExpiration");
		}
		set
		{
			_transport.Save(value, "TrafficCycleExpiration");
		}
	}

	public string? RelayDomain
	{
		get
		{
			return _transport.LoadOrInitProperty<string>(null, "RelayDomain");
		}
		set
		{
			_transport.Save(value, "RelayDomain");
		}
	}

	public List<RelayFailureEntry> RelayFailureLog
	{
		get
		{
			return _transport.LoadOrInitProperty(new List<RelayFailureEntry>(), "RelayFailureLog");
		}
		set
		{
			_transport.Save(value, "RelayFailureLog");
		}
	}

	public string? WintunAdapterCurrentName
	{
		get
		{
			return _transport.LoadOrInitProperty<string>(null, "WintunAdapterCurrentName");
		}
		set
		{
			_transport.Save(value, "WintunAdapterCurrentName");
		}
	}

	public IReadOnlyList<string> KnownWintunAdapterNames
	{
		get
		{
			lock (_wintunNamesLock)
			{
				List<string> list = _transport.LoadOrInitProperty(new List<string>(), "KnownWintunAdapterNames") ?? new List<string>();
				return list.ToArray();
			}
		}
	}

	public Settings(JsonAppSettingsTransport settingsTransport)
	{
		_transport = settingsTransport ?? throw new ArgumentNullException("settingsTransport");
		SplitTunnel = new SplitTunnelSettings(_transport);
	}

	public void AddKnownWintunAdapterName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return;
		}
		lock (_wintunNamesLock)
		{
			List<string> list = _transport.LoadOrInitProperty(new List<string>(), "KnownWintunAdapterNames") ?? new List<string>();
			if (!list.Contains(name))
			{
				List<string> value = new List<string>(list) { name };
				_transport.Save(value, "KnownWintunAdapterNames");
			}
		}
	}

	public void PruneKnownWintunAdapterNames(IEnumerable<string> namesToRemove)
	{
		HashSet<string> toRemove = namesToRemove?.ToHashSet() ?? new HashSet<string>();
		if (toRemove.Count == 0)
		{
			return;
		}
		lock (_wintunNamesLock)
		{
			List<string> list = _transport.LoadOrInitProperty(new List<string>(), "KnownWintunAdapterNames") ?? new List<string>();
			List<string> list2 = list.Where((string n) => !toRemove.Contains(n)).ToList();
			if (list2.Count != list.Count)
			{
				_transport.Save(list2, "KnownWintunAdapterNames");
			}
		}
	}
}
