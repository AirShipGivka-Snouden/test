using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.Cryptography;
using VpnHood.Core.Tunneling.Utils;

namespace VpnHood.Core.Tunneling.Channels;

public abstract class UdpChannelTransmitter : IDisposable
{
	private const int VersionOffset = 0;

	private const int VersionLength = 1;

	private const int SessionIdOffset = 1;

	private const int SessionIdLength = 8;

	private const int SeqOffset = 9;

	private const int SeqLength = 8;

	private const int TagOffset = 17;

	public const int TagLength = 16;

	private readonly EventReporter _udpSignReporter = new EventReporter("Invalid udp signature.", GeneralEventId.UdpSign);

	private readonly EventReporter _invalidSessionReporter = new EventReporter("Invalid UDP session.", GeneralEventId.UdpSign);

	private readonly Memory<byte> _sendBuffer = new byte[1500];

	private readonly UdpClient _udpClient;

	private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);

	private static ulong _sendSequenceNumber;

	private bool _disposed;

	private bool _isSendBufferSizeCustomized;

	private bool _isReceivedBufferSizeCustomized;

	public const int HeaderLength = 33;

	public int MaxPacketSize { get; set; } = 1500;

	public IPEndPoint LocalEndPoint { get; }

	public bool Connected => !_disposed;

	public TransferBufferSize? BufferSize
	{
		get
		{
			return new TransferBufferSize(_udpClient.Client.SendBufferSize, _udpClient.Client.ReceiveBufferSize);
		}
		set
		{
			using UdpClient udpClient = new UdpClient(_udpClient.Client.AddressFamily);
			if (value.HasValue && value.GetValueOrDefault().Send > 0)
			{
				_isSendBufferSizeCustomized = true;
				_udpClient.Client.SendBufferSize = value.Value.Send;
			}
			else if (_isSendBufferSizeCustomized)
			{
				_udpClient.Client.SendBufferSize = udpClient.Client.SendBufferSize;
			}
			if (value.HasValue && value.GetValueOrDefault().Receive > 0)
			{
				_isReceivedBufferSizeCustomized = true;
				_udpClient.Client.ReceiveBufferSize = value.Value.Receive;
			}
			else if (_isReceivedBufferSizeCustomized)
			{
				_udpClient.Client.ReceiveBufferSize = udpClient.Client.ReceiveBufferSize;
			}
		}
	}

	protected abstract SessionUdpTransport? SessionIdToUdpTransport(ulong sessionId);

	protected UdpChannelTransmitter(UdpClient udpClient)
	{
		_udpClient = udpClient;
		LocalEndPoint = udpClient.Client.GetLocalEndPoint();
		if (_sendSequenceNumber == 0L)
		{
			Span<byte> obj = stackalloc byte[8];
			RandomNumberGenerator.Fill(obj);
			_sendSequenceNumber = BinaryPrimitives.ReadUInt64LittleEndian(obj);
		}
		Task.Run((Func<Task?>)ReadLoopAsync);
	}

	private static void BuildNonce(Span<byte> nonce, ulong sessionId, ulong seq)
	{
		BinaryPrimitives.WriteUInt64LittleEndian(nonce.Slice(0, 8), seq);
		uint value = (uint)(sessionId ^ (sessionId >> 32));
		BinaryPrimitives.WriteUInt32LittleEndian(nonce.Slice(8, 4), value);
	}

	internal async Task SendAsync(ulong sessionId, ReadOnlyMemory<byte> payload, IPEndPoint ipEndPoint, ICryptor cryptor)
	{
		if (payload.Length + 33 > MaxPacketSize)
		{
			throw new ArgumentOutOfRangeException("payload");
		}
		try
		{
			await _sendSemaphore.WaitAsync().Vhc();
			await SendCoreAsync(sessionId, ipEndPoint, payload, cryptor);
		}
		catch (Exception ex) when (SocketUtils.IsInvalidUdpStateException(ex))
		{
			VhLogger.Instance.LogError(GeneralEventId.Essential, ex, "UdpChannelTransmitter: Socket is in invalid state. Disposing the transmitter. DataLength: {DataLength}, DestinationIp: {DestinationIp}", payload.Length, VhLogger.Format(ipEndPoint));
			Dispose();
			throw;
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogError(GeneralEventId.Udp, exception, "UdpChannelTransmitter: Could not send data. DataLength: {DataLength}, DestinationIp: {DestinationIp}", payload.Length, VhLogger.Format(ipEndPoint));
			throw;
		}
		finally
		{
			_sendSemaphore.Release();
		}
	}

	private async Task SendCoreAsync(ulong sessionId, IPEndPoint ipEndPoint, ReadOnlyMemory<byte> payload, ICryptor cryptor)
	{
		ulong num = _sendSequenceNumber++;
		Span<byte> span = _sendBuffer.Span;
		BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(1, 8), sessionId);
		BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(9, 8), num);
		span[0] = 0;
		Span<byte> tag = span.Slice(17, 16);
		tag.Clear();
		Span<byte> cipherText = span.Slice(33, payload.Length);
		Span<byte> span2 = span.Slice(1, 16);
		Span<byte> span3 = stackalloc byte[12];
		BuildNonce(span3, sessionId, num);
		cryptor.Encrypt(span3, payload.Span, cipherText, tag, span2);
		int totalLength = 33 + payload.Length;
		UdpClient udpClient = _udpClient;
		Memory<byte> sendBuffer = _sendBuffer;
		int num2 = await udpClient.SendAsync(sendBuffer.Slice(0, totalLength), ipEndPoint).Vhc();
		if (num2 != totalLength)
		{
			throw new Exception($"UdpClient: Sent {num2} bytes instead of {totalLength} bytes.");
		}
	}

	private async Task ReadLoopAsync()
	{
		byte[] nonce = new byte[12];
		byte[] plainTextBuffer = new byte[MaxPacketSize];
		while (!_disposed)
		{
			try
			{
				UdpReceiveResult udpReceiveResult = await _udpClient.ReceiveAsync().Vhc();
				byte[] buffer = udpReceiveResult.Buffer;
				if (buffer.Length < 33)
				{
					_udpSignReporter.Raise();
					continue;
				}
				Span<byte> span = buffer.AsSpan();
				ulong sessionId = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(1, 8));
				ulong seq = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(9, 8));
				SessionUdpTransport sessionUdpTransport = SessionIdToUdpTransport(sessionId);
				if (sessionUdpTransport == null)
				{
					_invalidSessionReporter.Raise();
					continue;
				}
				if (sessionUdpTransport.IsServer)
				{
					sessionUdpTransport.RemoteEndPoint = udpReceiveResult.RemoteEndPoint;
				}
				_ = ref span[0];
				Span<byte> span2 = span.Slice(17, 16);
				Span<byte> span3 = span.Slice(33);
				Span<byte> span4 = span.Slice(1, 16);
				BuildNonce(nonce, sessionId, seq);
				int length = span3.Length;
				if (length > plainTextBuffer.Length)
				{
					throw new InvalidOperationException($"Receive buffer too small. length={length}, buffer={plainTextBuffer.Length}");
				}
				Span<byte> plainText = plainTextBuffer.AsSpan(0, length);
				sessionUdpTransport.ReceiveCryptor.Decrypt(nonce, span3, span2, plainText, span4);
				Memory<byte> obj = udpReceiveResult.Buffer.AsMemory(0, length);
				plainText.CopyTo(obj.Span);
				sessionUdpTransport.DataReceived?.Invoke(obj);
			}
			catch (Exception) when (_disposed)
			{
				break;
			}
			catch (Exception ex2) when (SocketUtils.IsInvalidUdpStateException(ex2))
			{
				VhLogger.Instance.LogError(GeneralEventId.Essential, ex2, "UdpChannelTransmitter: Read loop crashed.");
				Dispose();
				break;
			}
			catch (Exception)
			{
				_udpSignReporter.Raise();
			}
		}
	}

	public virtual void Dispose()
	{
		if (!_disposed)
		{
			VhLogger.Instance.LogInformation(GeneralEventId.Essential, "UdpChannelTransmitter: Disposing the transmitter. LocalEndPoint: {LocalEndPoint}", VhLogger.Format(_udpClient.Client.LocalEndPoint));
			_disposed = true;
			_udpClient.Dispose();
			_sendSemaphore.Dispose();
			_invalidSessionReporter.Dispose();
			_udpSignReporter.Dispose();
		}
	}
}
