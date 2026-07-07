namespace VpnHood.Core.PacketTransports;

public abstract class PacketTransport : PacketTransportBase
{
	protected PacketTransport(PacketTransportOptions options)
		: base(options, singleMode: false, passthrough: false)
	{
	}
}
