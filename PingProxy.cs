using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using VpnHood.Core.PacketTransports;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Collections;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.Utils;

namespace VpnHood.Core.Tunneling.Proxies;

public class PingProxy(bool autoDisposePackets) : SinglePacketTransport(new PacketTransportOptions
{
	AutoDisposePackets = autoDisposePackets,
	Blocking = false,
	QueueCapacity = 1
}), ITimeoutItem, IDisposable
{
	private readonly Ping _ping = new Ping();

	public TimeSpan PingTimeout { get; set; } = TunnelDefaults.PingTimeout;

	public DateTime LastUsedTime { get; set; }

	public new bool IsDisposed => base.IsDisposed;

	public void Cancel()
	{
		_ping.SendAsyncCancel();
	}

	protected override ValueTask SendPacketAsync(IpPacket ipPacket)
	{
		if (ipPacket.Version != IpVersion.IPv4)
		{
			return SendIpV6((IpV6Packet)ipPacket);
		}
		return SendIpV4((IpV4Packet)ipPacket);
	}

	private async ValueTask SendIpV4(IpV4Packet ipPacket)
	{
		if (ipPacket == null)
		{
			throw new ArgumentNullException("ipPacket");
		}
		if (ipPacket.Protocol != IpProtocol.IcmpV4)
		{
			throw new InvalidOperationException($"Packet is not {IpProtocol.IcmpV4}! Packet: {PacketLogger.Format(ipPacket)}");
		}
		IcmpV4Packet icmpPacket = ipPacket.ExtractIcmpV4();
		if (icmpPacket.Type != IcmpV4Type.EchoRequest)
		{
			throw new InvalidOperationException($"The icmp is not {IcmpV4Type.EchoRequest}! Packet: {PacketLogger.Format(ipPacket)}");
		}
		bool dontFragment = (ipPacket.FragmentFlags & 2) != 0;
		PingOptions options = new PingOptions(ipPacket.TimeToLive - 1, dontFragment);
		PingReply pingReply = await _ping.SendPingAsync(ipPacket.DestinationAddress, (int)PingTimeout.TotalMilliseconds, icmpPacket.Payload.ToArray(), options).Vhc();
		if (pingReply.Status != IPStatus.Success)
		{
			throw new Exception($"Ping Reply has been failed! Status: {pingReply.Status}. Packet: {PacketLogger.Format(ipPacket)}");
		}
		IpPacket ipPacket2 = PacketBuilder.BuildIcmpV4EchoReply(ipPacket.DestinationAddress, ipPacket.SourceAddress, pingReply.Buffer, icmpPacket.Identifier, icmpPacket.SequenceNumber, updateChecksum: false);
		ipPacket2.UpdateAllChecksums();
		OnPacketReceived(ipPacket2);
	}

	private async ValueTask SendIpV6(IpV6Packet ipPacket)
	{
		IcmpV6Packet icmpPacket = ipPacket.ExtractIcmpV6();
		if (icmpPacket.Type != IcmpV6Type.EchoRequest)
		{
			throw new InvalidOperationException($"The icmp is not {IcmpV6Type.EchoRequest}! Packet: {PacketLogger.Format(ipPacket)}");
		}
		PingOptions options = new PingOptions(ipPacket.TimeToLive - 1, dontFragment: true);
		PingReply pingReply = await _ping.SendPingAsync(ipPacket.DestinationAddress, (int)PingTimeout.TotalMilliseconds, icmpPacket.Payload.ToArray(), options).Vhc();
		if (pingReply.Status != IPStatus.Success)
		{
			throw new Exception($"Ping Reply has been failed. Status: {pingReply.Status}. Packet: {PacketLogger.Format(ipPacket)}");
		}
		IpPacket ipPacket2 = PacketBuilder.BuildIcmpV6EchoReply(ipPacket.DestinationAddress, ipPacket.SourceAddress, pingReply.Buffer, icmpPacket.Identifier, icmpPacket.SequenceNumber, updateChecksum: false);
		ipPacket2.UpdateAllChecksums();
		OnPacketReceived(ipPacket2);
	}

	protected override void DisposeManaged()
	{
		_ping.Dispose();
		base.DisposeManaged();
	}
}
