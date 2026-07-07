using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.Channels.Streams;
using VpnHood.Core.Tunneling.Utils;

namespace VpnHood.Core.Tunneling.Connections;

public sealed class TcpStreamConnection : IStreamConnection, IDisposable, IAsyncDisposable
{
	private readonly TcpClient _tcpClient;

	private bool _disposed;

	[CompilerGenerated]
	private string _003CConnectionId_003Ek__BackingField;

	public string ConnectionName { get; }

	public bool IsServer { get; }

	public bool Connected => VhUtils.IsTcpClientHealthy(_tcpClient);

	public Stream Stream { get; }

	public IPEndPoint LocalEndPoint => _tcpClient.GetLocalEndPoint();

	public IPEndPoint RemoteEndPoint => _tcpClient.GetRemoteEndPoint();

	public bool RequireHttpResponse { get; set; }

	public string ConnectionId
	{
		[CompilerGenerated]
		get
		{
			return _003CConnectionId_003Ek__BackingField;
		}
		set
		{
			if (_003CConnectionId_003Ek__BackingField != value)
			{
				VhLogger.Instance.LogDebug(GeneralEventId.Stream, "ConnectionId has been changed. ConnectionId: {ConnectionId}, NewConnectionId: {NewConnectionId}", _003CConnectionId_003Ek__BackingField, value);
			}
			_003CConnectionId_003Ek__BackingField = value;
			if (Stream is ChunkStream chunkStream)
			{
				chunkStream.StreamId = value;
			}
		}
	}

	public TcpStreamConnection(TcpClient tcpClient, string connectionName, bool isServer, string? connectionId = null)
	{
		_tcpClient = tcpClient;
		ConnectionName = connectionName;
		IsServer = isServer;
		Stream = tcpClient.GetStream();
		ConnectionId = connectionId ?? UniqueIdFactory.Create();
	}

	public TcpStreamConnection(TcpClient tcpClient, Stream stream, string connectionName, bool isServer, string? connectionId = null)
		: this(tcpClient, connectionName, isServer, connectionId)
	{
		Stream = stream;
	}

	public override string ToString()
	{
		string value = (IsServer ? "server" : "client");
		return $"{ConnectionId}:{ConnectionName}:{value}";
	}

	public void Dispose()
	{
		if (!Interlocked.Exchange(ref _disposed, value: true))
		{
			Stream.Dispose();
			_tcpClient.Dispose();
			VhLogger.Instance.LogTrace(GeneralEventId.Stream, "Connection has been disposed. ConnectionId: {ConnectionId}", ConnectionId);
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (!Interlocked.Exchange(ref _disposed, value: true))
		{
			await Stream.DisposeAsync();
			_tcpClient.Dispose();
			VhLogger.Instance.LogTrace(GeneralEventId.Stream, "Connection has been disposed asynchronously. ConnectionId: {ConnectionId}", ConnectionId);
		}
	}
}
