using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace VpnHood.Core.Tunneling.Connections;

public class StreamConnectionDecorator(IStreamConnection streamConnection, Stream? stream = null) : IStreamConnection, IDisposable, IAsyncDisposable
{
	protected bool Disposed;

	protected IStreamConnection InnerStreamConnection => streamConnection;

	public virtual bool Connected
	{
		get
		{
			if (!Disposed)
			{
				return InnerStreamConnection.Connected;
			}
			return false;
		}
	}

	public Stream Stream => stream ?? InnerStreamConnection.Stream;

	public IPEndPoint LocalEndPoint => InnerStreamConnection.LocalEndPoint;

	public IPEndPoint RemoteEndPoint => InnerStreamConnection.RemoteEndPoint;

	public bool IsServer => InnerStreamConnection.IsServer;

	public string ConnectionName => InnerStreamConnection.ConnectionName;

	public string ConnectionId
	{
		get
		{
			return InnerStreamConnection.ConnectionId;
		}
		set
		{
			InnerStreamConnection.ConnectionId = value;
		}
	}

	public bool RequireHttpResponse
	{
		get
		{
			return InnerStreamConnection.RequireHttpResponse;
		}
		set
		{
			InnerStreamConnection.RequireHttpResponse = value;
		}
	}

	public virtual void Dispose()
	{
		if (!Disposed)
		{
			Disposed = true;
			stream?.Dispose();
			InnerStreamConnection.Dispose();
		}
	}

	public virtual async ValueTask DisposeAsync()
	{
		if (!Disposed)
		{
			Disposed = true;
			if (stream != null)
			{
				await stream.DisposeAsync();
			}
			await InnerStreamConnection.DisposeAsync();
		}
	}
}
