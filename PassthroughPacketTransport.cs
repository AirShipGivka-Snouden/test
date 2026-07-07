using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VpnHood.Core.Packets;

namespace VpnHood.Core.PacketTransports;

public abstract class PassthroughPacketTransport : PacketTransportBase
{
	protected PassthroughPacketTransport()
		: base(new PacketTransportOptions
		{
			AutoDisposePackets = false,
			Blocking = false
		}, singleMode: true, passthrough: true)
	{
	}

	protected sealed override ValueTask SendPacketsAsync(IReadOnlyList<IpPacket> ipPackets)
	{
		int count = ipPackets.Count;
		if (count <= 1)
		{
			if (count == 0)
			{
				return default(ValueTask);
			}
			SendPacket(ipPackets[0]);
			return default(ValueTask);
		}
		throw new ArgumentOutOfRangeException("ipPackets", "ipPackets should not be more than 1 in SinglePacketTransport");
	}

	protected abstract void SendPacket(IpPacket ipPacket);
}
