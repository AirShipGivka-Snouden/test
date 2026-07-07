using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Toolkit.Streams;

public static class StreamUtils
{
	public const int MaxMessageLength = 524288;

	public static T ReadObject<T>(Stream stream, int maxLength = 524288)
	{
		Span<byte> span = stackalloc byte[4];
		stream.ReadExactly(span);
		int num = BinaryPrimitives.ReadInt32LittleEndian(span);
		if (num == 0)
		{
			throw new Exception("json length is zero!");
		}
		if (num > maxLength)
		{
			throw new FormatException($"json length for {typeof(T)} is too big! It should be less than {maxLength} bytes but it was {num} bytes");
		}
		Memory<byte> memory = new byte[num].AsMemory();
		stream.ReadExactly(memory.Span);
		T val = JsonSerializer.Deserialize<T>(memory.Span);
		if (val == null)
		{
			throw new Exception("Could not read Message!");
		}
		return val;
	}

	public static Task<T> ReadObjectAsync<T>(Stream stream, CancellationToken cancellationToken)
	{
		return ReadObjectAsync<T>(stream, 524288, cancellationToken);
	}

	public static async Task<T> ReadObjectAsync<T>(Stream stream, int maxLength, CancellationToken cancellationToken)
	{
		T val = JsonSerializer.Deserialize<T>(await ReadMessageAsync(stream, maxLength, cancellationToken).Vhc());
		if (val == null)
		{
			throw new Exception("Could not read Message!");
		}
		return val;
	}

	public static Task<string> ReadMessageAsync(Stream stream, CancellationToken cancellationToken)
	{
		return ReadMessageAsync(stream, 524288, cancellationToken);
	}

	public static async Task<string> ReadMessageAsync(Stream stream, int maxLength, CancellationToken cancellationToken)
	{
		Memory<byte> lengthBuffer = new byte[4].AsMemory();
		await stream.ReadExactlyAsync(lengthBuffer, cancellationToken);
		if (((ReadOnlySpan<byte>)lengthBuffer.Span).SequenceEqual("HTTP"u8))
		{
			throw new UnauthorizedAccessException("Stream returned an HTTP response.");
		}
		int num = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer.Span);
		if (num == 0)
		{
			throw new Exception("json length is zero!");
		}
		if (num > maxLength)
		{
			throw new FormatException($"json length is too big! It should be less than {maxLength} bytes but it was {num} bytes");
		}
		Memory<byte> buffer = new byte[num].AsMemory();
		await stream.ReadExactlyAsync(buffer, cancellationToken).Vhc();
		return Encoding.UTF8.GetString(buffer.Span);
	}

	public static Memory<byte> ObjectToJsonBuffer(object obj)
	{
		byte[] array = JsonSerializer.SerializeToUtf8Bytes(obj);
		Memory<byte> result = new byte[4 + array.Length];
		BinaryPrimitives.WriteInt32LittleEndian(result.Span.Slice(0, 4), array.Length);
		array.CopyTo(result.Slice(4));
		return result;
	}

	public static void WriteObject(Stream stream, object obj)
	{
		stream.Write(ObjectToJsonBuffer(obj).Span);
	}

	public static ValueTask WriteObjectAsync(Stream stream, object obj, CancellationToken cancellationToken)
	{
		return stream.WriteAsync(ObjectToJsonBuffer(obj), cancellationToken);
	}
}
