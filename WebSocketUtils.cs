using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VpnHood.Core.Tunneling.WebSockets;

public static class WebSocketUtils
{
	public static Span<byte> BuildWebSocketFrameHeader(Span<byte> buffer, long payloadLength, bool closeConnection = false)
	{
		if (buffer.Length < 10)
		{
			throw new ArgumentException("Buffer too small, must be at least 10 bytes.", "buffer");
		}
		byte b = (byte)(0x80 | (closeConnection ? 8 : 2));
		buffer[0] = b;
		int length;
		if (payloadLength > 125)
		{
			if (payloadLength <= 65535)
			{
				buffer[1] = 126;
				buffer[2] = (byte)((payloadLength >> 8) & 0xFF);
				buffer[3] = (byte)(payloadLength & 0xFF);
				length = 4;
			}
			else
			{
				buffer[1] = 127;
				buffer[2] = (byte)((payloadLength >> 56) & 0xFF);
				buffer[3] = (byte)((payloadLength >> 48) & 0xFF);
				buffer[4] = (byte)((payloadLength >> 40) & 0xFF);
				buffer[5] = (byte)((payloadLength >> 32) & 0xFF);
				buffer[6] = (byte)((payloadLength >> 24) & 0xFF);
				buffer[7] = (byte)((payloadLength >> 16) & 0xFF);
				buffer[8] = (byte)((payloadLength >> 8) & 0xFF);
				buffer[9] = (byte)(payloadLength & 0xFF);
				length = 10;
			}
		}
		else
		{
			buffer[1] = (byte)payloadLength;
			length = 2;
		}
		return buffer.Slice(0, length);
	}

	public static Span<byte> BuildWebSocketFrameHeader(Span<byte> buffer, long payloadLength, ReadOnlySpan<byte> maskKey, bool closeConnection = false)
	{
		if (buffer.Length < 14)
		{
			throw new ArgumentException("Buffer too small, must be at least 14 bytes.", "buffer");
		}
		if (maskKey.Length != 4)
		{
			throw new ArgumentException("Mask key must be 4 bytes.", "maskKey");
		}
		byte b = (byte)(0x80 | (closeConnection ? 8 : 2));
		buffer[0] = b;
		int num;
		if (payloadLength > 125)
		{
			if (payloadLength <= 65535)
			{
				buffer[1] = 254;
				buffer[2] = (byte)((payloadLength >> 8) & 0xFF);
				buffer[3] = (byte)(payloadLength & 0xFF);
				num = 4;
			}
			else
			{
				buffer[1] = byte.MaxValue;
				buffer[2] = (byte)((payloadLength >> 56) & 0xFF);
				buffer[3] = (byte)((payloadLength >> 48) & 0xFF);
				buffer[4] = (byte)((payloadLength >> 40) & 0xFF);
				buffer[5] = (byte)((payloadLength >> 32) & 0xFF);
				buffer[6] = (byte)((payloadLength >> 24) & 0xFF);
				buffer[7] = (byte)((payloadLength >> 16) & 0xFF);
				buffer[8] = (byte)((payloadLength >> 8) & 0xFF);
				buffer[9] = (byte)(payloadLength & 0xFF);
				num = 10;
			}
		}
		else
		{
			buffer[1] = (byte)(0x80 | (byte)payloadLength);
			num = 2;
		}
		maskKey.CopyTo(buffer.Slice(num, 4));
		return buffer[..(num + 4)];
	}

	public static async Task<WebSocketHeader> ReadWebSocketHeader(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		if (buffer.Length < 14)
		{
			throw new ArgumentException("Buffer must be at least 14 bytes.", "buffer");
		}
		await stream.ReadExactlyAsync(buffer.Slice(0, 2), cancellationToken);
		byte b = buffer.Span[0];
		byte b2 = buffer.Span[1];
		byte opcode = (byte)(b & 0xF);
		bool isMasked = (b2 & 0x80) != 0;
		byte b3 = (byte)(b2 & 0x7F);
		byte headerLength = 2;
		long payloadLength;
		if (b3 > 125)
		{
			if (b3 == 126)
			{
				await stream.ReadExactlyAsync(buffer.Slice(2, 2), cancellationToken);
				ReadOnlySpan<byte> readOnlySpan = buffer.Span;
				payloadLength = (readOnlySpan[2] << 8) | readOnlySpan[3];
				headerLength += 2;
			}
			else
			{
				await stream.ReadExactlyAsync(buffer.Slice(2, 8), cancellationToken);
				ReadOnlySpan<byte> readOnlySpan = buffer.Span;
				payloadLength = (long)(((ulong)readOnlySpan[2] << 56) | ((ulong)readOnlySpan[3] << 48) | ((ulong)readOnlySpan[4] << 40) | ((ulong)readOnlySpan[5] << 32) | ((ulong)readOnlySpan[6] << 24) | ((ulong)readOnlySpan[7] << 16) | ((ulong)readOnlySpan[8] << 8) | readOnlySpan[9]);
				headerLength += 8;
			}
		}
		else
		{
			payloadLength = b3;
		}
		Memory<byte> maskKey = Memory<byte>.Empty;
		if (isMasked)
		{
			maskKey = buffer.Slice(headerLength, 4);
			await stream.ReadExactlyAsync(maskKey, cancellationToken);
		}
		return new WebSocketHeader
		{
			IsBinary = (opcode == 2),
			IsText = (opcode == 1),
			IsPing = (opcode == 9),
			IsPong = (opcode == 10),
			IsCloseConnection = (opcode == 8),
			PayloadLength = payloadLength,
			MaskKey = maskKey
		};
	}

	public static string ComputeWebSocketAccept(string secWebSocketKey)
	{
		string s = secWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
		return Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(s)));
	}
}
