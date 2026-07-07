using System;
using System.IO;
using System.Net;

namespace VpnHood.Core.Tunneling.Connections;

public interface IStreamConnection : IDisposable, IAsyncDisposable
{
	string ConnectionId { get; set; }

	string ConnectionName { get; }

	bool IsServer { get; }

	bool Connected { get; }

	Stream Stream { get; }

	IPEndPoint LocalEndPoint { get; }

	IPEndPoint RemoteEndPoint { get; }

	bool RequireHttpResponse { get; set; }
}
