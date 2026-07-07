using System;
using VpnHood.Core.Client.Abstractions;
using VpnHood.Core.Common.Messaging;

namespace Ciphra.VPN.Common.Models;

public class ConnectionStateDto
{
	public ClientState? ClientState { get; }

	public DateTime? CreatedTime { get; }

	public Exception? Error { get; }

	public long SpeedSent { get; }

	public long SpeedReceived { get; }

	public long TrafficSent { get; }

	public long TrafficReceived { get; }

	public long MaxTraffic { get; }

	public long CycleTrafficSent { get; }

	public long CycleTrafficReceived { get; }

	public DateTime? SessionExpirationTime { get; }

	public int? ActiveClientCount { get; }

	public ChannelProtocol? ActiveChannelProtocol { get; }

	public bool? IsUdpChannelSupported { get; }

	public static ConnectionStateDto None()
	{
		return new ConnectionStateDto(VpnHood.Core.Client.Abstractions.ClientState.None, null, null, 0L, 0L, 0L, 0L, 0L, 0L, 0L);
	}

	public ConnectionStateDto(ClientState? clientState, DateTime? createdTime, Exception? error = null, long speedSent = 0L, long speedReceived = 0L, long trafficSent = 0L, long trafficReceived = 0L, long maxTraffic = 0L, long cycleTrafficSent = 0L, long cycleTrafficReceived = 0L, DateTime? sessionExpirationTime = null, int? activeClientCount = null, ChannelProtocol? activeChannelProtocol = null, bool? isUdpChannelSupported = null)
	{
		ClientState = clientState.GetValueOrDefault();
		CreatedTime = createdTime;
		Error = error;
		SpeedSent = speedSent;
		SpeedReceived = speedReceived;
		TrafficSent = trafficSent;
		TrafficReceived = trafficReceived;
		MaxTraffic = maxTraffic;
		CycleTrafficSent = cycleTrafficSent;
		CycleTrafficReceived = cycleTrafficReceived;
		SessionExpirationTime = sessionExpirationTime;
		ActiveClientCount = activeClientCount;
		ActiveChannelProtocol = activeChannelProtocol;
		IsUdpChannelSupported = isUdpChannelSupported;
	}
}
