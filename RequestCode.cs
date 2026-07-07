namespace VpnHood.Core.Tunneling.Messaging;

public enum RequestCode : byte
{
	Hello = 1,
	TcpPacketChannel = 2,
	ProxyChannel = 3,
	SessionStatus = 4,
	UdpPacket = 5,
	RewardedAd = 10,
	ServerStatus = 20,
	ServerCheck = 30,
	Bye = 50
}
