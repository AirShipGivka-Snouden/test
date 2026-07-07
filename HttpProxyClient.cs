using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VpnHood.Core.Proxies.HttpProxyClients;

public class HttpProxyClient(HttpProxyClientOptions options, ILogger<HttpProxyClient>? logger = null) : IProxyClient
{
	public IPEndPoint ProxyEndPoint => options.ProxyEndPoint;

	public async Task ConnectAsync(TcpClient tcpClient, IPEndPoint destination, CancellationToken cancellationToken)
	{
		await ConnectAsync(tcpClient, destination.Address.ToString(), destination.Port, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task ConnectAsync(TcpClient tcpClient, string host, int port, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(tcpClient, "tcpClient");
		ArgumentException.ThrowIfNullOrWhiteSpace(host, "host");
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port, "port");
		logger?.LogDebug("Connecting to {Host}:{Port} through HTTP proxy {ProxyEndPoint}", host, port, options.ProxyEndPoint);
		try
		{
			if (!tcpClient.Connected)
			{
				tcpClient.NoDelay = true;
				await tcpClient.ConnectAsync(options.ProxyEndPoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			Stream stream = tcpClient.GetStream();
			if (options.UseTls)
			{
				logger?.LogDebug("Establishing TLS connection to proxy");
				SslStream ssl = new SslStream(stream, leaveInnerStreamOpen: true, UserCertificateValidationCallback);
				string targetHost = options.ProxyHost ?? options.ProxyEndPoint.Address.ToString();
				await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
				{
					TargetHost = targetHost,
					EnabledSslProtocols = SslProtocols.None,
					CertificateRevocationCheckMode = X509RevocationMode.NoCheck
				}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				stream = ssl;
				logger?.LogDebug("TLS connection established");
			}
			await SendConnectRequest(stream, host, port, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await ReadConnectResponse(stream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			logger?.LogDebug("HTTP CONNECT tunnel established to {Host}:{Port}", host, port);
		}
		catch (Exception exception)
		{
			logger?.LogError(exception, "Failed to connect to {Host}:{Port} through HTTP proxy", host, port);
			tcpClient.Close();
			throw;
		}
	}

	public async Task CheckConnectionAsync(TcpClient tcpClient, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(tcpClient, "tcpClient");
		try
		{
			tcpClient.NoDelay = true;
			await tcpClient.ConnectAsync(options.ProxyEndPoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (options.UseTls)
			{
				SslStream sslStream = new SslStream(tcpClient.GetStream(), leaveInnerStreamOpen: true, UserCertificateValidationCallback);
				string targetHost = options.ProxyHost ?? options.ProxyEndPoint.Address.ToString();
				await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
				{
					TargetHost = targetHost,
					EnabledSslProtocols = SslProtocols.None,
					CertificateRevocationCheckMode = X509RevocationMode.NoCheck
				}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
			tcpClient.Close();
			throw;
		}
	}

	private bool UserCertificateValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
	{
		if (options.AllowInvalidCertificates)
		{
			logger?.LogWarning("Accepting invalid certificate due to AllowInvalidCertificates option");
			return true;
		}
		if (sslPolicyErrors != SslPolicyErrors.None)
		{
			logger?.LogWarning("SSL certificate validation failed: {SslPolicyErrors}", sslPolicyErrors);
			return false;
		}
		return true;
	}

	private static string BuildAuthority(string host, int port)
	{
		if (host.Contains(':') && host.IndexOf(':') != host.LastIndexOf(':'))
		{
			return $"[{host}]:{port}";
		}
		return $"{host}:{port}";
	}

	private async Task SendConnectRequest(Stream stream, string host, int port, CancellationToken cancellationToken)
	{
		string value = BuildAuthority(host, port);
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
		handler.AppendLiteral("CONNECT ");
		handler.AppendFormatted(value);
		handler.AppendLiteral(" HTTP/1.1\r\n");
		stringBuilder3.Append(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder2);
		handler.AppendLiteral("Host: ");
		handler.AppendFormatted(value);
		handler.AppendLiteral("\r\n");
		stringBuilder4.Append(ref handler);
		stringBuilder.Append("Connection: keep-alive\r\n");
		if (options.Username != null)
		{
			string s = options.Username + ":" + (options.Password ?? string.Empty);
			string value2 = Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(29, 1, stringBuilder2);
			handler.AppendLiteral("Proxy-Authorization: Basic ");
			handler.AppendFormatted(value2);
			handler.AppendLiteral("\r\n");
			stringBuilder5.Append(ref handler);
			logger?.LogDebug("Added proxy authentication for user: {Username}", options.Username);
		}
		if (options.ExtraHeaders != null)
		{
			foreach (KeyValuePair<string, string> extraHeader in options.ExtraHeaders)
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(4, 2, stringBuilder2);
				handler.AppendFormatted(extraHeader.Key);
				handler.AppendLiteral(": ");
				handler.AppendFormatted(extraHeader.Value);
				handler.AppendLiteral("\r\n");
				stringBuilder6.Append(ref handler);
			}
		}
		stringBuilder.Append("Content-Length: 0\r\n");
		stringBuilder.Append("\r\n");
		byte[] bytes = Encoding.UTF8.GetBytes(stringBuilder.ToString());
		await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		await stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		logger?.LogDebug("Sent CONNECT request to proxy");
	}

	private async Task ReadConnectResponse(Stream stream, CancellationToken cancellationToken)
	{
		byte[] buffer = new byte[8192];
		int totalReceived = 0;
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(TimeSpan.FromSeconds(30L));
		try
		{
			while (totalReceived < buffer.Length)
			{
				int num = await stream.ReadAsync(buffer.AsMemory(totalReceived, buffer.Length - totalReceived), timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					throw new IOException("Proxy closed connection before sending complete response");
				}
				totalReceived += num;
				if (totalReceived < 4)
				{
					continue;
				}
				for (int i = 3; i < totalReceived; i++)
				{
					if (buffer[i - 3] == 13 && buffer[i - 2] == 10 && buffer[i - 1] == 13 && buffer[i] == 10)
					{
						ValidateConnectResponse(Encoding.UTF8.GetString(buffer, 0, i + 1));
						logger?.LogDebug("Received successful CONNECT response from proxy");
						return;
					}
				}
			}
			throw new ProxyClientException(SocketError.ProtocolNotSupported, "HTTP proxy response headers too large");
		}
		catch (OperationCanceledException) when (timeout.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
		{
			throw new TimeoutException("Timeout waiting for proxy response");
		}
	}

	private static void ValidateConnectResponse(string responseText)
	{
		if (string.IsNullOrWhiteSpace(responseText))
		{
			throw new ProxyClientException(SocketError.ProtocolNotSupported, "Empty or non-HTTP response");
		}
		if (responseText[0] == '\ufeff')
		{
			string text = responseText;
			responseText = text.Substring(1, text.Length - 1);
		}
		if (!responseText.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
		{
			throw new ProxyClientException(SocketError.ProtocolNotSupported, "Not an HTTP proxy");
		}
		string[] array = responseText.Split(new string[1] { "\r\n" }, StringSplitOptions.None)[0].Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length < 2 || !int.TryParse(array[1], out var result))
		{
			throw new ProxyClientException(SocketError.ProtocolNotSupported, "Malformed HTTP status line");
		}
		if (result == 200)
		{
			return;
		}
		string value = ((array.Length > 2) ? array[2] : "Unknown");
		ProxyClientException ex;
		switch (result)
		{
		case 407:
			ex = new ProxyClientException(SocketError.AccessDenied, $"Proxy authentication required or failed (status {result}: {value})");
			break;
		case 408:
			ex = new ProxyClientException(SocketError.TimedOut, $"Proxy connection timed out (status {result}: {value})");
			break;
		case 404:
			ex = new ProxyClientException(SocketError.HostNotFound, $"HTTP proxy or target not found (status {result}: {value})");
			break;
		case 400:
		case 401:
		case 403:
		case 405:
			ex = new ProxyClientException(SocketError.ProtocolNotSupported, $"HTTP server does not support CONNECT (status {result}: {value})");
			break;
		case 500:
		case 502:
		case 503:
		case 504:
			ex = new ProxyClientException(SocketError.HostUnreachable, $"Proxy failed to connect to target (status {result}: {value})");
			break;
		case 429:
			ex = new ProxyClientException(SocketError.TryAgain, $"Proxy rate limited request (status {result}: {value})");
			break;
		default:
			ex = new ProxyClientException(SocketError.SocketError, $"Unexpected HTTP proxy response (status {result}: {value})");
			break;
		}
		throw ex;
	}
}
