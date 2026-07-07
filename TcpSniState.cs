using System;

namespace VpnHood.Core.Filtering.DomainFiltering.SniExtractors.Tcp;

internal sealed class TcpSniState
{
	public byte[] Buffer { get; set; } = Array.Empty<byte>();

	public int BufferLength { get; set; }

	public int PacketBudget { get; set; } = 3;

	public long DeadlineTicks { get; set; }

	public int MaxBytes { get; set; } = 16384;
}
