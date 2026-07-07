using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Packets;

public abstract class IpPacket(Memory<byte> buffer) : IDisposable
{
	private bool _disposed;

	protected IPAddress? SourceAddressField;

	protected IPAddress? DestinationAddressField;

	[CompilerGenerated]
	private IPayloadPacket? _003CPayloadPacket_003Ek__BackingField;

	public Memory<byte> Buffer
	{
		get
		{
			if (!_disposed)
			{
				return buffer;
			}
			throw new ObjectDisposedException("IpPacket");
		}
	}

	public int PacketLength => buffer.Length;

	public IPayloadPacket? PayloadPacket
	{
		[CompilerGenerated]
		get
		{
			return _003CPayloadPacket_003Ek__BackingField;
		}
		set
		{
			if (value != null && !value.Buffer.Equals(Payload))
			{
				throw new InvalidOperationException("The PayloadPacket buffer must match the packet buffer.");
			}
			_003CPayloadPacket_003Ek__BackingField = value;
		}
	}

	public IpVersion Version
	{
		get
		{
			return (IpVersion)(Buffer.Span[0] >> 4);
		}
		protected set
		{
			Memory<byte> memory = Buffer;
			ref byte reference = ref memory.Span[0];
			uint num = (uint)value << 4;
			memory = Buffer;
			reference = (byte)(num | (uint)(memory.Span[0] & 0xF));
		}
	}

	protected abstract Span<byte> SourceAddressBuffer { get; set; }

	protected abstract Span<byte> DestinationAddressBuffer { get; set; }

	public ReadOnlySpan<byte> SourceAddressSpan
	{
		get
		{
			return SourceAddressBuffer;
		}
		set
		{
			if (!value.TryCopyTo(SourceAddressBuffer))
			{
				throw new ArgumentException("Invalid IP address format.");
			}
			SourceAddressBuffer = null;
		}
	}

	public ReadOnlySpan<byte> DestinationAddressSpan
	{
		get
		{
			return DestinationAddressBuffer;
		}
		set
		{
			if (!value.TryCopyTo(DestinationAddressBuffer))
			{
				throw new ArgumentException("Invalid IP address format.");
			}
			DestinationAddressBuffer = null;
		}
	}

	public IPAddress SourceAddress
	{
		get
		{
			return SourceAddressField ?? (SourceAddressField = new IPAddress(SourceAddressSpan));
		}
		set
		{
			if (!value.TryWriteBytes(SourceAddressBuffer, out var bytesWritten) || bytesWritten != SourceAddressSpan.Length)
			{
				throw new ArgumentException("Invalid IP address format.");
			}
			SourceAddressField = value;
		}
	}

	public IPAddress DestinationAddress
	{
		get
		{
			return DestinationAddressField ?? (DestinationAddressField = new IPAddress(DestinationAddressSpan));
		}
		set
		{
			if (!value.TryWriteBytes(DestinationAddressBuffer, out var bytesWritten) || bytesWritten != DestinationAddressSpan.Length)
			{
				throw new ArgumentException("Invalid IP address format.");
			}
			DestinationAddressField = value;
		}
	}

	public abstract IpProtocol Protocol { get; }

	public abstract byte TimeToLive { get; set; }

	public abstract Memory<byte> Header { get; }

	public Memory<byte> Payload => Buffer.Slice(Header.Length);

	public static IpVersion GetPacketVersion(ReadOnlySpan<byte> buffer)
	{
		return (IpVersion)(buffer[0] >> 4);
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(20, 3, stringBuilder2);
		handler.AppendLiteral("Src=");
		handler.AppendFormatted(SourceAddress);
		handler.AppendLiteral(", Dst=");
		handler.AppendFormatted(DestinationAddress);
		handler.AppendLiteral(", Proto=");
		handler.AppendFormatted(Protocol);
		handler.AppendLiteral(", ");
		stringBuilder3.Append(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(25, 2, stringBuilder2);
		handler.AppendLiteral("TotalLength:");
		handler.AppendFormatted(Buffer.Length);
		handler.AppendLiteral(", PayloadLen=");
		handler.AppendFormatted(Payload.Length);
		stringBuilder4.Append(ref handler);
		if (PayloadPacket != null)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(19, 2, stringBuilder2);
			handler.AppendLiteral(", PayloadPacket: ");
			handler.AppendFormatted(PayloadPacket.GetType().Name);
			handler.AppendLiteral(", ");
			handler.AppendFormatted(PayloadPacket);
			stringBuilder5.Append(ref handler);
		}
		return stringBuilder.ToString();
	}

	protected virtual void Dispose(bool disposing)
	{
		_disposed = true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	~IpPacket()
	{
		Dispose(disposing: false);
	}
}
