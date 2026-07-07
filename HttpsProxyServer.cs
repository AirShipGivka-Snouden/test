using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VpnHood.Core.Proxies.HttpProxyServers;

public sealed class HttpsProxyServer : IDisposable
{
	private readonly HttpProxyServerOptions _options;

	private readonly ILogger<HttpsProxyServer>? _logger;

	private readonly TcpListener _listener;

	private readonly CancellationTokenSource _serverCts = new CancellationTokenSource();

	private volatile bool _isRunning;

	public HttpsProxyServer(HttpProxyServerOptions options, ILogger<HttpsProxyServer>? logger = null)
	{
		_options = options ?? throw new ArgumentNullException("options");
		_logger = logger;
		if (_options.ServerCertificate == null)
		{
			throw new ArgumentException("ServerCertificate is required for HTTPS proxy server", "options");
		}
		_listener = new TcpListener(_options.ListenEndPoint);
	}

	public void Start()
	{
		if (!_isRunning)
		{
			_listener.Start(_options.Backlog);
			_isRunning = true;
			_logger?.LogInformation("HTTPS proxy server started on {EndPoint}", _options.ListenEndPoint);
		}
	}

	public void Stop()
	{
		if (_isRunning)
		{
			_isRunning = false;
			_serverCts.Cancel();
			_listener.Stop();
			_logger?.LogInformation("HTTPS proxy server stopped");
		}
	}

	public async Task RunAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _serverCts.Token);
		CancellationToken operationCancellationToken = linkedCts.Token;
		Start();
		try
		{
			while (!operationCancellationToken.IsCancellationRequested)
			{
				try
				{
					TcpClient client = await _listener.AcceptTcpClientAsync(operationCancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					Task.Run(() => HandleClientAsync(client, operationCancellationToken), operationCancellationToken);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception exception)
				{
					_logger?.LogError(exception, "Error accepting client connection");
				}
			}
		}
		finally
		{
			Stop();
		}
	}

	private async Task HandleClientAsync(TcpClient client, CancellationToken serverCancellationToken)
	{
		string clientEndpointAddress = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
		_logger?.LogDebug("Handling HTTPS client connection from {ClientEndpoint}", clientEndpointAddress);
		using TcpClient tcpClient = client;
		_ = 6;
		try
		{
			tcpClient.NoDelay = true;
			NetworkStream stream = tcpClient.GetStream();
			using SslStream sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
			_logger?.LogDebug("Establishing TLS connection with {ClientEndpoint}", clientEndpointAddress);
			using CancellationTokenSource handshakeCts = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken);
			handshakeCts.CancelAfter(_options.HandshakeTimeout);
			CancellationToken handshakeCancellationToken = handshakeCts.Token;
			await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
			{
				ServerCertificate = _options.ServerCertificate,
				ClientCertificateRequired = false,
				EnabledSslProtocols = (SslProtocols.Tls12 | SslProtocols.Tls13),
				CertificateRevocationCheckMode = X509RevocationMode.NoCheck
			}, handshakeCancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			_logger?.LogDebug("TLS connection established with {ClientEndpoint}", clientEndpointAddress);
			StreamReader reader = new StreamReader(sslStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: true, -1, leaveOpen: true);
			StreamWriter writer = new StreamWriter(sslStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
			{
				NewLine = "\r\n",
				AutoFlush = true
			};
			string text = await reader.ReadLineAsync(handshakeCancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (string.IsNullOrWhiteSpace(text))
			{
				_logger?.LogWarning("Empty request line from {ClientEndpoint}", clientEndpointAddress);
				return;
			}
			string[] parts = text.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2)
			{
				_logger?.LogWarning("Invalid request line from {ClientEndpoint}: {RequestLine}", clientEndpointAddress, text);
				await WriteErrorResponse(writer, "400 Bad Request", handshakeCancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			string method = parts[0].ToUpperInvariant();
			_logger?.LogDebug("Processing {Method} request from {ClientEndpoint}", method, clientEndpointAddress);
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
			if (_options.Username != null)
			{
				if (!ValidateBasicAuth(headers.GetValueOrDefault("Proxy-Authorization"), _options.Username, _options.Password ?? string.Empty))
				{
					_logger?.LogWarning("Authentication failed for {ClientEndpoint}", clientEndpointAddress);
					await WriteProxyAuthRequiredAsync(writer, handshakeCancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					return;
				}
				_logger?.LogDebug("Authentication successful for {ClientEndpoint}", clientEndpointAddress);
			}
			if (method == "CONNECT")
			{
				await HandleConnectRequestAsync(parts[1], sslStream, writer, serverCancellationToken, clientEndpointAddress).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			_logger?.LogWarning("Unsupported method {Method} from {ClientEndpoint}", method, clientEndpointAddress);
			await WriteErrorResponse(writer, "405 Method Not Allowed", handshakeCancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
			_logger?.LogDebug("Client connection cancelled for {ClientEndpoint}", clientEndpointAddress);
		}
		catch (Exception exception)
		{
			_logger?.LogError(exception, "Error handling HTTPS client {ClientEndpoint}", clientEndpointAddress);
		}
	}

	private async Task HandleConnectRequestAsync(string authority, SslStream clientStream, StreamWriter writer, CancellationToken cancellationToken, string clientEndpointAddress)
	{
		try
		{
			string[] array = authority.Split(':');
			string hostname = array[0];
			int result;
			int port = ((array.Length > 1 && int.TryParse(array[1], out result)) ? result : 443);
			_logger?.LogDebug("Connecting to {Host}:{Port} for {ClientEndpoint}", hostname, port, clientEndpointAddress);
			using TcpClient remoteClient = new TcpClient();
			remoteClient.NoDelay = true;
			using (CancellationTokenSource connectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				connectionCts.CancelAfter(_options.HostConnectionTimeout);
				await remoteClient.ConnectAsync(hostname, port, connectionCts.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
			await writer.WriteLineAsync("HTTP/1.1 200 Connection Established").ConfigureAwait(continueOnCapturedContext: false);
			await writer.WriteLineAsync().ConfigureAwait(continueOnCapturedContext: false);
			await writer.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			_logger?.LogDebug("Tunneling established between {ClientEndpoint} and {Host}:{Port}", clientEndpointAddress, hostname, port);
			await PumpStreamsAsync(clientStream, remoteClient.GetStream(), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception exception)
		{
			_logger?.LogError(exception, "Failed to establish CONNECT tunnel for {ClientEndpoint} to {Authority}", clientEndpointAddress, authority);
			await WriteErrorResponse(writer, "502 Bad Gateway", cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
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

	public void Dispose()
	{
		Stop();
		_serverCts.Dispose();
	}
}
