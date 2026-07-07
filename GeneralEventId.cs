using Microsoft.Extensions.Logging;

namespace VpnHood.Core.Tunneling;

public static class GeneralEventId
{
	private enum EventCode
	{
		Essential = 10,
		Session,
		Sni,
		Nat,
		Ping,
		Dns,
		Packet,
		Tcp,
		Udp,
		UdpSign,
		StreamChannel,
		PacketChannel,
		Track,
		AccessManager,
		NetProtect,
		NetFilter,
		SessionTrack,
		Request,
		Stream,
		AcmeChallenge,
		Test
	}

	public static EventId Nat = new EventId(13, "Nat");

	public static EventId Session = new EventId(11, "Session");

	public static EventId Request = new EventId(27, "Request");

	public static EventId Essential = new EventId(10, "Essential");

	public static EventId ProxyChannel = new EventId(20, "ProxyChannel");

	public static EventId PacketChannel = new EventId(21, "PacketChannel");

	public static EventId Stream = new EventId(28, "Stream");

	public static EventId Test = new EventId(30, "Test");

	public static EventId UdpSign = new EventId(19, "UdpSign");

	public static EventId Packet = new EventId(16, "Packet");

	public static EventId Sni = new EventId(12, "Sni");

	public static EventId Ping = new EventId(14, "Ping");

	public static EventId Dns = new EventId(15, "Dns");

	public static EventId Tcp = new EventId(17, "Tcp");

	public static EventId Udp = new EventId(18, "Udp");

	public static EventId AccessManager = new EventId(23, "AccessManager");

	public static EventId NetProtect = new EventId(24, "NetProtect");

	public static EventId NetFilter = new EventId(25, "NetFilter");

	public static EventId AcmeChallenge = new EventId(29, "AcmeChallenge");

	public static EventId SessionTrack = new EventId(26, "SessionTrack");

	public static EventId Track = new EventId(22, "Track");
}
