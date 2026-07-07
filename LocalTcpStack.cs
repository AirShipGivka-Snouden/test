using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.TcpStack.Abstractions;
using VpnHood.Core.TcpStack.Primitives;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.TcpStack;

public sealed class LocalTcpStack : ITcpStack, IDisposable
{
	private const ushort LoopbackWindowSize = ushort.MaxValue;

	private readonly ConcurrentDictionary<IPEndPointPairValue, LocalTcpConnection> _connections = new ConcurrentDictionary<IPEndPointPairValue, LocalTcpConnection>();

	private readonly ConcurrentDictionary<IpEndPointValue, LocalTcpListener> _listeners = new ConcurrentDictionary<IpEndPointValue, LocalTcpListener>();

	private readonly Lock _anyListenerLock = new Lock();

	private LocalTcpListener? _anyListener;

	private bool _disposed;

	public bool VerboseLogging { get; set; }

	public Action<IpPacket>? OnPacketSend { get; set; }

	public LocalTcpListener Listen(IpEndPointValue localEndPoint)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		return _listeners.GetOrAdd(localEndPoint, (IpEndPointValue ep) => new LocalTcpListener(this, ep));
	}

	public LocalTcpListener ListenAny()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		using (_anyListenerLock.EnterScope())
		{
			return _anyListener ?? (_anyListener = new LocalTcpListener(this, null));
		}
	}

	ITcpListener ITcpStack.ListenAny()
	{
		return ListenAny();
	}

	ITcpListener ITcpStack.Listen(IPEndPoint localEndPoint)
	{
		return Listen(localEndPoint.ToValue());
	}

	public bool StopListening(IPEndPoint localEndPoint)
	{
		return StopListening(localEndPoint.ToValue());
	}

	public bool StopListening(IpEndPointValue localEndPoint)
	{
		LocalTcpListener value;
		return _listeners.TryRemove(localEndPoint, out value);
	}

	internal bool StopListeningAny()
	{
		using (_anyListenerLock.EnterScope())
		{
			if (_anyListener == null)
			{
				return false;
			}
			_anyListener = null;
			return true;
		}
	}

	public void ProcessIncoming(IpPacket ipPacket)
	{
		if (_disposed)
		{
			return;
		}
		try
		{
			ProcessIncomingInternal(ipPacket);
		}
		catch
		{
		}
	}

	private void ProcessIncomingInternal(IpPacket ipPacket)
	{
		if (ipPacket.Protocol == IpProtocol.Tcp)
		{
			TcpPacket tcpPacket = ipPacket.ExtractTcp();
			IPEndPointPairValue iPEndPointPairValue = new IPEndPointPairValue(new IpEndPointValue(ipPacket.SourceAddress, tcpPacket.SourcePort), new IpEndPointValue(ipPacket.DestinationAddress, tcpPacket.DestinationPort));
			LocalTcpConnection value;
			if (tcpPacket != null && tcpPacket.Synchronize && !tcpPacket.Acknowledgment)
			{
				HandleSynPacket(iPEndPointPairValue, tcpPacket);
			}
			else if (_connections.TryGetValue(iPEndPointPairValue, out value))
			{
				HandleExistingConnection(value, tcpPacket);
			}
			else if (!tcpPacket.Reset)
			{
				SendRst(iPEndPointPairValue.Destination, iPEndPointPairValue.Source, tcpPacket);
			}
		}
	}

	private void HandleSynPacket(IPEndPointPairValue ipEndPointPair, TcpPacket tcpPacket)
	{
		LocalTcpListener localTcpListener = ResolveListener(ipEndPointPair.Destination);
		if (localTcpListener == null)
		{
			SendRst(ipEndPointPair.Destination, ipEndPointPair.Source, tcpPacket);
			return;
		}
		if (_connections.TryGetValue(ipEndPointPair, out LocalTcpConnection value))
		{
			if (value.State == TcpConnectionState.SynReceived)
			{
				SendSynAck(value);
			}
			return;
		}
		uint @int = (uint)RandomNumberGenerator.GetInt32(int.MaxValue);
		ushort? peerMss = ParseMssOption(tcpPacket.Options.Span);
		byte peerWsShift = ParseWindowScaleOption(tcpPacket.Options.Span);
		LocalTcpConnection localTcpConnection = new LocalTcpConnection(ipEndPointPair, @int, tcpPacket.SequenceNumber, peerMss, localTcpListener, peerWsShift);
		localTcpConnection.OnClosed += OnConnectionClosed;
		if (!_connections.TryAdd(ipEndPointPair, localTcpConnection))
		{
			localTcpConnection.Dispose();
			return;
		}
		SendSynAck(localTcpConnection);
		localTcpConnection.Start(this);
	}

	private void SendSynAck(LocalTcpConnection conn)
	{
		IpPacket ipPacket = PacketBuilder.BuildTcp(options: new byte[8] { 2, 4, 5, 180, 1, 3, 3, 0 }, sourceEndPoint: conn.IpEndPointPair.Destination, destinationEndPoint: conn.IpEndPointPair.Source, payload: ReadOnlySpan<byte>.Empty);
		TcpPacket tcpPacket = ipPacket.ExtractTcp();
		conn.SetSndNxtAfterSyn();
		uint item = conn.SnapshotSequence().rcvNxt;
		tcpPacket.SequenceNumber = conn.IsnLocal;
		tcpPacket.AcknowledgmentNumber = item;
		tcpPacket.Synchronize = true;
		tcpPacket.Acknowledgment = true;
		tcpPacket.WindowSize = ushort.MaxValue;
		SendPacket(ipPacket);
	}

	private void HandleExistingConnection(LocalTcpConnection conn, TcpPacket tcpPacket)
	{
		if (conn.State == TcpConnectionState.SynReceived && tcpPacket.Acknowledgment)
		{
			conn.MarkEstablished();
		}
		TcpFlags tcpFlags = TcpFlags.None;
		if (tcpPacket.Finish)
		{
			tcpFlags |= TcpFlags.Fin;
		}
		if (tcpPacket.Reset)
		{
			tcpFlags |= TcpFlags.Rst;
		}
		if (tcpPacket.Acknowledgment)
		{
			tcpFlags |= TcpFlags.Ack;
		}
		if (tcpPacket.Push)
		{
			tcpFlags |= TcpFlags.Psh;
		}
		var (flag, flag2) = conn.TryHandleIncoming(tcpPacket.SequenceNumber, tcpPacket.AcknowledgmentNumber, tcpPacket.WindowSize, tcpFlags, tcpPacket.Payload.Span);
		if (flag && flag2)
		{
			SendAckOnly(conn);
		}
	}

	internal void SendAckOnly(LocalTcpConnection conn)
	{
		IpPacket ipPacket = PacketBuilder.BuildTcp(conn.IpEndPointPair.Destination, conn.IpEndPointPair.Source, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty);
		TcpPacket tcpPacket = ipPacket.ExtractTcp();
		(uint sndNxt, uint rcvNxt) tuple = conn.SnapshotSequence();
		uint item = tuple.sndNxt;
		uint item2 = tuple.rcvNxt;
		tcpPacket.SequenceNumber = item;
		tcpPacket.AcknowledgmentNumber = item2;
		tcpPacket.Acknowledgment = true;
		tcpPacket.WindowSize = ushort.MaxValue;
		SendPacket(ipPacket);
	}

	private void SendRst(IpEndPointValue localEndPoint, IpEndPointValue remoteEndPoint, TcpPacket incomingTcp)
	{
		IpPacket ipPacket = PacketBuilder.BuildTcp(localEndPoint, remoteEndPoint, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty);
		TcpPacket tcpPacket = ipPacket.ExtractTcp();
		tcpPacket.Reset = true;
		if (incomingTcp.Acknowledgment)
		{
			tcpPacket.SequenceNumber = incomingTcp.AcknowledgmentNumber;
		}
		else
		{
			tcpPacket.SequenceNumber = 0u;
			uint num = incomingTcp.SequenceNumber + (uint)incomingTcp.Payload.Length;
			if (incomingTcp.Synchronize)
			{
				num++;
			}
			if (incomingTcp.Finish)
			{
				num++;
			}
			tcpPacket.AcknowledgmentNumber = num;
			tcpPacket.Acknowledgment = true;
		}
		SendPacket(ipPacket);
	}

	private void OnConnectionClosed(LocalTcpConnection conn)
	{
		_connections.TryRemove(conn.IpEndPointPair, out LocalTcpConnection _);
	}

	public void DropAllConnections()
	{
		foreach (KeyValuePair<IPEndPointPairValue, LocalTcpConnection> connection in _connections)
		{
			if (_connections.TryRemove(connection.Key, out LocalTcpConnection value))
			{
				value.Dispose();
			}
		}
	}

	internal void SendPacket(IpPacket packet)
	{
		Action<IpPacket> onPacketSend = OnPacketSend;
		if (onPacketSend == null)
		{
			packet.Dispose();
			return;
		}
		try
		{
			packet.UpdateAllChecksums();
			onPacketSend(packet);
		}
		catch
		{
			try
			{
				packet.Dispose();
			}
			catch
			{
			}
			throw;
		}
	}

	private LocalTcpListener? ResolveListener(IpEndPointValue endPoint)
	{
		if (_listeners.TryGetValue(endPoint, out LocalTcpListener value))
		{
			return value;
		}
		return _anyListener;
	}

	private static ushort? ParseMssOption(ReadOnlySpan<byte> options)
	{
		int num = 0;
		while (num < options.Length)
		{
			byte b = options[num];
			switch (b)
			{
			case 0:
				return null;
			case 1:
				num++;
				continue;
			}
			if (num + 1 >= options.Length)
			{
				return null;
			}
			byte b2 = options[num + 1];
			if (b2 < 2 || num + b2 > options.Length)
			{
				return null;
			}
			if (b == 2 && b2 == 4)
			{
				return (ushort)((options[num + 2] << 8) | options[num + 3]);
			}
			num += b2;
		}
		return null;
	}

	private static byte ParseWindowScaleOption(ReadOnlySpan<byte> options)
	{
		int num = 0;
		while (num < options.Length)
		{
			byte b = options[num];
			switch (b)
			{
			case 0:
				return 0;
			case 1:
				num++;
				continue;
			}
			if (num + 1 >= options.Length)
			{
				return 0;
			}
			byte b2 = options[num + 1];
			if (b2 < 2 || num + b2 > options.Length)
			{
				return 0;
			}
			if (b == 3 && b2 == 3)
			{
				return options[num + 2];
			}
			num += b2;
		}
		return 0;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		foreach (KeyValuePair<IpEndPointValue, LocalTcpListener> listener in _listeners)
		{
			if (_listeners.TryRemove(listener.Key, out LocalTcpListener value))
			{
				value.Dispose();
			}
		}
		LocalTcpListener anyListener;
		using (_anyListenerLock.EnterScope())
		{
			anyListener = _anyListener;
			_anyListener = null;
		}
		anyListener?.Dispose();
		foreach (KeyValuePair<IPEndPointPairValue, LocalTcpConnection> connection in _connections)
		{
			if (_connections.TryRemove(connection.Key, out LocalTcpConnection value2))
			{
				value2.Dispose();
			}
		}
	}
}
