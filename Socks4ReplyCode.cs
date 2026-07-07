namespace VpnHood.Core.Proxies.Socks4ProxyClients;

public enum Socks4ReplyCode : byte
{
	RequestGranted = 90,
	RequestRejectedOrFailed,
	CannotConnectToIdentd,
	DifferingUserId
}
