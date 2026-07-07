using System;
using System.Net;

namespace VpnHood.Core.Proxies.Socks5ProxyServers;

public class Socks5ProxyServerOptions
{
	public required IPEndPoint ListenEndPoint { get; init; }

	public string? Username { get; init; }

	public string? Password { get; init; }

	public TimeSpan HandshakeTimeout { get; init; } = TimeSpan.FromSeconds(15L);

	public TimeSpan HostConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30L);

	public int Backlog { get; init; } = 512;
}
