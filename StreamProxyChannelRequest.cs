using System.Net;
using System.Text.Json.Serialization;
using VpnHood.Core.Toolkit.Converters;

namespace VpnHood.Core.Tunneling.Messaging;

public class StreamProxyChannelRequest : RequestBase
{
	[JsonConverter(typeof(IPEndPointConverter))]
	public required IPEndPoint DestinationEndPoint { get; set; }

	public StreamProxyChannelRequest()
		: base(VpnHood.Core.Tunneling.Messaging.RequestCode.ProxyChannel)
	{
	}
}
