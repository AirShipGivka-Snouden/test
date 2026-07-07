using System;
using System.Collections.Generic;
using VpnHood.Core.PacketTransports;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Tunneling.Channels;

namespace VpnHood.Core.Tunneling;

public class Tunnel : PassthroughPacketTransport
{
	private readonly ChannelManager _channelManager;

	public TrafficMeter TrafficMeter { get; }

	public int PacketChannelCount => _channelManager.PacketChannelCount;

	public int StreamProxyChannelCount => _channelManager.ProxyChannelCount;

	public IReadOnlyList<IPacketChannel> PacketChannels => _channelManager.PacketChannels;

	public int MaxPacketChannelCount
	{
		get
		{
			return _channelManager.MaxPacketChannelCount;
		}
		set
		{
			_channelManager.MaxPacketChannelCount = value;
		}
	}

	public int Mtu { get; set; }

	public void RemoveChannels<T>() where T : IChannel
	{
		_channelManager.RemoveChannels<T>();
	}

	public Tunnel(TunnelOptions options)
	{
		Mtu = options.Mtu;
		TrafficMeter = new TrafficMeter();
		_channelManager = new ChannelManager(options.MaxPacketChannelCount, Channel_OnPacketReceived);
	}

	public void AddChannel(IChannel channel, bool disposeIfFailed = false)
	{
		try
		{
			_channelManager.AddChannel(channel);
		}
		catch when (disposeIfFailed)
		{
			channel.Dispose();
			throw;
		}
	}

	private void Channel_OnPacketReceived(object? sender, IpPacket ipPacket)
	{
		OnPacketReceived(ipPacket);
	}

	protected override void SendPacket(IpPacket ipPacket)
	{
		SendPacketInternal(ipPacket);
	}

	private void SendPacketInternal(IpPacket ipPacket)
	{
		IPacketChannel packetChannel = FindChannelForPacket(ipPacket);
		VerifyMtu(ipPacket, 120);
		packetChannel.SendPacketQueued(ipPacket);
	}

	private void VerifyMtu(IpPacket ipPacket, int overheadSize)
	{
		if (ipPacket.PacketLength + overheadSize <= Mtu)
		{
			return;
		}
		IpPacket ipPacket2 = PacketBuilder.BuildIcmpPacketTooBigReply(ipPacket, (ushort)Mtu);
		OnPacketReceived(ipPacket2);
		throw new Exception($"The packet length is larger than MTU. PacketLength: {ipPacket.PacketLength}, MTU: {Mtu}.");
	}

	private IPacketChannel FindChannelForPacket(IpPacket ipPacket)
	{
		IPacketChannel packetChannel = FindChannelForPacketInternal(ipPacket);
		while (packetChannel.State != PacketChannelState.Connected)
		{
			_channelManager.CleanupChannels();
			packetChannel = FindChannelForPacketInternal(ipPacket);
		}
		return packetChannel;
	}

	private IPacketChannel FindChannelForPacketInternal(IpPacket ipPacket)
	{
		int packetChannelCount = _channelManager.PacketChannelCount;
		int channelIndex = packetChannelCount switch
		{
			0 => throw new Exception("No available PacketChannel to send packets."), 
			1 => 0, 
			_ => ipPacket.Protocol switch
			{
				IpProtocol.Tcp => ipPacket.ExtractTcp().SourcePort % packetChannelCount, 
				IpProtocol.Udp => ipPacket.ExtractUdp().SourcePort % packetChannelCount, 
				_ => 0, 
			}, 
		};
		return _channelManager.GetPacketChannel(channelIndex);
	}

	protected override void DisposeManaged()
	{
		_channelManager.Dispose();
		TrafficMeter.Dispose();
		base.DisposeManaged();
	}
}
