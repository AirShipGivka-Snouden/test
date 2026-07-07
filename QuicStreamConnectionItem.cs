using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling;
using VpnHood.Core.Tunneling.Connections;

namespace VpnHood.Core.Client.ConnectorServices;

internal class QuicStreamConnectionItem(int maxStreamsPerConnection, int maxLifetimeStreamsPerConnection, IPEndPoint quicEndPoint) : IAsyncDisposable
{
	private QuicConnection? _connection;

	private readonly AsyncLock _connectLock = new AsyncLock();

	private readonly Lock _usageLock = new Lock();

	public bool IsDead { get; private set; }

	public int ActiveStreamCount { get; private set; }

	public int TotalStreamCount { get; private set; }

	public DateTime? ZeroActiveSince { get; private set; }

	public bool CanOpenStream
	{
		get
		{
			if (!IsDead && ActiveStreamCount < maxStreamsPerConnection)
			{
				return TotalStreamCount < maxLifetimeStreamsPerConnection;
			}
			return false;
		}
	}

	public QuicConnection Connection => _connection ?? throw new InvalidOperationException("QUIC connection has not been established yet.");

	public async Task<IStreamConnection> OpenStreamConnection(VpnEndPoint vpnEndPoint, RemoteCertificateValidationCallback certificateValidationCallback, string connectionId, CancellationToken cancellationToken)
	{
		using (await _connectLock.LockAsync(cancellationToken).Vhc())
		{
			int num;
			_ = num - 1;
			_ = 1;
			try
			{
				if (_connection == null)
				{
					_connection = await ConnectAsync(vpnEndPoint, certificateValidationCallback, cancellationToken).Vhc();
				}
				QuicStream stream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, cancellationToken).Vhc();
				using (_usageLock.EnterScope())
				{
					TotalStreamCount++;
					ActiveStreamCount++;
					ZeroActiveSince = null;
				}
				QuicStreamConnection quicStreamConnection = new QuicStreamConnection(stream, Connection.LocalEndPoint, Connection.RemoteEndPoint, "tunnel", isServer: false, connectionId);
				quicStreamConnection.Disposed += Connection_Disposed;
				return quicStreamConnection;
			}
			catch
			{
				IsDead = true;
				throw;
			}
		}
	}

	private void Connection_Disposed(object? sender, EventArgs e)
	{
		using (_usageLock.EnterScope())
		{
			ActiveStreamCount--;
			if (ActiveStreamCount == 0)
			{
				ZeroActiveSince = FastDateTime.Now;
			}
		}
	}

	private ValueTask<QuicConnection> ConnectAsync(VpnEndPoint vpnEndPoint, RemoteCertificateValidationCallback certificateValidationCallback, CancellationToken cancellationToken)
	{
		VhLogger.Instance.LogDebug(GeneralEventId.Request, "Establishing a new QUIC connection to the Server... EndPoint: {EndPoint}", VhLogger.Format(quicEndPoint));
		QuicClientConnectionOptions obj = new QuicClientConnectionOptions
		{
			RemoteEndPoint = quicEndPoint,
			DefaultStreamErrorCode = 0L,
			DefaultCloseErrorCode = 0L
		};
		SslClientAuthenticationOptions obj2 = new SslClientAuthenticationOptions
		{
			CertificateRevocationCheckMode = X509RevocationMode.NoCheck
		};
		int num = 1;
		List<SslApplicationProtocol> list = new List<SslApplicationProtocol>(num);
		CollectionsMarshal.SetCount(list, num);
		CollectionsMarshal.AsSpan(list)[0] = SslApplicationProtocol.Http3;
		obj2.ApplicationProtocols = list;
		obj2.RemoteCertificateValidationCallback = certificateValidationCallback;
		obj2.EnabledSslProtocols = SslProtocols.Tls13;
		obj2.TargetHost = vpnEndPoint.HostName;
		obj2.EncryptionPolicy = EncryptionPolicy.RequireEncryption;
		obj.ClientAuthenticationOptions = obj2;
		return QuicConnection.ConnectAsync(obj, cancellationToken);
	}

	public ValueTask DisposeAsync()
	{
		return _connection?.DisposeAsync() ?? ValueTask.CompletedTask;
	}
}
