using System.Collections.Generic;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services.Interfaces;
using VpnHood.Core.Common.Messaging;

namespace Ciphra.VPN.Common.Services;

public class SmartHealService : ISmartHealService
{
	public IReadOnlyList<SmartHealCandidate> Candidates { get; } = new global::_003C_003Ez__ReadOnlyArray<SmartHealCandidate>(new SmartHealCandidate[5]
	{
		new SmartHealCandidate(new ConnectionSettings(DropQuic: false, DropUdp: false, ChannelProtocol.Udp), "Switch to UDP", "Protocol set to UDP"),
		new SmartHealCandidate(new ConnectionSettings(DropQuic: true, DropUdp: false, ChannelProtocol.Udp), "UDP + Drop QUIC", "Protocol set to UDP, QUIC blocking enabled"),
		new SmartHealCandidate(new ConnectionSettings(DropQuic: true, DropUdp: false, ChannelProtocol.Tcp), "Drop QUIC", "QUIC traffic blocking enabled"),
		new SmartHealCandidate(new ConnectionSettings(DropQuic: true, DropUdp: true, ChannelProtocol.Tcp), "Force TCP only", "QUIC and UDP blocking enabled, protocol set to TCP"),
		new SmartHealCandidate(new ConnectionSettings(DropQuic: false, DropUdp: true, ChannelProtocol.Tcp), "Drop UDP", "UDP blocking enabled")
	});
}
