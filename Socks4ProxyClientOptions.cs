using System.Net;

namespace VpnHood.Core.Proxies.Socks4ProxyClients;

public class Socks4ProxyClientOptions
{
	public required IPEndPoint ProxyEndPoint { get; init; }

	public string? UserName { get; init; }
}
