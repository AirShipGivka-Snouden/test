using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Proxies.EndPointManagement.Abstractions;
using VpnHood.Core.Proxies.HttpProxyClients;
using VpnHood.Core.Proxies.Socks4ProxyClients;
using VpnHood.Core.Proxies.Socks5ProxyClients;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Proxies.EndPointManagement;

public static class ProxyClientFactory
{
	public static async Task<IPAddress> GetIpAddress(string host, CancellationToken cancellationToken)
	{
		if (IPAddress.TryParse(host, out IPAddress address))
		{
			return address;
		}
		IPHostEntry iPHostEntry = await Dns.GetHostEntryAsync(host, cancellationToken).Vhc();
		if (iPHostEntry.AddressList.Length == 0)
		{
			throw new Exception("Failed to resolve proxy server address.");
		}
		return iPHostEntry.AddressList[Random.Shared.Next(iPHostEntry.AddressList.Length)];
	}

	public static async Task<IProxyClient> CreateProxyClient(ProxyEndPoint proxyEndPoint, CancellationToken cancellationToken)
	{
		IPEndPoint proxyEndPoint2 = new IPEndPoint(await GetIpAddress(proxyEndPoint.Host, cancellationToken).Vhc(), proxyEndPoint.Port);
		IProxyClient result;
		switch (proxyEndPoint.Protocol)
		{
		case ProxyProtocol.Socks5:
			result = new Socks5ProxyClient(new Socks5ProxyClientOptions
			{
				ProxyEndPoint = proxyEndPoint2,
				Password = proxyEndPoint.Password,
				Username = proxyEndPoint.Username
			});
			break;
		case ProxyProtocol.Socks4:
			result = new Socks4ProxyClient(new Socks4ProxyClientOptions
			{
				ProxyEndPoint = proxyEndPoint2,
				UserName = proxyEndPoint.Username
			});
			break;
		case ProxyProtocol.Http:
		case ProxyProtocol.Https:
			result = new HttpProxyClient(new HttpProxyClientOptions
			{
				ProxyEndPoint = proxyEndPoint2,
				Username = proxyEndPoint.Username,
				Password = proxyEndPoint.Password,
				AllowInvalidCertificates = true,
				ProxyHost = proxyEndPoint.Host,
				UseTls = (proxyEndPoint.Protocol == ProxyProtocol.Https)
			});
			break;
		default:
			throw new NotSupportedException($"Proxy type {proxyEndPoint.Protocol} is not supported.");
		}
		return result;
	}
}
