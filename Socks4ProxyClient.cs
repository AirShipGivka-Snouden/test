using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VpnHood.Core.Proxies.Socks4ProxyClients;

public class Socks4ProxyClient(Socks4ProxyClientOptions options) : IProxyClient
{
	public IPEndPoint ProxyEndPoint => options.ProxyEndPoint;

	public Task ConnectAsync(TcpClient tcpClient, IPEndPoint destination, CancellationToken cancellationToken)
	{
		return ConnectAsync(tcpClient, destination.Address.ToString(), destination.Port, cancellationToken);
	}

	public async Task ConnectAsync(TcpClient tcpClient, string host, int port, CancellationToken cancellationToken)
	{
		_ = 3;
		try
		{
			if (!tcpClient.Connected)
			{
				await tcpClient.ConnectAsync(options.ProxyEndPoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			NetworkStream stream = tcpClient.GetStream();
			IPAddress address;
			bool flag = IPAddress.TryParse(host, out address) && address.AddressFamily == AddressFamily.InterNetwork;
			if (!flag)
			{
				address = IPAddress.Parse("0.0.0.1");
			}
			ReadOnlyMemory<byte> buffer = BuildRequest(address, port, flag ? null : host, options.UserName);
			await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			byte[] reply = new byte[8];
			await stream.ReadExactlyAsync(reply, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			Socks4ReplyCode socks4ReplyCode = (Socks4ReplyCode)reply[1];
			if (socks4ReplyCode != Socks4ReplyCode.RequestGranted)
			{
				ThrowSocks4Error(socks4ReplyCode);
			}
		}
		catch
		{
			tcpClient.Close();
			throw;
		}
	}

	public async Task CheckConnectionAsync(TcpClient tcpClient, CancellationToken cancellationToken)
	{
		try
		{
			tcpClient.NoDelay = true;
			await tcpClient.ConnectAsync(options.ProxyEndPoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			tcpClient.Close();
			throw;
		}
	}

	private static ReadOnlyMemory<byte> BuildRequest(IPAddress ip, int port, string? domainName, string? userId)
	{
		if (ip.AddressFamily == AddressFamily.InterNetworkV6)
		{
			throw new ProxyClientException(SocketError.OperationNotSupported, "SOCKS4 only supports IPv4 addresses.");
		}
		string s = userId ?? string.Empty;
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		byte[] array = ((domainName != null) ? Encoding.UTF8.GetBytes(domainName) : null);
		byte[] addressBytes = ip.GetAddressBytes();
		byte[] array2 = new byte[8 + bytes.Length + 1 + ((array != null) ? array.Length : 0) + ((domainName != null) ? 1 : 0)];
		int num = 0;
		array2[num++] = 4;
		array2[num++] = 1;
		array2[num++] = (byte)(port >> 8);
		array2[num++] = (byte)(port & 0xFF);
		Array.Copy(addressBytes, 0, array2, num, 4);
		num += 4;
		if (bytes.Length != 0)
		{
			Array.Copy(bytes, 0, array2, num, bytes.Length);
		}
		num += bytes.Length;
		array2[num++] = 0;
		if (array != null)
		{
			Array.Copy(array, 0, array2, num, array.Length);
			num += array.Length;
			array2[num++] = 0;
		}
		return new ReadOnlyMemory<byte>(array2, 0, num);
	}

	private static void ThrowSocks4Error(Socks4ReplyCode code)
	{
		throw new ProxyClientException(code switch
		{
			Socks4ReplyCode.RequestRejectedOrFailed => SocketError.AccessDenied, 
			Socks4ReplyCode.CannotConnectToIdentd => SocketError.ConnectionRefused, 
			Socks4ReplyCode.DifferingUserId => SocketError.AccessDenied, 
			_ => SocketError.SocketError, 
		});
	}
}
