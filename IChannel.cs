using System;
using VpnHood.Core.Common.Messaging;

namespace VpnHood.Core.Tunneling.Channels;

public interface IChannel : IDisposable
{
	string ChannelId { get; }

	DateTime LastActivityTime { get; }

	Traffic Traffic { get; }

	PacketChannelState State { get; }

	void Start();
}
