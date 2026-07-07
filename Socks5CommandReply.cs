namespace VpnHood.Core.Proxies.Socks5Proxy;

public enum Socks5CommandReply : byte
{
	Succeeded,
	GeneralSocksServerFailure,
	ConnectionNotAllowedByRuleset,
	NetworkUnreachable,
	HostUnreachable,
	ConnectionRefused,
	TtlExpired,
	CommandNotSupported,
	AddressTypeNotSupported
}
