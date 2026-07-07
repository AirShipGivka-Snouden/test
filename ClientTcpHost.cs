using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Packets;
using VpnHood.Core.TcpStack;
using VpnHood.Core.TcpStack.Abstractions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling;
using VpnHood.Core.Tunneling.Connections;
using VpnHood.Core.Tunneling.Utils;

namespace VpnHood.Core.Client;

internal class ClientTcpHost(ClientStreamHandler streamHandler) : IClientTcpHost, IDisposable
{
	private sealed class LocalStreamConnection(ITcpClient tcpClient) : IStreamConnection, IDisposable, IAsyncDisposable
	{
		private bool _disposed;

		public string ConnectionId { get; set; } = UniqueIdFactory.Create();

		public string ConnectionName => "app";

		public bool IsServer => false;

		public bool Connected
		{
			get
			{
				if (!_disposed)
				{
					Stream stream = tcpClient.Stream;
					if (stream != null && stream.CanRead)
					{
						return stream.CanWrite;
					}
					return false;
				}
				return false;
			}
		}

		public Stream Stream => tcpClient.Stream;

		public IPEndPoint LocalEndPoint { get; } = tcpClient.LocalEndPoint;

		public IPEndPoint RemoteEndPoint { get; } = tcpClient.RemoteEndPoint;

		public bool RequireHttpResponse { get; set; }

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				tcpClient.Dispose();
			}
		}

		public async ValueTask DisposeAsync()
		{
			if (!_disposed)
			{
				_disposed = true;
				await tcpClient.DisposeAsync().Vhc();
			}
		}
	}

	private bool _disposed;

	private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

	private ITcpStack? _tcpStack;

	private ITcpListener? _listener;

	public IReadOnlyList<IPAddress> CatcherAddressIps => Array.Empty<IPAddress>();

	public event EventHandler<IpPacket>? PacketReceived;

	public bool IsOwnPacket(IpPacket ipPacket)
	{
		return false;
	}

	public void DropCurrentConnections()
	{
		_tcpStack?.DropAllConnections();
	}

	public void Start()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		using (VhLogger.Instance.BeginScope("ClientTcpHost"))
		{
			VhLogger.Instance.LogInformation("Starting ClientTcpHost (TcpStack)...");
			_tcpStack = new LocalTcpStack
			{
				OnPacketSend = delegate(IpPacket packet)
				{
					this.PacketReceived?.Invoke(this, packet);
				}
			};
			_listener = _tcpStack.ListenAny();
			Task.Run(() => AcceptLoop(_listener, _cancellationTokenSource.Token));
		}
	}

	private async Task AcceptLoop(ITcpListener listener, CancellationToken cancellationToken)
	{
		_ = 1;
		try
		{
			await foreach (ITcpClient item in listener.AcceptAllAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
			{
				ProcessAcceptedStream(item, cancellationToken);
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex2)
		{
			if (!_disposed)
			{
				VhLogger.LogError(GeneralEventId.Request, ex2, "ClientHost accept loop terminated unexpectedly.");
			}
		}
		finally
		{
			VhLogger.Instance.LogInformation("ClientHost accept loop has been closed.");
		}
	}

	private async Task ProcessAcceptedStream(ITcpClient tcpClient, CancellationToken cancellationToken)
	{
		LocalStreamConnection connection = new LocalStreamConnection(tcpClient);
		try
		{
			IPEndPoint localEndPoint = tcpClient.LocalEndPoint;
			await streamHandler.ProcessConnection(connection, localEndPoint, cancellationToken).Vhc();
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogError(GeneralEventId.Stream, exception, "Could not process a tcp stream request.");
			await connection.DisposeAsync();
		}
	}

	public void ProcessOutgoingPacket(IpPacket ipPacket)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_tcpStack == null)
		{
			throw new InvalidOperationException("ClientHost has not been started.");
		}
		_tcpStack.ProcessIncoming(ipPacket);
		ipPacket.Dispose();
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_cancellationTokenSource.TryCancel();
			_cancellationTokenSource.Dispose();
			_listener?.Dispose();
			_tcpStack?.Dispose();
			this.PacketReceived = null;
		}
	}
}
