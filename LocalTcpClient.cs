using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using VpnHood.Core.TcpStack.Abstractions;

namespace VpnHood.Core.TcpStack;

public sealed class LocalTcpClient(LocalTcpStream stream, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint) : ITcpClient, IAsyncDisposable, IDisposable
{
	public IPEndPoint LocalEndPoint { get; } = localEndPoint;

	public IPEndPoint RemoteEndPoint { get; } = remoteEndPoint;

	public Stream Stream => stream;

	public void Dispose()
	{
		stream.Dispose();
	}

	public ValueTask DisposeAsync()
	{
		return stream.DisposeAsync();
	}
}
