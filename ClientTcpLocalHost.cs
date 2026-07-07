using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling;
using VpnHood.Core.Tunneling.Connections;
using VpnHood.Core.Tunneling.Exceptions;
using VpnHood.Core.Tunneling.Utils;

namespace VpnHood.Core.Client;

internal class ClientTcpLocalHost(ClientStreamHandler streamHandler, IPAddress catcherAddressIpV4, IPAddress catcherAddressIpV6) : IClientTcpHost, IDisposable
{
	private bool _disposed;

	private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

	private readonly Nat _nat = new Nat(isDestinationSensitive: true);

	private TcpListener? _tcpListenerIpV4;

	private TcpListener? _tcpListenerIpV6;

	private IPEndPoint? _localEndPointIpV4;

	private IPEndPoint? _localEndPointIpV6;

	public IReadOnlyList<IPAddress> CatcherAddressIps { get; } = new global::_003C_003Ez__ReadOnlyArray<IPAddress>(new IPAddress[2] { catcherAddressIpV4, catcherAddressIpV6 });

	public event EventHandler<IpPacket>? PacketReceived;

	public void DropCurrentConnections()
	{
		_nat.RemoveAll();
	}

	public bool IsOwnPacket(IpPacket ipPacket)
	{
		if (ipPacket.Protocol != IpProtocol.Tcp)
		{
			return false;
		}
		if (!catcherAddressIpV4.SpanEquals(ipPacket.DestinationAddressSpan))
		{
			return catcherAddressIpV6.SpanEquals(ipPacket.DestinationAddressSpan);
		}
		return true;
	}

	public void Start()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		using (VhLogger.Instance.BeginScope("ClientHost"))
		{
			VhLogger.Instance.LogInformation("Starting ClientHost...");
			_tcpListenerIpV4 = new TcpListener(IPAddress.Any, 0);
			_tcpListenerIpV4.Start();
			_localEndPointIpV4 = (IPEndPoint)_tcpListenerIpV4.LocalEndpoint;
			VhLogger.Instance.LogInformation("ClientHost is listening. EndPoint: {EndPoint}", VhLogger.Format(_localEndPointIpV4));
			AcceptTcpClientLoop(_tcpListenerIpV4);
			try
			{
				_tcpListenerIpV6 = new TcpListener(IPAddress.IPv6Any, 0);
				_tcpListenerIpV6.Start();
				_localEndPointIpV6 = (IPEndPoint)_tcpListenerIpV6.LocalEndpoint;
				VhLogger.Instance.LogInformation("ClientHost is listening. EndPoint: {EndPoint}", VhLogger.Format(_localEndPointIpV6));
				AcceptTcpClientLoop(_tcpListenerIpV6);
			}
			catch (Exception exception)
			{
				VhLogger.Instance.LogError(exception, "Could not create a listener. EndPoint: {EndPoint}", VhLogger.Format(new IPEndPoint(IPAddress.IPv6Any, 0)));
			}
		}
	}

	private async Task AcceptTcpClientLoop(TcpListener tcpListener)
	{
		IPEndPoint localEp = (IPEndPoint)tcpListener.LocalEndpoint;
		try
		{
			while (!_cancellationTokenSource.IsCancellationRequested)
			{
				TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync().Vhc();
				bool? keepAlive = true;
				VhUtils.ConfigTcpClient(tcpClient, null, null, null, keepAlive);
				TcpStreamConnection streamConnection = new TcpStreamConnection(tcpClient, "app", isServer: false);
				ProcessConnection(streamConnection, _cancellationTokenSource.Token);
			}
		}
		catch (Exception ex)
		{
			if (!_disposed)
			{
				VhLogger.LogError(GeneralEventId.Request, ex, "");
			}
		}
		finally
		{
			VhLogger.Instance.LogInformation("ClientHost Listener has been closed. LocalEp: {localEp}", localEp);
		}
	}

	public void ProcessOutgoingPacket(IpPacket ipPacket)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		PacketLogger.LogPacket(ipPacket, "Processing a ClientHost packet...");
		if (_localEndPointIpV4 == null)
		{
			throw new InvalidOperationException("_localEndPointIpV4 has not been initialized! Did you call Start!");
		}
		IPAddress iPAddress = ((ipPacket.Version == IpVersion.IPv4) ? catcherAddressIpV4 : catcherAddressIpV6);
		IPEndPoint iPEndPoint = ((ipPacket.Version == IpVersion.IPv4) ? _localEndPointIpV4 : _localEndPointIpV6);
		try
		{
			TcpPacket tcpPacket = ipPacket.ExtractTcp();
			if (iPEndPoint == null)
			{
				throw new Exception("There is no localEndPoint registered for this packet.");
			}
			if (iPAddress.SpanEquals(ipPacket.DestinationAddressSpan))
			{
				NatItemEx natItemEx = ((NatItemEx)_nat.Resolve(ipPacket.Version, ipPacket.Protocol, tcpPacket.DestinationPort)) ?? throw new NatEndPointNotFoundException("Could not find incoming tcp destination in NAT.");
				ipPacket.SourceAddress = natItemEx.DestinationAddress;
				ipPacket.DestinationAddress = natItemEx.SourceAddress;
				tcpPacket.SourcePort = natItemEx.DestinationPort;
				tcpPacket.DestinationPort = natItemEx.SourcePort;
			}
			else
			{
				NatItem natItem = ((tcpPacket != null && tcpPacket.Synchronize && !tcpPacket.Acknowledgment) ? _nat.Add(ipPacket, overwrite: true) : (_nat.Get(ipPacket) ?? throw new NatEndPointNotFoundException("Could not find outgoing tcp destination in NAT.")));
				tcpPacket.SourcePort = natItem.NatId;
				ipPacket.DestinationAddress = ipPacket.SourceAddress;
				ipPacket.SourceAddress = iPAddress;
				tcpPacket.DestinationPort = (ushort)iPEndPoint.Port;
			}
			ipPacket.UpdateAllChecksums();
			this.PacketReceived?.Invoke(this, ipPacket);
		}
		catch (NatEndPointNotFoundException innerException) when (ipPacket.Protocol == IpProtocol.Tcp)
		{
			IpPacket e = PacketBuilder.BuildTcpResetReply(ipPacket);
			this.PacketReceived?.Invoke(this, e);
			throw new PacketDropException("Packet dropped and TCP reset sent.", innerException);
		}
	}

	private async Task ProcessConnection(IStreamConnection streamConnection, CancellationToken cancellationToken)
	{
		try
		{
			IPEndPoint remoteEndPoint = streamConnection.RemoteEndPoint;
			IpVersion ipVersion = remoteEndPoint.IpVersion();
			NatItemEx natItemEx = ((NatItemEx)_nat.Resolve(ipVersion, IpProtocol.Tcp, (ushort)remoteEndPoint.Port)) ?? throw new Exception($"Could not resolve original remote from NAT! RemotePort: {remoteEndPoint.Port}");
			IPAddress objB = ((ipVersion == IpVersion.IPv4) ? catcherAddressIpV4 : catcherAddressIpV6);
			if (!object.Equals(streamConnection.RemoteEndPoint.Address, objB))
			{
				throw new Exception("TcpProxy rejected an outbound connection!");
			}
			IPEndPoint hostEndPoint = new IPEndPoint(natItemEx.DestinationAddress, natItemEx.DestinationPort);
			await streamHandler.ProcessConnection(streamConnection, hostEndPoint, cancellationToken).Vhc();
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogError(GeneralEventId.Stream, exception, "Could not process a tcp stream request.");
			await streamConnection.DisposeAsync();
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_cancellationTokenSource.TryCancel();
			_cancellationTokenSource.Dispose();
			_tcpListenerIpV4?.Stop();
			_tcpListenerIpV6?.Stop();
			_nat.Dispose();
			this.PacketReceived = null;
			_disposed = true;
		}
	}
}
