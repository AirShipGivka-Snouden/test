using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VpnHood.Core.TcpStack.Abstractions;

public interface ITcpListener : IDisposable
{
	IAsyncEnumerable<ITcpClient> AcceptAllAsync(CancellationToken cancellationToken = default(CancellationToken));

	ValueTask<ITcpClient> AcceptAsync(CancellationToken cancellationToken = default(CancellationToken));

	void Stop();
}
