using System;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.PacketTransports;

public class PacketTransportStat
{
	public int SentPackets { get; set; }

	public int ReceivedPackets { get; set; }

	public int DroppedPackets { get; set; }

	public int SentBytes { get; set; }

	public int ReceivedBytes { get; set; }

	public DateTime CreatedTime { get; set; } = FastDateTime.Now;

	public DateTime LastSentTime { get; set; } = FastDateTime.Now;

	public DateTime LastReceivedTime { get; set; } = FastDateTime.Now;

	public DateTime LastActivityTime
	{
		get
		{
			if (!(LastReceivedTime > LastSentTime))
			{
				return LastSentTime;
			}
			return LastReceivedTime;
		}
	}
}
