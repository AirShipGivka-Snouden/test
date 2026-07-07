using VpnHood.Core.Client.Abstractions;

namespace Ciphra.VPN.Common.Services;

public static class VpnServiceExtensions
{
	public static bool CanDisconnect(this ClientState clientState)
	{
		return clientState != ClientState.Disconnecting && clientState != ClientState.Disposed && clientState != ClientState.None;
	}
}
