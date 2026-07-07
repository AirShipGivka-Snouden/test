using System;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.Models;

namespace Ciphra.VPN.Common.Services.Interfaces;

public interface IVpnService : IDisposable
{
	IObservable<SmartHealCandidate> SmartHealSucceeded { get; }

	IObservable<bool> IsSmartHealInProgress { get; }

	ConnectionStateDto? CheckConnectionState(bool createIfMissing = false);

	Task ConnectToServerAsync(VpnServerDto vpnServer, CancellationToken ct, ConnectionSettings? overrideSettings = null);

	Task DisconnectAsync(TimeSpan? timeout = null);
}
