using System;

namespace VpnHood.Core.Filtering.DomainFiltering.SniExtractors.Tcp;

internal static class TcpSniExtractor
{
	private static readonly long TimeoutTicks300Ms = TimeSpan.FromMilliseconds(300L).Ticks;

	public static PacketSniResult TryExtractSniFromTcpPayload(ReadOnlySpan<byte> tcpPayload, object? state = null, long nowTicks = 0L)
	{
		if (nowTicks == 0L)
		{
			nowTicks = Environment.TickCount64 * 10000;
		}
		TcpSniState tcpSniState = state as TcpSniState;
		if (tcpSniState == null)
		{
			if (!LooksLikeClientHello(tcpPayload))
			{
				return PacketSniResult.NotFound;
			}
			tcpSniState = new TcpSniState
			{
				PacketBudget = 3,
				DeadlineTicks = nowTicks + TimeoutTicks300Ms,
				MaxBytes = 16384
			};
		}
		if (tcpSniState.PacketBudget <= 0 || nowTicks > tcpSniState.DeadlineTicks)
		{
			return PacketSniResult.NotFound;
		}
		tcpSniState.PacketBudget--;
		int num = tcpSniState.BufferLength + tcpPayload.Length;
		if (num > tcpSniState.MaxBytes)
		{
			return PacketSniResult.NotFound;
		}
		if (num > tcpSniState.Buffer.Length)
		{
			byte[] array = new byte[Math.Min(num * 2, tcpSniState.MaxBytes)];
			if (tcpSniState.BufferLength > 0)
			{
				Buffer.BlockCopy(tcpSniState.Buffer, 0, array, 0, tcpSniState.BufferLength);
			}
			tcpSniState.Buffer = array;
		}
		tcpPayload.CopyTo(tcpSniState.Buffer.AsSpan(tcpSniState.BufferLength));
		tcpSniState.BufferLength = num;
		Span<byte> span = tcpSniState.Buffer.AsSpan(0, tcpSniState.BufferLength);
		string text = TlsClientHelloParser.ExtractSni(span);
		if (text != null)
		{
			return PacketSniResult.Found(text);
		}
		if (HasCompleteClientHello(span))
		{
			return PacketSniResult.NotFound;
		}
		if (tcpSniState.PacketBudget <= 0 || nowTicks > tcpSniState.DeadlineTicks)
		{
			return PacketSniResult.NotFound;
		}
		return PacketSniResult.Pending(tcpSniState);
	}

	private static bool LooksLikeClientHello(ReadOnlySpan<byte> data)
	{
		if (data.Length < 9)
		{
			return false;
		}
		if (data[0] != 22)
		{
			return false;
		}
		if (data[5] != 1)
		{
			return false;
		}
		return true;
	}

	private static bool HasCompleteClientHello(ReadOnlySpan<byte> data)
	{
		if (data.Length < 5)
		{
			return false;
		}
		int num = (data[3] << 8) | data[4];
		int num2 = 5 + num;
		return data.Length >= num2;
	}
}
