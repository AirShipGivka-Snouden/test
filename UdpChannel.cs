using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Packets;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.Utils;

namespace VpnHood.Core.Tunneling.Channels;

public class UdpChannel : PacketChannel
{
	private readonly Memory<byte> _buffer = new byte[1500];

	private readonly TaskCompletionSource<bool> _readingTask = new TaskCompletionSource<bool>();

	private readonly bool _leaveUdpTransportOpen;

	public IUdpTransport UdpTransport { get; }

	public override int OverheadLength => UdpTransport.OverheadLength;

	public UdpChannel(IUdpTransport udpTransport, UdpChannelOptions options)
		: base(options)
	{
		UdpTransport = udpTransport;
		UdpTransport.DataReceived = OnDataReceived;
		_leaveUdpTransportOpen = options.LeaveUdpTransportOpen;
	}

	protected override Task StartReadTask()
	{
		return _readingTask.Task;
	}

	private async Task SendBuffer(ReadOnlyMemory<byte> buffer)
	{
		await UdpTransport.SendAsync(buffer).Vhc();
	}

	protected override async ValueTask SendPacketsAsync(IReadOnlyList<IpPacket> ipPackets)
	{
		_ = 1;
		try
		{
			TrafficMeter trafficMeter = base.TrafficMeter;
			int bufferIndex = 0;
			for (int i = 0; i < ipPackets.Count; i++)
			{
				IpPacket ipPacket = ipPackets[i];
				Memory<byte> packetBytes = ipPacket.Buffer;
				if (trafficMeter != null && trafficMeter.ShouldThrottleSend())
				{
					VhLogger.Instance.LogDebug(GeneralEventId.Udp, "Dropping a UDP packet due to send throttle. ChannelId: {ChannelId}, PacketLength: {PacketLength}", base.ChannelId, packetBytes.Length);
					continue;
				}
				if (bufferIndex > 0 && bufferIndex + packetBytes.Length > _buffer.Length - UdpTransport.OverheadLength)
				{
					UdpChannel udpChannel = this;
					Memory<byte> buffer = _buffer;
					await udpChannel.SendBuffer(buffer.Slice(0, bufferIndex)).Vhc();
					trafficMeter?.OnSent(bufferIndex);
					bufferIndex = 0;
				}
				if (bufferIndex + packetBytes.Length > _buffer.Length - UdpTransport.OverheadLength)
				{
					VhLogger.Instance.LogWarning(GeneralEventId.Udp, "Packet is too big to send. PacketLength: {PacketLength}", packetBytes.Length);
				}
				else
				{
					packetBytes.Span.CopyTo(_buffer.Span.Slice(bufferIndex));
					bufferIndex += packetBytes.Length;
					packetBytes = default(Memory<byte>);
				}
			}
			if (bufferIndex > 0)
			{
				UdpChannel udpChannel2 = this;
				Memory<byte> buffer = _buffer;
				await udpChannel2.SendBuffer(buffer.Slice(0, bufferIndex)).Vhc();
				trafficMeter?.OnSent(bufferIndex);
			}
		}
		catch (Exception ex) when (((ex is OperationCanceledException || ex is ObjectDisposedException) ? 1 : 0) != 0)
		{
			Dispose();
		}
		catch (Exception exception) when (UdpTransport.Connected)
		{
			VhLogger.Instance.LogTrace(GeneralEventId.Udp, exception, "Unexpected error in sending packets. ChannelId: {ChannelId}", base.ChannelId);
		}
		catch (Exception exception2)
		{
			VhLogger.Instance.LogError(GeneralEventId.Udp, exception2, "UdpChannel has been closed due to a transport error in sending packets. ChannelId: {ChannelId}", base.ChannelId);
			Dispose();
		}
	}

	private static IpPacket ReadNextPacketKeepMemory(Memory<byte> buffer)
	{
		return PacketBuilder.Attach(buffer[..PacketUtil.ReadPacketLength(buffer.Span)]);
	}

	public void OnDataReceived(Memory<byte> buffer)
	{
		int num = 0;
		while (num < buffer.Length)
		{
			IpPacket ipPacket = ReadNextPacketKeepMemory(buffer.Slice(num));
			num += ipPacket.PacketLength;
			if (base.TrafficMeter != null && base.TrafficMeter.ShouldThrottleReceive())
			{
				PacketLogger.LogPacket(ipPacket, "Dropping a UDP packet due to receive throttle. ChannelId: " + base.ChannelId, LogLevel.Trace, null, GeneralEventId.Udp);
				ipPacket.Dispose();
			}
			else
			{
				base.TrafficMeter?.OnReceived(ipPacket.PacketLength);
				OnPacketReceived(ipPacket);
			}
		}
	}

	protected override void DisposeManaged()
	{
		if (!_leaveUdpTransportOpen)
		{
			UdpTransport.Dispose();
		}
		_readingTask.TrySetResult(result: true);
		base.DisposeManaged();
	}
}
