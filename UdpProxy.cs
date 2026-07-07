using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.PacketTransports;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Collections;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.Utils;

namespace VpnHood.Core.Tunneling.Proxies;

internal class UdpProxy : SinglePacketTransport, ITimeoutItem, IDisposable
{
	private readonly UdpClient _udpClient;

	private readonly IPEndPoint? _sourceEndPoint;

	private IPEndPoint? _destinationEndPoint;

	public IPEndPoint LocalEndPoint { get; }

	public AddressFamily AddressFamily { get; }

	public DateTime LastUsedTime { get; set; }

	public new bool IsDisposed => base.IsDisposed;

	public UdpProxy(UdpClient udpClient, IPEndPoint? sourceEndPoint, int queueCapacity, bool autoDisposePackets)
		: base(new PacketTransportOptions
		{
			AutoDisposePackets = autoDisposePackets,
			Blocking = false,
			QueueCapacity = queueCapacity
		})
	{
		_udpClient = udpClient;
		_sourceEndPoint = sourceEndPoint;
		LastUsedTime = FastDateTime.Now;
		LocalEndPoint = udpClient.Client.GetLocalEndPoint();
		AddressFamily = LocalEndPoint.AddressFamily;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			udpClient.Client.IOControl(-1744830452, new byte[1], new byte[1]);
		}
		StartReceivingAsync();
	}

	protected virtual IPEndPoint? GetSourceEndPoint(IPEndPoint remoteEndPoint)
	{
		return _sourceEndPoint;
	}

	protected override async ValueTask SendPacketAsync(IpPacket ipPacket)
	{
		try
		{
			if (ipPacket.Protocol == IpProtocol.IPv4 && ipPacket is IpV4Packet ipV4Packet && AddressFamily.IsV4())
			{
				_udpClient.DontFragment = ipV4Packet.DontFragment;
			}
			UdpPacket udpPacket = ipPacket.ExtractUdp();
			if (_destinationEndPoint == null || !_destinationEndPoint.Address.Equals(ipPacket.DestinationAddress) || _destinationEndPoint.Port != udpPacket.DestinationPort)
			{
				_destinationEndPoint = new IPEndPoint(ipPacket.DestinationAddress, udpPacket.DestinationPort);
			}
			int num = await _udpClient.SendAsync(udpPacket.Payload, _destinationEndPoint).Vhc();
			LastUsedTime = FastDateTime.Now;
			if (num != udpPacket.Payload.Length)
			{
				throw new Exception($"Couldn't send all udp bytes. Requested: {udpPacket.Payload.Length}, Sent: {num}");
			}
		}
		catch (Exception ex) when (SocketUtils.IsInvalidUdpStateException(ex))
		{
			VhLogger.Instance.LogError(ex, "Invalid UDP state detected in UdpProxy. Disposing the proxy.");
			Dispose();
			throw;
		}
	}

	private async Task StartReceivingAsync()
	{
		while (!IsDisposed)
		{
			try
			{
				UdpReceiveResult udpReceiveResult = await _udpClient.ReceiveAsync().Vhc();
				LastUsedTime = FastDateTime.Now;
				IPEndPoint sourceEndPoint = GetSourceEndPoint(udpReceiveResult.RemoteEndPoint);
				if (sourceEndPoint == null)
				{
					VhLogger.Instance.LogInformation(GeneralEventId.Udp, "Could not find UDP source address.");
					break;
				}
				IpPacket ipPacket = PacketBuilder.BuildUdp(udpReceiveResult.RemoteEndPoint, sourceEndPoint, udpReceiveResult.Buffer);
				ipPacket.UpdateAllChecksums();
				OnPacketReceived(ipPacket);
			}
			catch (Exception) when (IsDisposed)
			{
				break;
			}
			catch (Exception ex2) when (SocketUtils.IsInvalidUdpStateException(ex2))
			{
				VhLogger.Instance.LogError(ex2, "Unexpected error in UDP receive loop.");
				Dispose();
				break;
			}
			catch (Exception exception)
			{
				VhLogger.Instance.LogError(exception, "Error in UdpProxy receive loop.");
			}
		}
	}

	protected override void DisposeManaged()
	{
		_udpClient.Dispose();
		base.DisposeManaged();
	}
}
