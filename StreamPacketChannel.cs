using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Packets;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.Connections;

namespace VpnHood.Core.Tunneling.Channels;

public class StreamPacketChannel(StreamPacketChannelOptions options) : PacketChannel(options)
{
	private readonly int _receiveBufferSize = options.BufferSize.Receive;

	private readonly Memory<byte> _sendBuffer = new byte[options.BufferSize.Send];

	private readonly IStreamConnection _streamConnection = options.StreamConnection;

	public DateTime RequestTime { get; } = options.RequestTime;

	public override int OverheadLength => 0;

	protected override async ValueTask SendPacketsAsync(IReadOnlyList<IpPacket> ipPackets)
	{
		ObjectDisposedException.ThrowIf(base.IsDisposed, this);
		CancellationToken cancellationToken = base.CancellationToken;
		Memory<byte> buffer = _sendBuffer;
		int bufferIndex = 0;
		for (int i = 0; i < ipPackets.Count; i++)
		{
			IpPacket ipPacket = ipPackets[i];
			Memory<byte> packetBytes = ipPacket.Buffer;
			if (bufferIndex > 0 && bufferIndex + packetBytes.Length > buffer.Length)
			{
				await WriteBuffer(buffer.Slice(0, bufferIndex), cancellationToken);
				bufferIndex = 0;
			}
			if (packetBytes.Length > buffer.Length || ipPackets.Count == 1)
			{
				await WriteBuffer(packetBytes, cancellationToken);
				continue;
			}
			packetBytes.Span.CopyTo(buffer.Span.Slice(bufferIndex));
			bufferIndex += packetBytes.Length;
			packetBytes = default(Memory<byte>);
		}
		if (bufferIndex > 0)
		{
			await WriteBuffer(buffer.Slice(0, bufferIndex), cancellationToken);
		}
	}

	private async ValueTask WriteBuffer(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
	{
		TrafficMeter trafficMeter = base.TrafficMeter;
		if (trafficMeter != null)
		{
			ValueTask task = trafficMeter.ThrottleSendAsync(cancellationToken);
			if (!task.IsCompleted)
			{
				await task.Vhc();
			}
		}
		await _streamConnection.Stream.WriteAsync(buffer, cancellationToken).Vhc();
		trafficMeter?.OnSent(buffer.Length);
	}

	protected override async Task StartReadTask()
	{
		CancellationToken cancellationToken = base.CancellationToken;
		using StreamPacketReader streamPacketReader = new StreamPacketReader(_streamConnection.Stream, _receiveBufferSize);
		while (!cancellationToken.IsCancellationRequested)
		{
			IpPacket ipPacket = await streamPacketReader.ReadAsync(cancellationToken).Vhc();
			if (ipPacket == null)
			{
				VhLogger.Instance.LogDebug(GeneralEventId.PacketChannel, "Packet stream ended. Terminating read task.");
				break;
			}
			base.TrafficMeter?.OnReceived(ipPacket.PacketLength);
			if (base.TrafficMeter != null)
			{
				ValueTask task = base.TrafficMeter.ThrottleReceiveAsync(cancellationToken);
				if (!task.IsCompleted)
				{
					await task.Vhc();
				}
			}
			OnPacketReceived(ipPacket);
		}
	}

	protected override void DisposeManaged()
	{
		Stop();
		_streamConnection.Dispose();
		base.DisposeManaged();
	}
}
