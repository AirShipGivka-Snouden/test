using System;
using System.IO;
using System.Net;

namespace VpnHood.Core.TcpStack.Abstractions;

public interface ITcpClient : IAsyncDisposable, IDisposable
{
	IPEndPoint LocalEndPoint { get; }

	IPEndPoint RemoteEndPoint { get; }

	Stream Stream { get; }
}
