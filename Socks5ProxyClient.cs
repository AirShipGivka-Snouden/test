using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Proxies.Socks5Proxy;

namespace VpnHood.Core.Proxies.Socks5ProxyClients;

public class Socks5ProxyClient(Socks5ProxyClientOptions options, ILogger<Socks5ProxyClient>? logger = null) : IProxyClient
{
	private bool _isAuthenticated;

	public IPEndPoint ProxyEndPoint => options.ProxyEndPoint;

	public async Task ConnectAsync(TcpClient tcpClient, string host, int port, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(tcpClient, "tcpClient");
		ArgumentException.ThrowIfNullOrWhiteSpace(host, "host");
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port, "port");
		try
		{
			IPAddress[] array = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			IPAddress address = array.FirstOrDefault((IPAddress a) => a.AddressFamily == AddressFamily.InterNetwork) ?? array[0];
			await ConnectAsync(tcpClient, new IPEndPoint(address, port), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception exception)
		{
			logger?.LogError(exception, "Failed to resolve or connect to {Host}:{Port}", host, port);
			throw;
		}
	}

	public async Task CheckConnectionAsync(TcpClient tcpClient, CancellationToken cancellationToken)
	{
		_ = 1;
		try
		{
			tcpClient.NoDelay = true;
			await tcpClient.ConnectAsync(ProxyEndPoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			NetworkStream stream = tcpClient.GetStream();
			await EnsureAuthenticatedAsync(stream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			tcpClient.Close();
			throw;
		}
	}

	public async Task ConnectAsync(TcpClient tcpClient, IPEndPoint destination, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(tcpClient, "tcpClient");
		ArgumentNullException.ThrowIfNull(destination, "destination");
		logger?.LogDebug("Connecting to {Destination} through SOCKS5 proxy {ProxyEndPoint}", destination, ProxyEndPoint);
		try
		{
			if (!tcpClient.Connected)
			{
				tcpClient.NoDelay = true;
				await tcpClient.ConnectAsync(ProxyEndPoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			NetworkStream stream = tcpClient.GetStream();
			await EnsureAuthenticatedAsync(stream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await PerformConnectAsync(stream, destination, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			logger?.LogDebug("SOCKS5 CONNECT tunnel established to {Destination}", destination);
		}
		catch (Exception exception)
		{
			logger?.LogError(exception, "Failed to connect to {Destination} through SOCKS5 proxy", destination);
			tcpClient.Close();
			throw;
		}
	}

	public async Task<IPEndPoint> CreateUdpAssociateAsync(TcpClient tcpClient, CancellationToken cancellationToken)
	{
		return await CreateUdpAssociateAsync(tcpClient, null, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<IPEndPoint> CreateUdpAssociateAsync(TcpClient tcpClient, IPEndPoint? clientUdpEndPoint, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(tcpClient, "tcpClient");
		logger?.LogDebug("Creating UDP associate through SOCKS5 proxy");
		try
		{
			if (!tcpClient.Connected)
			{
				tcpClient.NoDelay = true;
				await tcpClient.ConnectAsync(ProxyEndPoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			NetworkStream stream = tcpClient.GetStream();
			await EnsureAuthenticatedAsync(stream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			IPEndPoint destination = clientUdpEndPoint ?? new IPEndPoint(IPAddress.Any, 0);
			Socks5CommandResult socks5CommandResult = await SendCommandAndReadReplyAsync(stream, Socks5Command.UdpAssociate, destination, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			IPAddress address = socks5CommandResult.BoundEndpoint.Address;
			if (address == null)
			{
				throw new NotSupportedException("Proxy returned an unsupported address type for UDP ASSOCIATE");
			}
			IPEndPoint iPEndPoint = new IPEndPoint((address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) ? ProxyEndPoint.Address : address, socks5CommandResult.BoundEndpoint.Port);
			logger?.LogDebug("UDP associate established on {UdpEndpoint}", iPEndPoint);
			return iPEndPoint;
		}
		catch (Exception exception)
		{
			logger?.LogError(exception, "Failed to create UDP associate through SOCKS5 proxy");
			throw;
		}
	}

	private async Task EnsureAuthenticatedAsync(NetworkStream stream, CancellationToken cancellationToken)
	{
		if (!_isAuthenticated)
		{
			await PerformAuthenticationAsync(stream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			_isAuthenticated = true;
		}
	}

	private async Task PerformAuthenticationAsync(NetworkStream stream, CancellationToken cancellationToken)
	{
		logger?.LogDebug("Performing SOCKS5 authentication negotiation");
		byte[] array = (string.IsNullOrEmpty(options.Username) ? new byte[3] { 5, 1, 0 } : new byte[4] { 5, 2, 0, 2 });
		await stream.WriteAsync(array, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		await stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		byte[] response = new byte[2];
		await stream.ReadExactlyAsync(response, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (response[0] != 5)
		{
			throw new ProtocolViolationException($"Invalid SOCKS version in response: {response[0]}");
		}
		Socks5AuthenticationType socks5AuthenticationType = (Socks5AuthenticationType)response[1];
		logger?.LogDebug("Server selected authentication method: {Method}", socks5AuthenticationType);
		switch (socks5AuthenticationType)
		{
		case Socks5AuthenticationType.NoAuthenticationRequired:
			logger?.LogDebug("No authentication required");
			break;
		case Socks5AuthenticationType.UsernamePassword:
			if (string.IsNullOrEmpty(options.Username))
			{
				throw new UnauthorizedAccessException("Server requires username/password authentication but no credentials provided");
			}
			await PerformUsernamePasswordAuthAsync(stream, options.Username, options.Password ?? string.Empty, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case Socks5AuthenticationType.ReplyNoAcceptableMethods:
			throw new UnauthorizedAccessException("No acceptable authentication methods found");
		default:
			throw new NotSupportedException($"Authentication method {socks5AuthenticationType} is not supported");
		}
	}

	private async Task PerformUsernamePasswordAuthAsync(NetworkStream stream, string username, string password, CancellationToken cancellationToken)
	{
		logger?.LogDebug("Performing username/password authentication for user: {Username}", username);
		Memory<byte> memory = ConstructAuthBuffer(username, password);
		await stream.WriteAsync(memory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		await stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		byte[] response = new byte[2];
		await stream.ReadExactlyAsync(response, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (response[0] != 1)
		{
			throw new ProtocolViolationException($"Invalid username/password auth version: {response[0]}");
		}
		if (response[1] != 0)
		{
			throw new UnauthorizedAccessException("Username/password authentication failed");
		}
		logger?.LogDebug("Username/password authentication successful");
	}

	private static async Task PerformConnectAsync(NetworkStream stream, IPEndPoint destination, CancellationToken cancellationToken)
	{
		Socks5CommandResult socks5CommandResult = await SendCommandAndReadReplyAsync(stream, Socks5Command.Connect, destination, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (socks5CommandResult.Reply != Socks5CommandReply.Succeeded)
		{
			throw MapSocksErrorToException(socks5CommandResult.Reply);
		}
	}

	private static async Task<Socks5CommandResult> SendCommandAndReadReplyAsync(NetworkStream stream, Socks5Command command, IPEndPoint destination, CancellationToken cancellationToken)
	{
		byte addressType = GetAddressType(destination.AddressFamily);
		byte[] addressBytes = destination.Address.GetAddressBytes();
		byte[] bytes = BitConverter.GetBytes((ushort)destination.Port);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		byte[] array = new byte[4 + addressBytes.Length + 2];
		array[0] = 5;
		array[1] = (byte)command;
		array[2] = 0;
		array[3] = addressType;
		addressBytes.CopyTo(array, 4);
		bytes.CopyTo(array, 4 + addressBytes.Length);
		await stream.WriteAsync(array, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		await stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		byte[] responseHeader = new byte[4];
		await stream.ReadExactlyAsync(responseHeader, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (responseHeader[0] != 5)
		{
			throw new ProtocolViolationException($"Invalid SOCKS version in reply: {responseHeader[0]}");
		}
		Socks5CommandReply reply = (Socks5CommandReply)responseHeader[1];
		Socks5AddressType addressType2 = (Socks5AddressType)responseHeader[3];
		return new Socks5CommandResult(command, reply, await ReadAddressPortAsync(stream, addressType2, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
	}

	private static async Task<Socks5Endpoint> ReadAddressPortAsync(NetworkStream stream, Socks5AddressType addressType, CancellationToken cancellationToken)
	{
		switch (addressType)
		{
		case Socks5AddressType.IpV4:
		{
			byte[] ipv4Buffer = new byte[6];
			await stream.ReadExactlyAsync(ipv4Buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			IPAddress address2 = new IPAddress(ipv4Buffer.AsSpan(0, 4));
			int port3 = (ipv4Buffer[4] << 8) | ipv4Buffer[5];
			return new Socks5Endpoint(null, address2, port3);
		}
		case Socks5AddressType.IpV6:
		{
			byte[] ipv6Buffer = new byte[18];
			await stream.ReadExactlyAsync(ipv6Buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			IPAddress address = new IPAddress(ipv6Buffer.AsSpan(0, 16));
			int port2 = (ipv6Buffer[16] << 8) | ipv6Buffer[17];
			return new Socks5Endpoint(null, address, port2);
		}
		case Socks5AddressType.DomainName:
		{
			byte[] lengthBuffer = new byte[1];
			await stream.ReadExactlyAsync(lengthBuffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			byte domainLength = lengthBuffer[0];
			byte[] domainBuffer = new byte[domainLength + 2];
			await stream.ReadExactlyAsync(domainBuffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			string host = Encoding.UTF8.GetString(domainBuffer.AsSpan(0, domainLength));
			int port = (domainBuffer[domainLength] << 8) | domainBuffer[domainLength + 1];
			return new Socks5Endpoint(host, null, port);
		}
		default:
			throw new NotSupportedException($"Unsupported address type in reply: {addressType}");
		}
	}

	private static byte GetAddressType(AddressFamily addressFamily)
	{
		return addressFamily switch
		{
			AddressFamily.InterNetwork => 1, 
			AddressFamily.InterNetworkV6 => 4, 
			_ => throw new NotSupportedException($"Unsupported address family: {addressFamily}"), 
		};
	}

	private static Memory<byte> ConstructAuthBuffer(string username, string password)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(username);
		byte[] bytes2 = Encoding.UTF8.GetBytes(password);
		if (bytes.Length > 255)
		{
			throw new ArgumentException("Username exceeds maximum length of 255 bytes", "username");
		}
		if (bytes2.Length > 255)
		{
			throw new ArgumentException("Password exceeds maximum length of 255 bytes", "password");
		}
		byte[] array = new byte[3 + bytes.Length + bytes2.Length];
		array[0] = 1;
		array[1] = (byte)bytes.Length;
		bytes.CopyTo(array, 2);
		array[2 + bytes.Length] = (byte)bytes2.Length;
		bytes2.CopyTo(array, 3 + bytes.Length);
		return new Memory<byte>(array);
	}

	private static Exception MapSocksErrorToException(Socks5CommandReply reply)
	{
		return reply switch
		{
			Socks5CommandReply.GeneralSocksServerFailure => new ProxyClientException(SocketError.SocketError, "General SOCKS server failure"), 
			Socks5CommandReply.ConnectionNotAllowedByRuleset => new ProxyClientException(SocketError.AccessDenied, "Connection not allowed by ruleset"), 
			Socks5CommandReply.NetworkUnreachable => new ProxyClientException(SocketError.NetworkUnreachable, "Network unreachable"), 
			Socks5CommandReply.HostUnreachable => new ProxyClientException(SocketError.HostUnreachable, "Host unreachable"), 
			Socks5CommandReply.ConnectionRefused => new ProxyClientException(SocketError.ConnectionRefused, "Connection refused"), 
			Socks5CommandReply.TtlExpired => new ProxyClientException(SocketError.TimedOut, "TTL expired"), 
			Socks5CommandReply.CommandNotSupported => new ProxyClientException(SocketError.OperationNotSupported, "Command not supported"), 
			Socks5CommandReply.AddressTypeNotSupported => new ProxyClientException(SocketError.AddressFamilyNotSupported, "Address type not supported"), 
			_ => new ProxyClientException(SocketError.ProtocolNotSupported, $"Unknown SOCKS5 error: {reply}"), 
		};
	}

	public static int WriteUdpRequest(Span<byte> destinationBuffer, IPEndPoint destination, ReadOnlySpan<byte> data)
	{
		ArgumentNullException.ThrowIfNull(destination, "destination");
		byte[] addressBytes = destination.Address.GetAddressBytes();
		byte addressType = GetAddressType(destination.AddressFamily);
		int num = 4 + addressBytes.Length + 2 + data.Length;
		if (destinationBuffer.Length < num)
		{
			throw new ArgumentException($"Destination buffer too small. Required: {num}, Available: {destinationBuffer.Length}", "destinationBuffer");
		}
		int num2 = 0;
		destinationBuffer[num2++] = 0;
		destinationBuffer[num2++] = 0;
		destinationBuffer[num2++] = 0;
		destinationBuffer[num2++] = addressType;
		int num3 = num2;
		addressBytes.CopyTo(destinationBuffer.Slice(num3, destinationBuffer.Length - num3));
		num2 += addressBytes.Length;
		destinationBuffer[num2++] = (byte)(destination.Port >> 8);
		destinationBuffer[num2++] = (byte)(destination.Port & 0xFF);
		num3 = num2;
		data.CopyTo(destinationBuffer.Slice(num3, destinationBuffer.Length - num3));
		return num2 + data.Length;
	}

	public static Socks5Endpoint ParseUdpResponse(ReadOnlySpan<byte> datagram, out ReadOnlySpan<byte> payload)
	{
		if (datagram.Length < 7)
		{
			throw new ArgumentException("Datagram too short for SOCKS5 UDP response", "datagram");
		}
		if (datagram[0] != 0 || datagram[1] != 0 || datagram[2] != 0)
		{
			throw new NotSupportedException("Fragmented or malformed SOCKS5 UDP packet");
		}
		Socks5AddressType socks5AddressType = (Socks5AddressType)datagram[3];
		int num = 4;
		string host = null;
		IPAddress address = null;
		switch (socks5AddressType)
		{
		case Socks5AddressType.IpV4:
			address = new IPAddress(datagram.Slice(num, 4));
			num += 4;
			break;
		case Socks5AddressType.IpV6:
			address = new IPAddress(datagram.Slice(num, 16));
			num += 16;
			break;
		case Socks5AddressType.DomainName:
		{
			byte b = datagram[num++];
			host = Encoding.UTF8.GetString(datagram.Slice(num, b));
			num += b;
			break;
		}
		default:
			throw new NotSupportedException($"Unsupported address type in UDP response: {socks5AddressType}");
		}
		int port = (datagram[num] << 8) | datagram[num + 1];
		num += 2;
		int num2 = num;
		payload = datagram.Slice(num2, datagram.Length - num2);
		return new Socks5Endpoint(host, address, port);
	}
}
