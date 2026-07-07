using System;
using VpnHood.Core.PacketTransports;

namespace VpnHood.Core.Tunneling.Channels;

public interface IPacketChannel : IPacketTransport, IDisposable, IChannel
{
	int OverheadLength { get; }
}
