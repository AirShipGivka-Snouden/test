using System.Collections.Generic;
using System.Net.Quic;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Client.Abstractions;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Tunneling.Messaging;

namespace VpnHood.Core.Client;

internal static class ChannelProtocolValidator
{
	public static ChannelProtocol[] GetChannelProtocols(HelloResponse helloResponse)
	{
		List<ChannelProtocol> list = new List<ChannelProtocol> { ChannelProtocol.Tcp };
		if (helloResponse != null)
		{
			int? udpPort = helloResponse.UdpPort;
			if (udpPort.HasValue && udpPort.GetValueOrDefault() > 0 && helloResponse.ProtocolVersion >= 11)
			{
				list.Add(ChannelProtocol.Udp);
			}
		}
		if (helloResponse != null)
		{
			int? udpPort = helloResponse.QuicPort;
			if (udpPort.HasValue && udpPort.GetValueOrDefault() > 0 && helloResponse.ProtocolVersion >= 13 && QuicConnection.IsSupported)
			{
				list.Add(ChannelProtocol.Quic);
			}
		}
		return list.ToArray();
	}

	public static ChannelProtocol Validate(ChannelProtocol value, SessionInfo sessionInfo)
	{
		if (value == ChannelProtocol.Quic && !sessionInfo.IsQuicChannelSupported)
		{
			VhLogger.Instance.LogWarning("Quic protocol is not supported, fallback to Tcp");
			value = ChannelProtocol.Tcp;
		}
		if (value == ChannelProtocol.Udp && !sessionInfo.IsUdpChannelSupported)
		{
			VhLogger.Instance.LogWarning("Udp protocol is not supported, fallback to Tcp");
			return ChannelProtocol.Tcp;
		}
		return value;
	}
}
