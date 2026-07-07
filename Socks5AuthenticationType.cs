namespace VpnHood.Core.Proxies.Socks5Proxy;

public enum Socks5AuthenticationType : byte
{
	NoAuthenticationRequired = 0,
	Gssapi = 1,
	UsernamePassword = 2,
	IanaAssignedRangeBegin = 3,
	IanaAssignedRangeEnd = 127,
	ReservedRangeBegin = 128,
	ReservedRangeEnd = 254,
	ReplyNoAcceptableMethods = byte.MaxValue
}
