using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VpnHood.Core.Packets;

namespace VpnHood.Core.PacketTransports;

public abstract class SinglePacketTransport : PacketTransportBase
{
	protected SinglePacketTransport(PacketTransportOptions options)
		: base(options, singleMode: true, passthrough: false)
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
			return SendPacketAsync(ipPackets[0]);
		}
		throw new ArgumentOutOfRangeException("ipPackets", "ipPackets should not be more than 1 in SinglePacketTransport");
	}

	protected abstract ValueTask SendPacketAsync(IpPacket ipPacket);
}
