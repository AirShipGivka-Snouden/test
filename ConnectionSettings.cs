using VpnHood.Core.Common.Messaging;

namespace Ciphra.VPN.Common.Models;

public record ConnectionSettings(bool DropQuic, bool DropUdp, ChannelProtocol ChannelProtocol);
