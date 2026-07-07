using System;
using System.Net;
using System.Threading.Tasks;
using VpnHood.Core.Tunneling.Cryptography;

namespace VpnHood.Core.Tunneling.Channels;

public class SessionUdpTransport(UdpChannelTransmitter channelTransmitter, ulong sessionId, ReadOnlySpan<byte> key, IPEndPoint? remoteEndPoint, bool isServer) : IUdpTransport, IDisposable
{
	public bool IsServer { get; } = isServer;

	public IPEndPoint? RemoteEndPoint { get; set; } = remoteEndPoint;

	private ICryptor SendCryptor { get; } = new AesGcmCryptor(key, 16);

	internal ICryptor ReceiveCryptor { get; } = new AesGcmCryptor(key, 16);

	public UdpChannelTransmitter ChannelTransmitter { get; set; } = channelTransmitter;

	public Action<Memory<byte>>? DataReceived { get; set; }

	public int OverheadLength => 120;

	public bool Connected => ChannelTransmitter.Connected;

	public Task SendAsync(ReadOnlyMemory<byte> buffer)
	{
		if (RemoteEndPoint == null)
		{
			throw new InvalidOperationException("RemoteEndPoint is not set.");
		}
		return ChannelTransmitter.SendAsync(sessionId, buffer, RemoteEndPoint, SendCryptor);
	}

	public void Dispose()
	{
		SendCryptor.Dispose();
		ReceiveCryptor.Dispose();
	}
}
