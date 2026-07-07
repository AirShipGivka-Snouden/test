using System;
using System.Collections.Generic;

namespace VpnHood.Core.Filtering.DomainFiltering.SniExtractors.Quic;

internal sealed class QuicSniState
{
	public bool IsV2;

	public bool SecretsReady;

	public byte[] Dcid = Array.Empty<byte>();

	public byte[] Key = Array.Empty<byte>();

	public byte[] Iv = Array.Empty<byte>();

	public byte[] Hp = Array.Empty<byte>();

	public List<(ulong Off, byte[] Data)> Segments = new List<(ulong, byte[])>();

	public int PacketBudget = 3;

	public long DeadlineTicks;

	public int MaxBytes = 65536;
}
