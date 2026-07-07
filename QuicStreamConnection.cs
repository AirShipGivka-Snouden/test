using System;
using System.IO;
using System.Net;
using System.Net.Quic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Tunneling.Channels.Streams;
using VpnHood.Core.Tunneling.Utils;

namespace VpnHood.Core.Tunneling.Connections;

public sealed class QuicStreamConnection : IStreamConnection, IDisposable, IAsyncDisposable
{
	private readonly QuicStream _stream;

	private bool _disposed;

	[CompilerGenerated]
	private string _003CConnectionId_003Ek__BackingField;

	public string ConnectionName { get; }

	public bool IsServer { get; }

	public bool Connected { get; private set; } = true;

	public Stream Stream => _stream;

	public IPEndPoint LocalEndPoint { get; }

	public IPEndPoint RemoteEndPoint { get; }

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
				VhLogger.Instance.LogDebug(GeneralEventId.Stream, "QuicConnectionId has been changed. ConnectionId: {ConnectionId}, NewConnectionId: {NewConnectionId}", _003CConnectionId_003Ek__BackingField, value);
			}
			_003CConnectionId_003Ek__BackingField = value;
			if (Stream is ChunkStream chunkStream)
			{
				chunkStream.StreamId = value;
			}
		}
	}

	public event EventHandler? Disposed;

	public QuicStreamConnection(QuicStream stream, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string connectionName, bool isServer, string? connectionId = null)
	{
		_stream = stream;
		LocalEndPoint = localEndPoint;
		RemoteEndPoint = remoteEndPoint;
		ConnectionName = connectionName;
		IsServer = isServer;
		ConnectionId = connectionId ?? UniqueIdFactory.Create();
	}

	public override string ToString()
	{
		string value = (IsServer ? "server" : "client");
		return $"{ConnectionId}:{ConnectionName}:{value}:quic";
	}

	public void Dispose()
	{
		if (!Interlocked.Exchange(ref _disposed, value: true))
		{
			Connected = false;
			_stream.Dispose();
			this.Disposed?.Invoke(this, EventArgs.Empty);
			VhLogger.Instance.LogTrace(GeneralEventId.Stream, "QuicStreamConnection has been disposed. ConnectionId: {ConnectionId}", ConnectionId);
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (!Interlocked.Exchange(ref _disposed, value: true))
		{
			Connected = false;
			await Stream.DisposeAsync();
			this.Disposed?.Invoke(this, EventArgs.Empty);
			VhLogger.Instance.LogTrace(GeneralEventId.Stream, "Connection has been disposed asynchronously. ConnectionId: {ConnectionId}", ConnectionId);
		}
	}
}
