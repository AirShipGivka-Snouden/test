using System.Collections.Generic;

namespace VpnHood.Core.Tunneling.Messaging;

public class TcpPacketChannelRequest : RequestBase
{
	public string? ChannelId { get; init; }

	public IReadOnlyList<string>? ActiveChannelIds { get; init; }

	public TcpPacketChannelRequest()
		: base(VpnHood.Core.Tunneling.Messaging.RequestCode.TcpPacketChannel)
	{
	}
}
