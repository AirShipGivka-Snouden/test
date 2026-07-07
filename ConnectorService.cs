using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Jobs;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling;
using VpnHood.Core.Tunneling.Channels.Streams;
using VpnHood.Core.Tunneling.Connections;
using VpnHood.Core.Tunneling.Utils;

namespace VpnHood.Core.Client.ConnectorServices;

internal class ConnectorService : IDisposable
{
	private class IdleConnectionItem
	{
		public required TimeSpan IdleConnectionTimeout { get; init; }

		public required ReusableStreamConnection StreamConnection { get; init; }

		private DateTime EnqueueTime { get; } = FastDateTime.Now;

		public bool IsExpired
		{
			get
			{
				if (!(EnqueueTime + IdleConnectionTimeout <= FastDateTime.Now))
				{
					return !StreamConnection.Connected;
				}
				return true;
			}
		}
	}

	private const bool UseBuffer = true;

	private readonly ConcurrentQueue<IdleConnectionItem> _idleConnectionItems = new ConcurrentQueue<IdleConnectionItem>();

	private readonly HashSet<ReusableStreamConnection> _sharedConnections = new HashSet<ReusableStreamConnection>(50);

	private readonly TcpStreamConnectionFactory _tcpConnectionFactory;

	private readonly QuicStreamConnectionFactory _quicConnectionFactory;

	private readonly Job _cleanupJob;

	private int _isDisposed;

	private bool _useWebSocket;

	[CompilerGenerated]
	private bool _003CAllowChannelReuse_003Ek__BackingField;

	public ConnectorStat Stat { get; }

	public int ProtocolVersion { get; private set; } = 8;

	public VpnEndPoint VpnEndPoint { get; init; }

	public TimeSpan RequestTimeout { get; set; }

	public bool UseQuic { get; set; }

	public TimeSpan IdleConnectionTimeout
	{
		get
		{
			return _quicConnectionFactory.IdleConnectionTimeout;
		}
		set
		{
			_quicConnectionFactory.IdleConnectionTimeout = value;
		}
	}

	public IPEndPoint? QuicEndPoint
	{
		get
		{
			return _quicConnectionFactory.QuicEndPoint;
		}
		set
		{
			_quicConnectionFactory.QuicEndPoint = value;
		}
	}

	public bool AllowChannelReuse
	{
		[CompilerGenerated]
		get
		{
			return _003CAllowChannelReuse_003Ek__BackingField;
		}
		set
		{
			if (!value)
			{
				PreventReuseChannel();
			}
			_003CAllowChannelReuse_003Ek__BackingField = value;
		}
	}

	public ConnectorService(ConnectorServiceOptions options)
	{
		AllowChannelReuse = options.AllowChannelReuse;
		Stat = new ConnectorStat(() => _idleConnectionItems.Count);
		VpnEndPoint = options.VpnEndPoint;
		RequestTimeout = options.RequestTimeout;
		_cleanupJob = new Job(Cleanup, "ConnectorCleanup");
		_tcpConnectionFactory = new TcpStreamConnectionFactory(options.SocketFactory, options.ProxyEndPointManager, options.VpnEndPoint, UserCertificateValidationCallback);
		_quicConnectionFactory = new QuicStreamConnectionFactory(options.VpnEndPoint, UserCertificateValidationCallback);
		IdleConnectionTimeout = TimeSpan.FromSeconds(30L).WhenNoDebugger();
	}

	public void Init(int protocolVersion, byte[]? serverSecret, TimeSpan channelIdleTimeout, bool useWebSocket, TimeSpan requestTimeout, bool useQuic, IPEndPoint? quicEndPoint)
	{
		ProtocolVersion = protocolVersion;
		IdleConnectionTimeout = channelIdleTimeout;
		_useWebSocket = useWebSocket;
		RequestTimeout = requestTimeout;
		UseQuic = useQuic;
		QuicEndPoint = quicEndPoint;
	}

	private static string BuildRequestPath(string? pathBase)
	{
		StringBuilder stringBuilder = new StringBuilder("/");
		if (!string.IsNullOrEmpty(pathBase))
		{
			stringBuilder.Append(pathBase.Trim('/'));
			stringBuilder.Append('/');
		}
		stringBuilder.Append(Guid.NewGuid());
		return stringBuilder.ToString();
	}

	private static string BuildPostRequest(string hostName, string? pathBase, TunnelStreamType streamType, int protocolVersion, int contentLength, string connectionId)
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder);
		handler.AppendLiteral("POST ");
		handler.AppendFormatted(BuildRequestPath(pathBase));
		handler.AppendLiteral(" HTTP/1.1\r\n");
		StringBuilder stringBuilder2 = stringBuilder.Append(ref handler);
		StringBuilder.AppendInterpolatedStringHandler handler2 = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder2);
		handler2.AppendLiteral("Host: ");
		handler2.AppendFormatted(hostName);
		handler2.AppendLiteral("\r\n");
		StringBuilder stringBuilder3 = stringBuilder2.Append(ref handler2);
		StringBuilder.AppendInterpolatedStringHandler handler3 = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder3);
		handler3.AppendLiteral("Content-Length: ");
		handler3.AppendFormatted(contentLength);
		handler3.AppendLiteral("\r\n");
		StringBuilder stringBuilder4 = stringBuilder3.Append(ref handler3).Append("Content-Type: application/octet-stream\r\n").Append("User-Agent: Hood\r\n");
		StringBuilder.AppendInterpolatedStringHandler handler4 = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder4);
		handler4.AppendLiteral("X-Buffered: ");
		handler4.AppendFormatted(value: true);
		handler4.AppendLiteral("\r\n");
		StringBuilder stringBuilder5 = stringBuilder4.Append(ref handler4);
		StringBuilder.AppendInterpolatedStringHandler handler5 = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder5);
		handler5.AppendLiteral("X-ProtocolVersion: ");
		handler5.AppendFormatted(protocolVersion);
		handler5.AppendLiteral("\r\n");
		StringBuilder stringBuilder6 = stringBuilder5.Append(ref handler5);
		StringBuilder.AppendInterpolatedStringHandler handler6 = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder6);
		handler6.AppendLiteral("X-ConnectionId: ");
		handler6.AppendFormatted(connectionId);
		handler6.AppendLiteral("\r\n");
		StringBuilder stringBuilder7 = stringBuilder6.Append(ref handler6);
		StringBuilder.AppendInterpolatedStringHandler handler7 = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder7);
		handler7.AppendLiteral("X-BinaryStream: ");
		handler7.AppendFormatted(streamType);
		handler7.AppendLiteral("\r\n");
		return stringBuilder7.Append(ref handler7).Append("\r\n").ToString();
	}

	private async Task<IStreamConnection> CreateWebSocketConnection(IStreamConnection streamConnection, CancellationToken cancellationToken)
	{
		string value = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder);
		handler.AppendLiteral("GET /");
		handler.AppendFormatted(BuildRequestPath(VpnEndPoint.PathBase));
		handler.AppendLiteral(" HTTP/1.1\r\n");
		StringBuilder stringBuilder2 = stringBuilder.Append(ref handler);
		StringBuilder.AppendInterpolatedStringHandler handler2 = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder2);
		handler2.AppendLiteral("Host: ");
		handler2.AppendFormatted(VpnEndPoint.HostName);
		handler2.AppendLiteral("\r\n");
		StringBuilder stringBuilder3 = stringBuilder2.Append(ref handler2);
		StringBuilder.AppendInterpolatedStringHandler handler3 = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder3);
		handler3.AppendLiteral("X-Buffered: ");
		handler3.AppendFormatted(value: true);
		handler3.AppendLiteral("\r\n");
		StringBuilder stringBuilder4 = stringBuilder3.Append(ref handler3);
		StringBuilder.AppendInterpolatedStringHandler handler4 = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder4);
		handler4.AppendLiteral("X-ProtocolVersion: ");
		handler4.AppendFormatted(ProtocolVersion);
		handler4.AppendLiteral("\r\n");
		StringBuilder stringBuilder5 = stringBuilder4.Append(ref handler4);
		StringBuilder.AppendInterpolatedStringHandler handler5 = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder5);
		handler5.AppendLiteral("X-ConnectionId: ");
		handler5.AppendFormatted(streamConnection.ConnectionId);
		handler5.AppendLiteral("\r\n");
		StringBuilder stringBuilder6 = stringBuilder5.Append(ref handler5).Append("Upgrade: websocket\r\n").Append("Connection: Upgrade\r\n")
			.Append("Sec-WebSocket-Version: 13\r\n");
		StringBuilder.AppendInterpolatedStringHandler handler6 = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder6);
		handler6.AppendLiteral("Sec-WebSocket-Key: ");
		handler6.AppendFormatted(value);
		handler6.AppendLiteral("\r\n");
		StringBuilder stringBuilder7 = stringBuilder6.Append(ref handler6).Append("\r\n");
		await streamConnection.Stream.WriteAsync(Encoding.UTF8.GetBytes(stringBuilder7.ToString()), cancellationToken).Vhc();
		if ((await HttpUtils.ReadResponse(streamConnection.Stream, cancellationToken).Vhc()).StatusCode != HttpStatusCode.SwitchingProtocols)
		{
			throw new Exception("Unexpected response.");
		}
		WebSocketStream stream = new WebSocketStream(streamConnection.Stream, streamConnection.ConnectionId, useBuffer: true, isServer: false);
		StreamConnectionDecorator streamConnectionDecorator = new StreamConnectionDecorator(streamConnection, stream)
		{
			RequireHttpResponse = false
		};
		if (!AllowChannelReuse)
		{
			return streamConnectionDecorator;
		}
		ReusableStreamConnection reusableStreamConnection = new ReusableStreamConnection(streamConnectionDecorator, ConnectionReuseCallback);
		lock (_sharedConnections)
		{
			_sharedConnections.Add(reusableStreamConnection);
		}
		return reusableStreamConnection;
	}

	private async Task<IStreamConnection> CreateSimpleConnection(IStreamConnection streamConnection, int contentLength, CancellationToken cancellationToken)
	{
		string s = BuildPostRequest(VpnEndPoint.HostName, VpnEndPoint.PathBase, TunnelStreamType.None, ProtocolVersion, contentLength, streamConnection.ConnectionId);
		await streamConnection.Stream.WriteAsync(Encoding.UTF8.GetBytes(s), cancellationToken).Vhc();
		streamConnection.RequireHttpResponse = true;
		return streamConnection;
	}

	private Task<IStreamConnection> CreateHttpConnection(IStreamConnection streamConnection, int contentLength, CancellationToken cancellationToken)
	{
		if (!_useWebSocket)
		{
			return CreateSimpleConnection(streamConnection, contentLength, cancellationToken);
		}
		return CreateWebSocketConnection(streamConnection, cancellationToken);
	}

	public async Task<IStreamConnection> GetConnectionToServer(string streamId, int contentLength, Action? onConnectAttempt, CancellationToken cancellationToken)
	{
		IStreamConnection streamConnection = ((!UseQuic) ? (await _tcpConnectionFactory.CreateConnection(streamId, onConnectAttempt, cancellationToken).Vhc()) : (await _quicConnectionFactory.CreateConnection(streamId, cancellationToken).Vhc()));
		IStreamConnection streamConnection2 = streamConnection;
		IStreamConnection result = await CreateHttpConnection(streamConnection2, contentLength, cancellationToken).Vhc();
		lock (Stat)
		{
			Stat.CreatedConnectionCount++;
			return result;
		}
	}

	public ReusableStreamConnection? GetFreeConnection()
	{
		IdleConnectionItem result;
		while (_idleConnectionItems.TryDequeue(out result))
		{
			if (!result.IsExpired)
			{
				return result.StreamConnection;
			}
			result.StreamConnection.PreventReuse();
			result.StreamConnection.Dispose();
		}
		return null;
	}

	private void ConnectionReuseCallback(ReusableStreamConnection streamConnection)
	{
		if (_isDisposed != 0 || !AllowChannelReuse)
		{
			VhLogger.Instance.LogDebug(GeneralEventId.Stream, "Disposing the reused client stream because the connector service is either disposed or reuse is no longer allowed. ConnectionId: {ConnectionId}", streamConnection.ConnectionId);
			streamConnection.PreventReuse();
			streamConnection.Dispose();
		}
		else
		{
			_idleConnectionItems.Enqueue(new IdleConnectionItem
			{
				IdleConnectionTimeout = IdleConnectionTimeout,
				StreamConnection = streamConnection
			});
		}
	}

	private ValueTask Cleanup(CancellationToken cancellationToken)
	{
		IdleConnectionItem result;
		while (_idleConnectionItems.TryPeek(out result) && result.IsExpired)
		{
			if (_idleConnectionItems.TryDequeue(out result))
			{
				result.StreamConnection.PreventReuse();
				result.StreamConnection.Dispose();
			}
		}
		lock (_sharedConnections)
		{
			_sharedConnections.RemoveWhere((ReusableStreamConnection x) => !x.Connected);
		}
		return ValueTask.CompletedTask;
	}

	private void PreventReuseChannel()
	{
		lock (_sharedConnections)
		{
			foreach (ReusableStreamConnection sharedConnection in _sharedConnections)
			{
				sharedConnection.PreventReuse();
			}
			IdleConnectionItem result;
			while (_idleConnectionItems.TryDequeue(out result))
			{
				result.StreamConnection.PreventReuse();
				result.StreamConnection.Dispose();
			}
			_idleConnectionItems.Clear();
		}
	}

	private bool UserCertificateValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
	{
		if (certificate == null)
		{
			return false;
		}
		if (sslPolicyErrors == SslPolicyErrors.None)
		{
			return true;
		}
		byte[]? certificateHash = VpnEndPoint.CertificateHash;
		if (certificateHash == null)
		{
			return false;
		}
		return certificateHash.SequenceEqual(certificate.GetCertHash());
	}

	private void Dispose(bool disposing)
	{
		if (Interlocked.Exchange(ref _isDisposed, 1) == 1 || !disposing)
		{
			return;
		}
		_cleanupJob.Dispose();
		_tcpConnectionFactory.Dispose();
		_quicConnectionFactory.DisposeAsync().VhBlock();
		PreventReuseChannel();
		IdleConnectionItem result;
		while (_idleConnectionItems.TryDequeue(out result))
		{
			result.StreamConnection.PreventReuse();
			result.StreamConnection.Dispose();
		}
		_idleConnectionItems.Clear();
		lock (_sharedConnections)
		{
			_sharedConnections.Clear();
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
