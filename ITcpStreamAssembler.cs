using VpnHood.Core.Packets;

namespace VpnHood.Core.Client;

public interface ITcpStreamAssembler
{
	bool IsOwnPacket(IpPacket ipPacket);

	void ProcessOutgoingPacket(IpPacket ipPacket);
}
