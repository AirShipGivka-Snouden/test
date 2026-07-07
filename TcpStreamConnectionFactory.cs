using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Proxies.EndPointManagement;
using VpnHood.Core.Toolkit.Extensions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Sockets;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling;
using VpnHood.Core.Tunneling.Connections;

namespace VpnHood.Core.Client.ConnectorServices;

internal class TcpStreamConnectionFactory(ISocketFactory socketFactory, ProxyEndPointManager proxyEndPointManager, VpnEndPoint vpnEndPoint, RemoteCertificateValidationCallback certificateValidationCallback) : IDisposable
{
	public async Task<IStreamConnection> CreateConnection(string connectionId, Action? onConnectAttempt, CancellationToken cancellationToken)
	{
		IPEndPoint tcpEndPoint = vpnEndPoint.TcpEndPoint;
		TcpClient tcpClient = null;
		try
		{
			VhLogger.Instance.LogDebug(GeneralEventId.Request, "Establishing a new TCP to the Server... EndPoint: {EndPoint}", VhLogger.Format(tcpEndPoint));
			if (proxyEndPointManager.IsEnabled)
			{
				tcpClient = await proxyEndPointManager.ConnectAsync(tcpEndPoint, onConnectAttempt, cancellationToken).Vhc();
			}
			else
			{
				tcpClient = socketFactory.CreateTcpClient(tcpEndPoint);
				await tcpClient.ConnectAsync(tcpEndPoint, cancellationToken).Vhc();
			}
			return await AuthenticateTls(connectionId, tcpClient, cancellationToken).Vhc();
		}
		catch (Exception ex)
		{
			if (proxyEndPointManager.IsEnabled && tcpClient != null)
			{
				proxyEndPointManager.RecordFailed(tcpClient, ex);
			}
			tcpClient?.Dispose();
			throw;
		}
	}

	private async Task<IStreamConnection> AuthenticateTls(string connectionId, TcpClient tcpClient, CancellationToken cancellationToken)
	{
		SslStream sslStream = new SslStream(tcpClient.GetStream(), leaveInnerStreamOpen: true, certificateValidationCallback);
		try
		{
			string hostName = vpnEndPoint.HostName;
			VhLogger.Instance.LogDebug(GeneralEventId.Request, "TLS Authenticating... HostName: {HostName}", VhLogger.FormatHostName(hostName));
			await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
			{
				TargetHost = hostName,
				EnabledSslProtocols = SslProtocols.None
			}, cancellationToken).Vhc();
			return new TcpStreamConnection(tcpClient, sslStream, "tunnel", isServer: false, connectionId);
		}
		catch
		{
			await sslStream.SafeDisposeAsync();
			throw;
		}
	}

	public void Dispose()
	{
	}
}
