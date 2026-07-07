using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.PacketTransports;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Collections;
using VpnHood.Core.Toolkit.Jobs;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Sockets;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.Exceptions;

namespace VpnHood.Core.Tunneling.Proxies;

public class UdpProxyPool : PassthroughPacketTransport, IPacketProxyPool, IPacketTransport, IDisposable
{
	private readonly bool _autoDisposeSentPackets;

	private readonly IPacketProxyCallbacks? _packetProxyCallbacks;

	private readonly ISocketFactory _socketFactory;

	private readonly TransferBufferSize? _bufferSize;

	private readonly int _packetQueueCapacity;

	private readonly TimeoutDictionary<IPEndPoint, UdpProxy> _udpProxies;

	private readonly TimeoutDictionary<IPEndPoint, TimeoutItem<bool>> _remoteEndPoints;

	private readonly EventReporter _maxWorkerEventReporter;

	private readonly int _maxClientCount;

	private readonly Job _cleanupUdpJob;

	public int RemoteEndPointCount => _remoteEndPoints.Count;

	public int ClientCount => _udpProxies.Count;

	public UdpProxyPool(UdpProxyPoolOptions options)
	{
		_autoDisposeSentPackets = options.AutoDisposePackets;
		_packetProxyCallbacks = options.PacketProxyCallbacks;
		_socketFactory = options.SocketFactory;
		_packetQueueCapacity = options.PacketQueueCapacity;
		_remoteEndPoints = new TimeoutDictionary<IPEndPoint, TimeoutItem<bool>>(options.UdpTimeout);
		_maxClientCount = options.MaxClientCount;
		_bufferSize = options.BufferSize;
		_maxWorkerEventReporter = new EventReporter("Session has reached to Maximum local UDP ports.", GeneralEventId.NetProtect, options.LogScope);
		_udpProxies = new TimeoutDictionary<IPEndPoint, UdpProxy>(options.UdpTimeout);
		_cleanupUdpJob = new Job(CleanupUdpWorkers, options.UdpTimeout, "UdpProxyPool");
	}

	protected override void SendPacket(IpPacket ipPacket)
	{
		UdpPacket udpPacket = ipPacket.ExtractUdp();
		IPEndPoint key = new IPEndPoint(ipPacket.SourceAddress, udpPacket.SourcePort);
		IPEndPoint iPEndPoint = new IPEndPoint(ipPacket.DestinationAddress, udpPacket.DestinationPort);
		AddressFamily addressFamily = ipPacket.SourceAddress.AddressFamily;
		bool isNewRemoteEndPoint = false;
		bool isNewLocalEndPoint = false;
		_remoteEndPoints.GetOrAdd(iPEndPoint, delegate
		{
			isNewRemoteEndPoint = true;
			return new TimeoutItem<bool>(value: true);
		});
		if (isNewRemoteEndPoint)
		{
			_packetProxyCallbacks?.OnConnectionRequested(IpProtocol.Udp, iPEndPoint.ToValue());
		}
		UdpProxy orAdd = _udpProxies.GetOrAdd(key, delegate(IPEndPoint sourceEndPoint)
		{
			if (_udpProxies.Count >= _maxClientCount)
			{
				_maxWorkerEventReporter.Raise();
				throw new UdpClientQuotaException(_udpProxies.Count);
			}
			isNewLocalEndPoint = true;
			UdpProxy udpProxy = new UdpProxy(CreateUdpClient(addressFamily), sourceEndPoint, _packetQueueCapacity, _autoDisposeSentPackets);
			udpProxy.PacketReceived += UdpProxy_OnPacketReceived;
			return udpProxy;
		});
		if (isNewLocalEndPoint || isNewRemoteEndPoint)
		{
			_packetProxyCallbacks?.OnConnectionEstablished(IpProtocol.Udp, orAdd.LocalEndPoint.ToValue(), iPEndPoint.ToValue(), isNewLocalEndPoint, isNewRemoteEndPoint);
		}
		orAdd.SendPacketQueued(ipPacket);
	}

	private void UdpProxy_OnPacketReceived(object? sender, IpPacket ipPacket)
	{
		OnPacketReceived(ipPacket);
	}

	private UdpClient CreateUdpClient(AddressFamily addressFamily)
	{
		UdpClient udpClient = _socketFactory.CreateUdpClient(addressFamily);
		ref readonly TransferBufferSize? bufferSize = ref _bufferSize;
		if (bufferSize.HasValue && bufferSize.GetValueOrDefault().Send > 0)
		{
			udpClient.Client.SendBufferSize = _bufferSize.Value.Send;
		}
		ref readonly TransferBufferSize? bufferSize2 = ref _bufferSize;
		if (bufferSize2.HasValue && bufferSize2.GetValueOrDefault().Receive > 0)
		{
			udpClient.Client.ReceiveBufferSize = _bufferSize.Value.Receive;
		}
		return udpClient;
	}

	private ValueTask CleanupUdpWorkers(CancellationToken cancellationToken)
	{
		_udpProxies.Cleanup();
		return default(ValueTask);
	}

	protected override void DisposeManaged()
	{
		_remoteEndPoints.Dispose();
		_maxWorkerEventReporter.Dispose();
		_udpProxies.Dispose();
		_cleanupUdpJob.Dispose();
		base.DisposeManaged();
	}
}
