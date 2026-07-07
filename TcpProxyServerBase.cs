using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VpnHood.Core.Proxies.HttpProxyServers;

namespace VpnHood.Core.Proxies;

public abstract class TcpProxyServerBase(IPEndPoint listerEndPoint, int backlog, ILogger? logger) : IDisposable
{
	protected readonly ILogger Logger = logger ?? NullLogger<HttpProxyServer>.Instance;

	private CancellationTokenSource? _serverCts;

	private readonly TcpListener _listener = new TcpListener(listerEndPoint);

	public IPEndPoint ListenerEndPoint => (IPEndPoint)_listener.LocalEndpoint;

	public bool IsStarted
	{
		get
		{
			if (_serverCts != null)
			{
				return !_serverCts.IsCancellationRequested;
			}
			return false;
		}
	}

	protected abstract object HandleClientAsync(TcpClient client, CancellationToken cancellationToken);

	public void Start()
	{
		if (!IsStarted)
		{
			_listener.Start(backlog);
			_serverCts = new CancellationTokenSource();
			Listen(_serverCts.Token);
			Logger.LogInformation("HTTP proxy server started on {EndPoint}", listerEndPoint);
		}
	}

	public void Stop()
	{
		if (IsStarted)
		{
			_serverCts?.Cancel();
			_serverCts = null;
			_listener.Stop();
			Logger.LogInformation("HTTP proxy server stopped");
		}
	}

	private async Task Listen(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				HandleClientAsync(await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false), cancellationToken);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception exception)
			{
				Logger?.LogError(exception, "Error accepting client connection");
			}
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			Stop();
			_serverCts?.Dispose();
			_listener.Dispose();
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
