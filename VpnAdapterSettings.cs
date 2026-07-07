using System;
using VpnHood.Core.PacketTransports;

namespace VpnHood.Core.VpnAdapters.Abstractions;

public class VpnAdapterSettings : PacketTransportOptions
{
	public required string AdapterName { get; init; }

	public TimeSpan MaxPacketSendDelay { get; init; } = TimeSpan.FromMilliseconds(500L);

	public bool AutoRestart { get; init; }

	public bool AutoMetric { get; init; } = true;
}
