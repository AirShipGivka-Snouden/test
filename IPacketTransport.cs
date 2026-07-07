using System;
using System.Threading.Tasks;
using VpnHood.Core.Packets;

namespace VpnHood.Core.PacketTransports;

public interface IPacketTransport : IDisposable
{
	bool IsSending { get; }

	ReadOnlyPacketTransportStat PacketStat { get; }

	event EventHandler<IpPacket>? PacketReceived;

	bool SendPacketQueued(IpPacket ipPacket);

	ValueTask SendPacketQueuedAsync(IpPacket ipPacket);
}
