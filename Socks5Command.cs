namespace VpnHood.Core.Proxies.Socks5Proxy;

public enum Socks5Command : byte
{
	Connect = 1,
	Bind,
	UdpAssociate
}
