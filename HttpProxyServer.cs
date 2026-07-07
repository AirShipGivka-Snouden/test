using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VpnHood.Core.Proxies.HttpProxyServers;

public sealed class HttpProxyServer(HttpProxyServerOptions options, ILogger<HttpProxyServer>? logger = null) : TcpProxyServerBase(options.ListenEndPoint, options.Backlog, logger)
{
	protected override async Task HandleClientAsync(TcpClient client, CancellationToken serverCancellationToken)
	{
		string clientEndpointAddress = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
		Logger.LogDebug("Handling client connection from {ClientEndpoint}", clientEndpointAddress);
		using TcpClient tcpClient = client;
		_ = 2;
		try
		{
			tcpClient.NoDelay = true;
			NetworkStream networkStream = tcpClient.GetStream();
			HttpHandshakeResult httpHandshakeResult = await PerformHandshakeAsync(networkStream, serverCancellationToken, clientEndpointAddress).ConfigureAwait(continueOnCapturedContext: false);
			if (httpHandshakeResult.IsValid)
			{
				if (httpHandshakeResult.Method == "CONNECT")
				{
					await HandleConnectRequestAsync(httpHandshakeResult.Target, networkStream, httpHandshakeResult.Writer, serverCancellationToken, clientEndpointAddress).ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					await HandleHttpRequestAsync(httpHandshakeResult.Method, httpHandshakeResult.Target, httpHandshakeResult.Headers, networkStream, serverCancellationToken, clientEndpointAddress).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		catch (OperationCanceledException)
		{
			Logger.LogDebug("Client connection cancelled for {ClientEndpoint}", clientEndpointAddress);
		}
		catch (Exception exception)
		{
			Logger.LogError(exception, "Error handling client {ClientEndpoint}", clientEndpointAddress);
		}
	}

	private async Task<HttpHandshakeResult> PerformHandshakeAsync(Stream networkStream, CancellationToken serverCancellationToken, string clientEndpointAddress)
	{
		StreamReader reader = new StreamReader(networkStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: true, -1, leaveOpen: true);
		StreamWriter writer = new StreamWriter(networkStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
		{
			NewLine = "\r\n",
			AutoFlush = true
		};
		using CancellationTokenSource handshakeCts = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken);
		handshakeCts.CancelAfter(options.HandshakeTimeout);
		CancellationToken handshakeCancellationToken = handshakeCts.Token;
		try
		{
			string text = await reader.ReadLineAsync(handshakeCancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (string.IsNullOrWhiteSpace(text))
			{
				Logger.LogWarning("Empty request line from {ClientEndpoint}", clientEndpointAddress);
				return HttpHandshakeResult.Invalid;
			}
			string[] array = text.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length < 2)
			{
				Logger.LogWarning("Invalid request line from {ClientEndpoint}: {RequestLine}", clientEndpointAddress, text);
				await WriteErrorResponse(writer, "400 Bad Request", handshakeCancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				return HttpHandshakeResult.Invalid;
			}
			string method = array[0].ToUpperInvariant();
			string target = array[1];
			Logger.LogDebug("Processing {Method} request from {ClientEndpoint}", method, clientEndpointAddress);
			Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			string text2;
			while (!string.IsNullOrEmpty(text2 = await reader.ReadLineAsync(handshakeCancellationToken).ConfigureAwait(continueOnCapturedContext: false)))
			{
				int num = text2.IndexOf(':');
				if (num > 0)
				{
					string key = text2.Substring(0, num).Trim();
					string text3 = text2;
					int num2 = num + 1;
					string value = text3.Substring(num2, text3.Length - num2).Trim();
					headers[key] = value;
				}
			}
			if (options.Username != null)
			{
				if (!ValidateBasicAuth(headers.GetValueOrDefault("Proxy-Authorization"), options.Username, options.Password ?? string.Empty))
				{
					Logger.LogWarning("Authentication failed for {ClientEndpoint}", clientEndpointAddress);
					await WriteProxyAuthRequiredAsync(writer, handshakeCancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					return HttpHandshakeResult.Invalid;
				}
				Logger.LogDebug("Authentication successful for {ClientEndpoint}", clientEndpointAddress);
			}
			return HttpHandshakeResult.Valid(method, target, headers, reader, writer);
		}
		catch (OperationCanceledException)
		{
			Logger.LogDebug("Handshake cancelled for {ClientEndpoint}", clientEndpointAddress);
			return HttpHandshakeResult.Invalid;
		}
		catch (Exception exception)
		{
			Logger.LogError(exception, "Error during handshake for {ClientEndpoint}", clientEndpointAddress);
			return HttpHandshakeResult.Invalid;
		}
	}

	private async Task HandleConnectRequestAsync(string authority, Stream clientStream, StreamWriter writer, CancellationToken cancellationToken, string clientEndpointAddress)
	{
		try
		{
			string[] array = authority.Split(':');
			string hostname = array[0];
			int result;
			int port = ((array.Length > 1 && int.TryParse(array[1], out result)) ? result : 443);
			Logger.LogDebug("Connecting to {Host}:{Port} for {ClientEndpoint}", hostname, port, clientEndpointAddress);
			using TcpClient remoteClient = new TcpClient();
			remoteClient.NoDelay = true;
			using (CancellationTokenSource connectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				connectionCts.CancelAfter(options.HostConnectionTimeout);
				await remoteClient.ConnectAsync(hostname, port, connectionCts.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
			await writer.WriteLineAsync("HTTP/1.1 200 Connection Established").ConfigureAwait(continueOnCapturedContext: false);
			await writer.WriteLineAsync().ConfigureAwait(continueOnCapturedContext: false);
			await writer.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			Logger.LogDebug("Tunneling established between {ClientEndpoint} and {Host}:{Port}", clientEndpointAddress, hostname, port);
			await PumpStreamsAsync(clientStream, remoteClient.GetStream(), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception exception)
		{
			Logger.LogError(exception, "Failed to establish CONNECT tunnel for {ClientEndpoint} to {Authority}", clientEndpointAddress, authority);
			await WriteErrorResponse(writer, "502 Bad Gateway", cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async Task HandleHttpRequestAsync(string method, string uri, Dictionary<string, string> headers, Stream clientStream, CancellationToken cancellationToken, string clientEndpointAddress)
	{
		_ = 4;
		try
		{
			if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri targetUri))
			{
				Logger.LogWarning("Invalid URI {Uri} from {ClientEndpoint}", uri, clientEndpointAddress);
				await WriteErrorResponse(new StreamWriter(clientStream, Encoding.UTF8)
				{
					AutoFlush = true
				}, "400 Bad Request", cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			string hostname = targetUri.Host;
			int port = ((!targetUri.IsDefaultPort) ? targetUri.Port : (targetUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? 443 : 80));
			using TcpClient remoteClient = new TcpClient();
			remoteClient.NoDelay = true;
			using (CancellationTokenSource connectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				connectionCts.CancelAfter(options.HostConnectionTimeout);
				await remoteClient.ConnectAsync(hostname, port, connectionCts.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
			NetworkStream remoteStream = remoteClient.GetStream();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(method).Append(' ').Append(targetUri.PathAndQuery)
				.Append(" HTTP/1.1\r\n");
			if (!headers.ContainsKey("Host"))
			{
				stringBuilder.Append("Host: ").Append(hostname).Append(':')
					.Append(port)
					.Append("\r\n");
			}
			stringBuilder.Append("Connection: close\r\n\r\n");
			byte[] bytes = Encoding.UTF8.GetBytes(stringBuilder.ToString());
			await remoteStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await remoteStream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await PumpStreamsAsync(remoteStream, clientStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception exception)
		{
			Logger.LogError(exception, "Failed to handle HTTP request for {ClientEndpoint}", clientEndpointAddress);
		}
	}

	private static bool ValidateBasicAuth(string? proxyAuthHeader, string expectedUsername, string expectedPassword)
	{
		if (string.IsNullOrEmpty(proxyAuthHeader))
		{
			return false;
		}
		if (!proxyAuthHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string text = proxyAuthHeader;
		int length = "Basic ".Length;
		string s = text.Substring(length, text.Length - length).Trim();
		try
		{
			string text2 = Encoding.UTF8.GetString(Convert.FromBase64String(s));
			int num = text2.IndexOf(':');
			if (num < 0)
			{
				return false;
			}
			string a = text2.Substring(0, num);
			text = text2;
			length = num + 1;
			string a2 = text.Substring(length, text.Length - length);
			return string.Equals(a, expectedUsername, StringComparison.Ordinal) && string.Equals(a2, expectedPassword, StringComparison.Ordinal);
		}
		catch
		{
			return false;
		}
	}

	private static async Task WriteProxyAuthRequiredAsync(StreamWriter writer, CancellationToken cancellationToken)
	{
		await writer.WriteLineAsync("HTTP/1.1 407 Proxy Authentication Required").ConfigureAwait(continueOnCapturedContext: false);
		await writer.WriteLineAsync("Proxy-Authenticate: Basic realm=\"Proxy\"").ConfigureAwait(continueOnCapturedContext: false);
		await writer.WriteLineAsync("Connection: close").ConfigureAwait(continueOnCapturedContext: false);
		await writer.WriteLineAsync("Content-Length: 0").ConfigureAwait(continueOnCapturedContext: false);
		await writer.WriteLineAsync().ConfigureAwait(continueOnCapturedContext: false);
		await writer.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task WriteErrorResponse(StreamWriter writer, string status, CancellationToken cancellationToken)
	{
		await writer.WriteLineAsync("HTTP/1.1 " + status).ConfigureAwait(continueOnCapturedContext: false);
		await writer.WriteLineAsync("Connection: close").ConfigureAwait(continueOnCapturedContext: false);
		await writer.WriteLineAsync("Content-Length: 0").ConfigureAwait(continueOnCapturedContext: false);
		await writer.WriteLineAsync().ConfigureAwait(continueOnCapturedContext: false);
		await writer.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task PumpStreamsAsync(Stream sourceStream, Stream destinationStream, CancellationToken cancellationToken)
	{
		Task[] tasks = new Task[2]
		{
			CopyStreamAsync(sourceStream, destinationStream, 4096, cancellationToken),
			CopyStreamAsync(destinationStream, sourceStream, 4096, cancellationToken)
		};
		try
		{
			await Task.WhenAny(tasks).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
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
				await destinationStream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch (Exception) when (cancellationToken.IsCancellationRequested)
		{
		}
	}
}
