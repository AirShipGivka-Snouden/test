using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Proxies.Socks5Proxy;

namespace VpnHood.Core.Proxies.Socks5ProxyServers;

public sealed class Socks5ProxyServer(Socks5ProxyServerOptions options, ILogger<Socks5ProxyServer>? logger = null) : TcpProxyServerBase(options.ListenEndPoint, options.Backlog, logger)
{
	protected override async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
	{
		IPEndPoint clientEndpoint = (client.Client.RemoteEndPoint as IPEndPoint) ?? new IPEndPoint(IPAddress.None, 0);
		Logger.LogDebug("Handling SOCKS5 client connection from {ClientEndpoint}", clientEndpoint);
		try
		{
			using (client)
			{
				client.NoDelay = true;
				NetworkStream networkStream = client.GetStream();
				Socks5HandshakeResult socks5HandshakeResult = await PerformHandshakeAsync(networkStream, clientEndpoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (!socks5HandshakeResult.IsValid)
				{
					return;
				}
				Logger.LogDebug("Processing {Command} command from {ClientEndpoint}", socks5HandshakeResult.RequestHeader.Command, clientEndpoint);
				switch (socks5HandshakeResult.RequestHeader.Command)
				{
				case Socks5Command.Connect:
				{
					IPEndPoint iPEndPoint = await ReadDestinationAsync(networkStream, socks5HandshakeResult.RequestHeader.AddressType, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					await HandleConnectCommandAsync(networkStream, iPEndPoint.Address, iPEndPoint.Port, cancellationToken, clientEndpoint).ConfigureAwait(continueOnCapturedContext: false);
					break;
				}
				case Socks5Command.UdpAssociate:
				{
					UdpAssociateResult udpAssociateResult = await HandleUdpAssociateCommandAsync(networkStream, client, socks5HandshakeResult.RequestHeader.AddressType, clientEndpoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					await SendReplyAsync(networkStream, Socks5CommandReply.Succeeded, udpAssociateResult.BindAddress, udpAssociateResult.BindPort, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					byte[] array = new byte[1];
					try
					{
						await networkStream.ReadAsync(array, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					}
					catch (Exception exception)
					{
						Logger.LogDebug(exception, "TCP connection closed for UDP associate with {ClientEndpoint}", clientEndpoint);
					}
					break;
				}
				default:
					Logger.LogWarning("Unsupported command {Command} from {ClientEndpoint}", socks5HandshakeResult.RequestHeader.Command, clientEndpoint);
					await SendReplyAsync(networkStream, Socks5CommandReply.CommandNotSupported, IPAddress.Any, 0, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					break;
				}
			}
		}
		catch (OperationCanceledException)
		{
			Logger.LogDebug("Client connection cancelled for {ClientEndpoint}", clientEndpoint);
		}
		catch (Exception exception2)
		{
			Logger.LogError(exception2, "Error handling SOCKS5 client {ClientEndpoint}", clientEndpoint);
		}
	}

	private async Task<Socks5HandshakeResult> PerformHandshakeAsync(NetworkStream networkStream, IPEndPoint clientEndpoint, CancellationToken cancellationToken)
	{
		_ = 2;
		try
		{
			using CancellationTokenSource handshakeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			handshakeCts.CancelAfter(options.HandshakeTimeout);
			Socks5AuthenticationType authType = await NegotiateAuthAsync(networkStream, options.Username != null, handshakeCts.Token).ConfigureAwait(continueOnCapturedContext: false);
			if (authType == Socks5AuthenticationType.UsernamePassword)
			{
				bool flag = await HandleUserPassAuthAsync(networkStream, options.Username, options.Password ?? string.Empty, handshakeCts.Token).ConfigureAwait(continueOnCapturedContext: false);
				Logger.LogDebug("Authentication result for {ClientEndpoint}", clientEndpoint);
				if (!flag)
				{
					return Socks5HandshakeResult.Invalid;
				}
			}
			return Socks5HandshakeResult.Valid(authType, await ReadRequestHeaderAsync(networkStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
		}
		catch (OperationCanceledException)
		{
			Logger.LogDebug("Handshake cancelled for {ClientEndpoint}", clientEndpoint);
			return Socks5HandshakeResult.Invalid;
		}
		catch (Exception exception)
		{
			Logger.LogError(exception, "Error during handshake for {ClientEndpoint}", clientEndpoint);
			return Socks5HandshakeResult.Invalid;
		}
	}

	private async Task<Socks5AuthenticationType> NegotiateAuthAsync(NetworkStream stream, bool requireAuth, CancellationToken cancellationToken)
	{
		byte[] header = new byte[2];
		await stream.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (header[0] != 5)
		{
			throw new ProtocolViolationException($"Invalid SOCKS version: {header[0]}");
		}
		byte b = header[1];
		if (b == 0)
		{
			throw new ProtocolViolationException("No authentication methods provided");
		}
		byte[] methods = new byte[b];
		await stream.ReadExactlyAsync(methods, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		bool flag = ((ReadOnlySpan<byte>)methods).Contains((byte)2);
		bool flag2 = ((ReadOnlySpan<byte>)methods).Contains((byte)0);
		Socks5AuthenticationType selectedAuthType;
		if (requireAuth)
		{
			if (!flag)
			{
				await stream.WriteAsync(new byte[2] { 5, 255 }, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				throw new UnauthorizedAccessException("Client does not support required authentication method");
			}
			selectedAuthType = Socks5AuthenticationType.UsernamePassword;
		}
		else
		{
			selectedAuthType = ((!flag2) ? (flag ? Socks5AuthenticationType.UsernamePassword : Socks5AuthenticationType.ReplyNoAcceptableMethods) : Socks5AuthenticationType.NoAuthenticationRequired);
			if (selectedAuthType == Socks5AuthenticationType.ReplyNoAcceptableMethods)
			{
				await stream.WriteAsync(new byte[2]
				{
					5,
					(byte)selectedAuthType
				}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				throw new UnauthorizedAccessException("No acceptable authentication methods found");
			}
		}
		await stream.WriteAsync(new byte[2]
		{
			5,
			(byte)selectedAuthType
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return selectedAuthType;
	}

	private static async Task<bool> HandleUserPassAuthAsync(NetworkStream stream, string expectedUsername, string expectedPassword, CancellationToken cancellationToken)
	{
		byte[] version = new byte[1];
		await stream.ReadExactlyAsync(version, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (version[0] != 1)
		{
			await stream.WriteAsync(new byte[2] { 1, 255 }, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return false;
		}
		byte[] usernameLengthBuffer = new byte[1];
		await stream.ReadExactlyAsync(usernameLengthBuffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		byte b = usernameLengthBuffer[0];
		byte[] usernameBytes = new byte[b];
		await stream.ReadExactlyAsync(usernameBytes, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		byte[] passwordLengthBuffer = new byte[1];
		await stream.ReadExactlyAsync(passwordLengthBuffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		byte b2 = passwordLengthBuffer[0];
		byte[] passwordBytes = new byte[b2];
		await stream.ReadExactlyAsync(passwordBytes, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		string a = Encoding.UTF8.GetString(usernameBytes);
		string a2 = Encoding.UTF8.GetString(passwordBytes);
		bool isValid = string.Equals(a, expectedUsername, StringComparison.Ordinal) && string.Equals(a2, expectedPassword, StringComparison.Ordinal);
		await stream.WriteAsync(new byte[2]
		{
			1,
			(byte)((!isValid) ? 255u : 0u)
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return isValid;
	}

	private static async Task<RequestHeader> ReadRequestHeaderAsync(NetworkStream stream, CancellationToken cancellationToken)
	{
		byte[] requestHeaderBytes = new byte[4];
		await stream.ReadExactlyAsync(requestHeaderBytes, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (requestHeaderBytes[0] != 5)
		{
			throw new ProtocolViolationException($"Invalid SOCKS version in request: {requestHeaderBytes[0]}");
		}
		return new RequestHeader
		{
			Command = (Socks5Command)requestHeaderBytes[1],
			AddressType = (Socks5AddressType)requestHeaderBytes[3]
		};
	}

	private async Task HandleConnectCommandAsync(NetworkStream clientStream, IPAddress destinationAddress, int destinationPort, CancellationToken cancellationToken, IPEndPoint clientEndpoint)
	{
		try
		{
			Logger.LogDebug("Connecting to {DestAddress}:{DestPort} for {ClientEndpoint}", destinationAddress, destinationPort, clientEndpoint);
			using TcpClient remoteClient = new TcpClient(destinationAddress.AddressFamily);
			remoteClient.NoDelay = true;
			using (CancellationTokenSource connectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				connectionCts.CancelAfter(options.HostConnectionTimeout);
				await remoteClient.ConnectAsync(destinationAddress, destinationPort, connectionCts.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
			IPEndPoint iPEndPoint = (IPEndPoint)remoteClient.Client.LocalEndPoint;
			await SendReplyAsync(clientStream, Socks5CommandReply.Succeeded, iPEndPoint.Address, iPEndPoint.Port, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			Logger.LogDebug("Tunneling established between {ClientEndpoint} and {DestAddress}:{DestPort}", clientEndpoint, destinationAddress, destinationPort);
			NetworkStream stream = remoteClient.GetStream();
			await PumpStreamsAsync(clientStream, stream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
			await SendReplyAsync(clientStream, Socks5CommandReply.TtlExpired, IPAddress.Any, 0, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (SocketException ex2) when (ex2.SocketErrorCode == SocketError.ConnectionRefused)
		{
			await SendReplyAsync(clientStream, Socks5CommandReply.ConnectionRefused, IPAddress.Any, 0, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (SocketException ex3) when (ex3.SocketErrorCode == SocketError.HostUnreachable)
		{
			await SendReplyAsync(clientStream, Socks5CommandReply.HostUnreachable, IPAddress.Any, 0, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (SocketException ex4) when (ex4.SocketErrorCode == SocketError.NetworkUnreachable)
		{
			await SendReplyAsync(clientStream, Socks5CommandReply.NetworkUnreachable, IPAddress.Any, 0, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception exception)
		{
			Logger.LogError(exception, "Failed to establish connection to {DestAddress}:{DestPort} for {ClientEndpoint}", destinationAddress, destinationPort, clientEndpoint);
			await SendReplyAsync(clientStream, Socks5CommandReply.GeneralSocksServerFailure, IPAddress.Any, 0, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async Task<UdpAssociateResult> HandleUdpAssociateCommandAsync(NetworkStream stream, TcpClient controlTcpClient, Socks5AddressType addressType, IPEndPoint clientEndpoint, CancellationToken cancellationToken)
	{
		await ReadDestinationAsync(stream, addressType, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
		IPEndPoint iPEndPoint = (IPEndPoint)udpClient.Client.LocalEndPoint;
		CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		UdpRelayLoopAsync(udpClient, clientEndpoint, cancellationTokenSource.Token);
		MonitorTcpConnectionAsync(controlTcpClient, cancellationTokenSource, udpClient);
		Logger.LogDebug("UDP associate established for {ClientEndpoint} on port {Port}", clientEndpoint, iPEndPoint.Port);
		return new UdpAssociateResult
		{
			BindAddress = IPAddress.Any,
			BindPort = iPEndPoint.Port
		};
	}

	private async Task MonitorTcpConnectionAsync(TcpClient tcpClient, CancellationTokenSource cts, UdpClient udpClient)
	{
		try
		{
			byte[] array = new byte[1];
			await tcpClient.GetStream().ReadAsync(array, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
		}
		finally
		{
			try
			{
				udpClient.Dispose();
				await cts.CancelAsync();
			}
			catch (Exception exception)
			{
				Logger.LogError(exception, "Error during UDP associate cleanup");
			}
			finally
			{
				cts.Dispose();
			}
		}
	}

	private async Task UdpRelayLoopAsync(UdpClient proxyUdpClient, IPEndPoint clientEndpoint, CancellationToken cancellationToken)
	{
		IPEndPoint clientUdpEndpoint = null;
		try
		{
			Logger.LogDebug("Starting UDP relay loop for {ClientEndpoint}", clientEndpoint);
			while (!cancellationToken.IsCancellationRequested)
			{
				UdpReceiveResult udpReceiveResult = await proxyUdpClient.ReceiveAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				IPEndPoint remoteEndPoint = udpReceiveResult.RemoteEndPoint;
				byte[] buffer = udpReceiveResult.Buffer;
				if (clientUdpEndpoint == null || remoteEndPoint.Equals(clientUdpEndpoint))
				{
					if (clientUdpEndpoint == null)
					{
						clientUdpEndpoint = remoteEndPoint;
					}
					await HandleUdpClientToDestinationAsync(proxyUdpClient, buffer, clientUdpEndpoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					await HandleUdpDestinationToClientAsync(proxyUdpClient, buffer, remoteEndPoint, clientUdpEndpoint, clientUdpEndpoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		catch (OperationCanceledException)
		{
			Logger.LogDebug("UDP relay loop cancelled for {ClientEndpoint}", clientEndpoint);
		}
		catch (Exception exception)
		{
			Logger.LogError(exception, "Error in UDP relay loop for {ClientEndpoint}", clientEndpoint);
		}
	}

	private async Task HandleUdpClientToDestinationAsync(UdpClient proxyUdpClient, byte[] data, IPEndPoint clientUdpEndpoint, CancellationToken cancellationToken)
	{
		if (data.Length < 7 || data[0] != 0 || data[1] != 0 || data[2] != 0)
		{
			Logger.LogWarning("Invalid SOCKS5 UDP packet format from {ClientEndpoint}", clientUdpEndpoint);
			return;
		}
		try
		{
			int offset = 3;
			Socks5AddressType socks5AddressType = (Socks5AddressType)data[offset++];
			IPAddress address;
			switch (socks5AddressType)
			{
			case Socks5AddressType.IpV4:
				address = new IPAddress(new ReadOnlySpan<byte>(data, offset, 4));
				offset += 4;
				break;
			case Socks5AddressType.IpV6:
				address = new IPAddress(new ReadOnlySpan<byte>(data, offset, 16));
				offset += 16;
				break;
			case Socks5AddressType.DomainName:
			{
				byte b = data[offset++];
				string hostNameOrAddress = Encoding.UTF8.GetString(data, offset, b);
				offset += b;
				IPAddress[] array = await Dns.GetHostAddressesAsync(hostNameOrAddress, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				address = array.FirstOrDefault((IPAddress iPAddress) => iPAddress.AddressFamily == AddressFamily.InterNetwork) ?? array[0];
				break;
			}
			default:
				Logger.LogWarning("Unsupported address type {AddressType} from {ClientEndpoint}", socks5AddressType, clientUdpEndpoint);
				return;
			}
			int port = (data[offset] << 8) | data[offset + 1];
			offset += 2;
			await proxyUdpClient.SendAsync(data.AsMemory(offset), new IPEndPoint(address, port), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception exception)
		{
			Logger.LogWarning(exception, "Failed to relay UDP packet from client {ClientEndpoint}", clientUdpEndpoint);
		}
	}

	private async Task HandleUdpDestinationToClientAsync(UdpClient proxyUdpClient, byte[] data, IPEndPoint sourceEndpoint, IPEndPoint clientEndpoint, IPEndPoint clientUdpEndpoint, CancellationToken cancellationToken)
	{
		try
		{
			byte[] addressBytes = sourceEndpoint.Address.GetAddressBytes();
			int num = 4 + addressBytes.Length + 2;
			int totalLength = num + data.Length;
			using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(totalLength);
			Memory<byte> memory = owner.Memory.Slice(0, totalLength);
			Span<byte> span = memory.Span;
			span[0] = 0;
			span[1] = 0;
			span[2] = 0;
			span[3] = (byte)((sourceEndpoint.Address.AddressFamily != AddressFamily.InterNetworkV6) ? 1 : 4);
			int num2 = 4;
			int num3 = num2;
			addressBytes.CopyTo(span.Slice(num3, span.Length - num3));
			num2 += addressBytes.Length;
			span[num2++] = (byte)(sourceEndpoint.Port >> 8);
			span[num2] = (byte)(sourceEndpoint.Port & 0xFF);
			Span<byte> span2 = data.AsSpan();
			num3 = num;
			span2.CopyTo(span.Slice(num3, span.Length - num3));
			await proxyUdpClient.SendAsync(memory, clientEndpoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			Logger.LogDebug("Relayed UDP response from {Source} to {ClientEndpoint}, payload size: {Size}, response size: {ResponseSize}", sourceEndpoint, clientUdpEndpoint, data.Length, totalLength);
		}
		catch (Exception exception)
		{
			Logger.LogWarning(exception, "Failed to relay UDP response from {Source} to client {ClientEndpoint}", sourceEndpoint, clientUdpEndpoint);
		}
	}

	private static async Task PumpStreamsAsync(NetworkStream clientStream, NetworkStream remoteStream, CancellationToken cancellationToken)
	{
		Task[] tasks = new Task[2]
		{
			CopyStreamAsync(clientStream, remoteStream, 4096, cancellationToken),
			CopyStreamAsync(remoteStream, clientStream, 4096, cancellationToken)
		};
		try
		{
			await Task.WhenAny(tasks).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			await Task.WhenAll(((IEnumerable<Task>)tasks).Select((Func<Task, Task>)async delegate(Task task)
			{
				try
				{
					await task.ConfigureAwait(continueOnCapturedContext: false);
				}
				catch
				{
				}
			})).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private static async Task CopyStreamAsync(Stream sourceStream, Stream destinationStream, int bufferSize, CancellationToken cancellationToken)
	{
		byte[] buffer = new byte[bufferSize];
		try
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				int num = await sourceStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					break;
				}
				await destinationStream.WriteAsync(buffer.AsMemory(0, num), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch when (cancellationToken.IsCancellationRequested)
		{
		}
	}

	private static async Task<IPEndPoint> ReadDestinationAsync(NetworkStream stream, Socks5AddressType addressType, CancellationToken cancellationToken)
	{
		switch (addressType)
		{
		case Socks5AddressType.IpV4:
		{
			byte[] buffer = new byte[6];
			await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return new IPEndPoint(new IPAddress(buffer.AsSpan(0, 4)), (buffer[4] << 8) | buffer[5]);
		}
		case Socks5AddressType.IpV6:
		{
			byte[] buffer = new byte[18];
			await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return new IPEndPoint(new IPAddress(buffer.AsSpan(0, 16)), (buffer[16] << 8) | buffer[17]);
		}
		case Socks5AddressType.DomainName:
		{
			byte[] buffer = new byte[1];
			await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			byte length = buffer[0];
			byte[] rented = null;
			try
			{
				rented = ArrayPool<byte>.Shared.Rent(length + 2);
				Memory<byte> mem = rented.AsMemory(0, length + 2);
				await stream.ReadExactlyAsync(mem, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				Span<byte> span = mem.Span;
				int port = (span[length] << 8) | span[length + 1];
				IPAddress[] array = await Dns.GetHostAddressesAsync(Encoding.UTF8.GetString(span.Slice(0, length)), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				return new IPEndPoint(array.FirstOrDefault((IPAddress address) => address.AddressFamily == AddressFamily.InterNetwork) ?? array[0], port);
			}
			finally
			{
				if (rented != null)
				{
					ArrayPool<byte>.Shared.Return(rented);
				}
			}
		}
		default:
			throw new NotSupportedException($"Unsupported address type: {addressType}");
		}
	}

	private static async Task SendReplyAsync(NetworkStream stream, Socks5CommandReply reply, IPAddress bindAddress, int bindPort, CancellationToken cancellationToken)
	{
		byte[] addressBytes = bindAddress.GetAddressBytes();
		Socks5AddressType socks5AddressType = ((bindAddress.AddressFamily != AddressFamily.InterNetworkV6) ? Socks5AddressType.IpV4 : Socks5AddressType.IpV6);
		int num = 4 + addressBytes.Length + 2;
		byte[] response = null;
		try
		{
			response = ArrayPool<byte>.Shared.Rent(num);
			response[0] = 5;
			response[1] = (byte)reply;
			response[2] = 0;
			response[3] = (byte)socks5AddressType;
			addressBytes.CopyTo(response, 4);
			response[4 + addressBytes.Length] = (byte)(bindPort >> 8);
			response[4 + addressBytes.Length + 1] = (byte)(bindPort & 0xFF);
			await stream.WriteAsync(response.AsMemory(0, num), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (response != null)
			{
				ArrayPool<byte>.Shared.Return(response);
			}
		}
	}
}
