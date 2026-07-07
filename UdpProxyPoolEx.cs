using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.PacketTransports;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Collections;
using VpnHood.Core.Toolkit.Jobs;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Sockets;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.Exceptions;

namespace VpnHood.Core.Tunneling.Proxies;

public class UdpProxyPoolEx : PassthroughPacketTransport, IPacketProxyPool, IPacketTransport, IDisposable
{
	private readonly IPacketProxyCallbacks? _packetProxyCallbacks;

	private readonly ISocketFactory _socketFactory;

	private readonly TransferBufferSize? _bufferSize;

	private readonly TimeoutDictionary<string, UdpProxyEx> _connectionMap;

	private readonly List<UdpProxyEx> _udpProxies = new List<UdpProxyEx>();

	private readonly TimeoutDictionary<IPEndPoint, TimeoutItem<bool>> _remoteEndPoints;

	private readonly EventReporter _maxWorkerEventReporter;

	private readonly TimeSpan _udpTimeout;

	private readonly int _maxClientCount;

	private readonly int _packetQueueCapacity;

	private readonly bool _autoDisposeSentPackets;

	private readonly Job _cleanupUdpWorkersJob;

	public int RemoteEndPointCount => _remoteEndPoints.Count;

	public int ClientCount
	{
		get
		{
			lock (_udpProxies)
			{
				return _udpProxies.Count;
			}
		}
	}

	public UdpProxyPoolEx(UdpProxyPoolOptions options)
	{
		_autoDisposeSentPackets = options.AutoDisposePackets;
		_packetProxyCallbacks = options.PacketProxyCallbacks;
		_socketFactory = options.SocketFactory;
		_packetQueueCapacity = options.PacketQueueCapacity;
		_remoteEndPoints = new TimeoutDictionary<IPEndPoint, TimeoutItem<bool>>(options.UdpTimeout);
		_maxClientCount = options.MaxClientCount;
		_bufferSize = options.BufferSize;
		_maxWorkerEventReporter = new EventReporter("Session has reached to Maximum local UDP ports.", GeneralEventId.NetProtect, options.LogScope);
		_connectionMap = new TimeoutDictionary<string, UdpProxyEx>(options.UdpTimeout);
		_udpTimeout = options.UdpTimeout;
		_cleanupUdpWorkersJob = new Job(CleanupUdpWorkers, options.UdpTimeout, "UdpProxyPoolEx");
	}

	protected override void SendPacket(IpPacket ipPacket)
	{
		UdpPacket udpPacket = ipPacket.ExtractUdp();
		IPEndPoint iPEndPoint = new IPEndPoint(ipPacket.SourceAddress, udpPacket.SourcePort);
		IPEndPoint destinationEndPoint = new IPEndPoint(ipPacket.DestinationAddress, udpPacket.DestinationPort);
		AddressFamily addressFamily = ipPacket.SourceAddress.AddressFamily;
		bool isNewRemoteEndPoint = false;
		bool flag = false;
		lock (_udpProxies)
		{
			string key = $"{iPEndPoint}:{destinationEndPoint}";
			if (!_connectionMap.TryGetValue(key, out UdpProxyEx value))
			{
				_remoteEndPoints.GetOrAdd(destinationEndPoint, delegate
				{
					isNewRemoteEndPoint = true;
					return new TimeoutItem<bool>(value: true);
				});
				if (isNewRemoteEndPoint)
				{
					_packetProxyCallbacks?.OnConnectionRequested(IpProtocol.Udp, destinationEndPoint.ToValue());
				}
				TimeoutItemUtil.CleanupTimeoutList(_udpProxies, _udpTimeout);
				value = _udpProxies.FirstOrDefault((UdpProxyEx x) => !x.IsDisposed && x.AddressFamily == addressFamily && !x.DestinationEndPointMap.TryGetValue(destinationEndPoint, out TimeoutItem<IPEndPoint> _));
				if (value == null)
				{
					if (_udpProxies.Count >= _maxClientCount)
					{
						_maxWorkerEventReporter.Raise();
						throw new UdpClientQuotaException(_udpProxies.Count);
					}
					value = new UdpProxyEx(CreateUdpClient(addressFamily), _udpTimeout, _packetQueueCapacity, _autoDisposeSentPackets);
					value.PacketReceived += UdpProxy_OnPacketReceived;
					VhLogger.Instance.LogTrace(GeneralEventId.Udp, "Created a new UdpProxyEx. WorkerCount: {WorkerCount}, {SourceEp} => {DestinationEp}", _udpProxies.Count, VhLogger.Format(iPEndPoint), VhLogger.Format(destinationEndPoint));
					_udpProxies.Add(value);
					flag = true;
				}
				if (!value.DestinationEndPointMap.TryAdd(destinationEndPoint, new TimeoutItem<IPEndPoint>(iPEndPoint)))
				{
					value.Dispose();
					throw new Exception($"Could not add {destinationEndPoint}.");
				}
				_connectionMap.AddOrUpdate(key, value);
			}
			if (flag || isNewRemoteEndPoint)
			{
				_packetProxyCallbacks?.OnConnectionEstablished(IpProtocol.Udp, value.LocalEndPoint.ToValue(), destinationEndPoint.ToValue(), flag, isNewRemoteEndPoint);
			}
			value.SendPacketQueued(ipPacket);
		}
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
		lock (_udpProxies)
		{
			TimeoutItemUtil.CleanupTimeoutList(_udpProxies, _udpTimeout);
		}
		return default(ValueTask);
	}

	protected override void DisposeManaged()
	{
		lock (_udpProxies)
		{
			_udpProxies.ForEach(delegate(UdpProxyEx udpWorker)
			{
				udpWorker.Dispose();
			});
		}
		_cleanupUdpWorkersJob.Dispose();
		_connectionMap.Dispose();
		_remoteEndPoints.Dispose();
		_maxWorkerEventReporter.Dispose();
		base.DisposeManaged();
	}
}
