using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace VpnHood.Core.Filtering.DomainFiltering.SniExtractors.Quic;

internal static class QuicSniExtractor
{
	private static readonly byte[] V1Salt = new byte[20]
	{
		56, 118, 44, 247, 245, 89, 52, 179, 77, 23,
		154, 230, 164, 200, 12, 173, 204, 187, 127, 10
	};

	private static readonly byte[] V2Salt = new byte[20]
	{
		13, 237, 227, 222, 247, 0, 166, 219, 129, 147,
		129, 190, 110, 38, 157, 203, 249, 189, 46, 217
	};

	private const uint V2Version = 1798521807u;

	private static readonly long TimeoutTicks300Ms = TimeSpan.FromMilliseconds(300L).Ticks;

	public static QuicSniResult TryExtractSniFromUdpPayload(ReadOnlySpan<byte> udpPayload, QuicSniState? state = null, long nowTicks = 0L)
	{
		if (nowTicks == 0L)
		{
			nowTicks = Environment.TickCount64 * 10000;
		}
		if (state == null)
		{
			if (!LooksLikeInitial(udpPayload, out var isV, out var dcid))
			{
				return new QuicSniResult
				{
					DomainName = null,
					NeedMore = false,
					State = null
				};
			}
			state = new QuicSniState
			{
				IsV2 = isV,
				Dcid = dcid.ToArray(),
				PacketBudget = 3,
				DeadlineTicks = nowTicks + TimeoutTicks300Ms,
				MaxBytes = 65536
			};
			DeriveInitialSecrets(state);
		}
		if (state.PacketBudget <= 0 || nowTicks > state.DeadlineTicks)
		{
			return new QuicSniResult
			{
				DomainName = null,
				NeedMore = false,
				State = null
			};
		}
		int i = 0;
		bool flag = false;
		uint version;
		int typeBits;
		int pnOffset;
		int headerLenUpToPn;
		ulong lengthField;
		int totalPacketBytes;
		ReadOnlySpan<byte> dcid2;
		for (; i < udpPayload.Length && IsLongHeader(udpPayload, i) && TryParseLongHeader(udpPayload, i, out version, out typeBits, out pnOffset, out headerLenUpToPn, out lengthField, out totalPacketBytes, out dcid2); i += totalPacketBytes)
		{
			bool flag2 = version == 1798521807;
			bool num = (flag2 ? (typeBits == 1) : (typeBits == 0));
			if (!state.SecretsReady)
			{
				state.IsV2 = flag2;
				state.Dcid = dcid2.ToArray();
				DeriveInitialSecrets(state);
			}
			if (num && DcidEquals(dcid2, state.Dcid))
			{
				flag = true;
				TryDecryptAndCollectCrypto(udpPayload, i, pnOffset, headerLenUpToPn, lengthField, state);
			}
			if (totalPacketBytes <= 0)
			{
				break;
			}
		}
		if (flag)
		{
			state.PacketBudget--;
		}
		byte[] array = AssembleContiguousFromZero(state.Segments, state.MaxBytes);
		if (array.Length != 0)
		{
			string text = TryParseSniFromClientHelloPartial(array);
			if (text != null)
			{
				return new QuicSniResult
				{
					DomainName = text,
					NeedMore = false,
					State = null
				};
			}
		}
		if (state.PacketBudget <= 0 || nowTicks > state.DeadlineTicks)
		{
			return new QuicSniResult
			{
				DomainName = null,
				NeedMore = false,
				State = null
			};
		}
		return new QuicSniResult
		{
			DomainName = null,
			NeedMore = true,
			State = state
		};
	}

	private static bool LooksLikeInitial(ReadOnlySpan<byte> udp, out bool isV2, out ReadOnlySpan<byte> dcid)
	{
		isV2 = false;
		dcid = default(ReadOnlySpan<byte>);
		if (udp.Length < 7)
		{
			return false;
		}
		if ((udp[0] & 0x80) == 0)
		{
			return false;
		}
		uint num = BinaryPrimitives.ReadUInt32BigEndian(udp.Slice(1, 4));
		if (num == 0)
		{
			return false;
		}
		isV2 = num == 1798521807;
		int num2 = 5;
		if (udp.Length < num2 + 1)
		{
			return false;
		}
		int num3 = udp[num2++];
		if (udp.Length < num2 + num3)
		{
			return false;
		}
		dcid = udp.Slice(num2, num3);
		num2 += num3;
		if (udp.Length < num2 + 1)
		{
			return false;
		}
		int num4 = udp[num2++];
		num2 += num4;
		if (udp.Length < num2)
		{
			return false;
		}
		if (!TryReadVarInt(udp, ref num2, out var v))
		{
			return false;
		}
		num2 += (int)v;
		if (!TryReadVarInt(udp, ref num2, out var v2))
		{
			return false;
		}
		return num2 + (int)v2 <= udp.Length;
	}

	private static bool IsLongHeader(ReadOnlySpan<byte> b, int off)
	{
		if (b.Length - off >= 1)
		{
			return (b[off] & 0x80) != 0;
		}
		return false;
	}

	private static bool TryParseLongHeader(ReadOnlySpan<byte> b, int off, out uint version, out int typeBits, out int pnOffset, out int headerLenUpToPn, out ulong lengthField, out int totalPacketBytes, out ReadOnlySpan<byte> dcid)
	{
		version = 0u;
		typeBits = 0;
		pnOffset = 0;
		headerLenUpToPn = 0;
		lengthField = 0uL;
		totalPacketBytes = 0;
		dcid = default(ReadOnlySpan<byte>);
		if (b.Length - off < 7)
		{
			return false;
		}
		byte b2 = b[off];
		typeBits = (b2 >> 4) & 3;
		version = BinaryPrimitives.ReadUInt32BigEndian(b.Slice(off + 1, 4));
		int num = off + 5;
		if (b.Length < num + 1)
		{
			return false;
		}
		int num2 = b[num++];
		if (b.Length < num + num2)
		{
			return false;
		}
		dcid = b.Slice(num, num2);
		num += num2;
		if (b.Length < num + 1)
		{
			return false;
		}
		int num3 = b[num++];
		num += num3;
		if (b.Length < num)
		{
			return false;
		}
		if ((version == 1798521807) ? (typeBits == 1) : (typeBits == 0))
		{
			if (!TryReadVarInt(b, ref num, out var v))
			{
				return false;
			}
			num += (int)v;
		}
		if (!TryReadVarInt(b, ref num, out lengthField))
		{
			return false;
		}
		pnOffset = num;
		headerLenUpToPn = pnOffset - off;
		totalPacketBytes = headerLenUpToPn + (int)lengthField;
		if (totalPacketBytes < 0 || off + totalPacketBytes > b.Length)
		{
			return false;
		}
		return true;
	}

	private static bool DcidEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
	{
		if (a.Length == b.Length)
		{
			return a.SequenceEqual(b);
		}
		return false;
	}

	private static void DeriveInitialSecrets(QuicSniState st)
	{
		byte[] salt = (st.IsV2 ? V2Salt : V1Salt);
		string label = (st.IsV2 ? "quicv2 key" : "quic key");
		string label2 = (st.IsV2 ? "quicv2 iv" : "quic iv");
		string label3 = (st.IsV2 ? "quicv2 hp" : "quic hp");
		byte[] secret = HkdfExpandLabel(HkdfExtract(salt, st.Dcid), "client in", 32);
		st.Key = HkdfExpandLabel(secret, label, 16);
		st.Iv = HkdfExpandLabel(secret, label2, 12);
		st.Hp = HkdfExpandLabel(secret, label3, 16);
		st.SecretsReady = true;
	}

	private static void TryDecryptAndCollectCrypto(ReadOnlySpan<byte> b, int off, int pnOffset, int headerLenUpToPn, ulong lengthField, QuicSniState st)
	{
		int num = pnOffset + 4;
		if (b.Length < num + 16)
		{
			return;
		}
		Span<byte> destination = stackalloc byte[16];
		Span<byte> destination2 = stackalloc byte[16];
		b.Slice(num, 16).CopyTo(destination);
		using (Aes aes = Aes.Create())
		{
			aes.Mode = CipherMode.ECB;
			aes.Padding = PaddingMode.None;
			aes.Key = st.Hp;
			using ICryptoTransform cryptoTransform = aes.CreateEncryptor();
			byte[] array = ArrayPool<byte>.Shared.Rent(16);
			byte[] array2 = ArrayPool<byte>.Shared.Rent(16);
			try
			{
				destination.CopyTo(array);
				cryptoTransform.TransformBlock(array, 0, 16, array2, 0);
				array2.AsSpan(0, 16).CopyTo(destination2);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(array);
				ArrayPool<byte>.Shared.Return(array2);
			}
		}
		byte b2 = (byte)(b[off] ^ (destination2[0] & 0xF));
		int num2 = (b2 & 3) + 1;
		if (num2 < 1 || num2 > 4 || b.Length < pnOffset + num2)
		{
			return;
		}
		Span<byte> span = stackalloc byte[4];
		for (int i = 0; i < num2; i++)
		{
			span[i] = (byte)(b[pnOffset + i] ^ destination2[1 + i]);
		}
		int num3 = headerLenUpToPn + num2;
		Span<byte> span2 = ((num3 > 128) ? ((Span<byte>)new byte[num3]) : stackalloc byte[num3]);
		Span<byte> span3 = span2;
		b.Slice(off, headerLenUpToPn).CopyTo(span3);
		span3[0] = b2;
		for (int j = 0; j < num2; j++)
		{
			span3[headerLenUpToPn + j] = span[j];
		}
		ulong num4 = 0uL;
		for (int k = 0; k < num2; k++)
		{
			num4 = (num4 << 8) | span[k];
		}
		Span<byte> span4 = stackalloc byte[12];
		st.Iv.CopyTo(span4);
		for (int l = 0; l < 8; l++)
		{
			span4[span4.Length - 1 - l] ^= (byte)(num4 >> 8 * l);
		}
		int num5 = pnOffset + num2;
		int num6 = (int)lengthField - num2;
		if (num6 <= 16 || b.Length < num5 + num6)
		{
			return;
		}
		int num7 = num6 - 16;
		byte[] array3 = ArrayPool<byte>.Shared.Rent(num7);
		byte[] array4 = ArrayPool<byte>.Shared.Rent(16);
		byte[] array5 = ArrayPool<byte>.Shared.Rent(num7);
		try
		{
			b.Slice(num5, num7).CopyTo(array3);
			b.Slice(num5 + num7, 16).CopyTo(array4);
			using AesGcm aesGcm = new AesGcm(st.Key, 16);
			aesGcm.Decrypt(span4, array3.AsSpan(0, num7), array4.AsSpan(0, 16), array5.AsSpan(0, num7), span3);
			CollectCryptoSegments(array5.AsSpan(0, num7), st.Segments);
		}
		catch
		{
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array3);
			ArrayPool<byte>.Shared.Return(array4);
			ArrayPool<byte>.Shared.Return(array5);
		}
	}

	private static void CollectCryptoSegments(ReadOnlySpan<byte> plain, List<(ulong Off, byte[] Data)> segs)
	{
		int p = 0;
		while (p < plain.Length)
		{
			byte b = plain[p++];
			switch (b)
			{
			case 2:
			case 3:
				if (!SkipAckFrame(plain, ref p, b == 3))
				{
					return;
				}
				break;
			case 6:
			{
				if (!TryReadVarInt(plain, ref p, out var v) || !TryReadVarInt(plain, ref p, out var v2) || p + (int)v2 > plain.Length)
				{
					return;
				}
				byte[] item = plain.Slice(p, (int)v2).ToArray();
				segs.Add((v, item));
				p += (int)v2;
				break;
			}
			default:
				return;
			case 0:
			case 1:
				break;
			}
		}
	}

	private static bool SkipAckFrame(ReadOnlySpan<byte> s, ref int p, bool withEcn)
	{
		if (!TryReadVarInt(s, ref p, out var v))
		{
			return false;
		}
		if (!TryReadVarInt(s, ref p, out v))
		{
			return false;
		}
		if (!TryReadVarInt(s, ref p, out var v2))
		{
			return false;
		}
		if (!TryReadVarInt(s, ref p, out v))
		{
			return false;
		}
		for (ulong num = 0uL; num < v2; num++)
		{
			if (!TryReadVarInt(s, ref p, out v))
			{
				return false;
			}
			if (!TryReadVarInt(s, ref p, out v))
			{
				return false;
			}
		}
		if (withEcn)
		{
			if (!TryReadVarInt(s, ref p, out v))
			{
				return false;
			}
			if (!TryReadVarInt(s, ref p, out v))
			{
				return false;
			}
			if (!TryReadVarInt(s, ref p, out v))
			{
				return false;
			}
		}
		return true;
	}

	private static byte[] AssembleContiguousFromZero(List<(ulong Off, byte[] Data)> segments, int maxBytes)
	{
		if (segments.Count == 0)
		{
			return Array.Empty<byte>();
		}
		segments.Sort(((ulong Off, byte[] Data) a, (ulong Off, byte[] Data) b) => a.Off.CompareTo(b.Off));
		ulong num = 0uL;
		ulong num2 = 0uL;
		foreach (var segment in segments)
		{
			if (segment.Off > num2)
			{
				break;
			}
			int num3 = (int)Math.Max(0L, (long)(num2 - segment.Off));
			int num4 = Math.Min(segment.Data.Length - num3, maxBytes - (int)num2);
			if (num4 > 0)
			{
				num = num2 + (ulong)num4;
				num2 = num;
				if (num >= (ulong)maxBytes)
				{
					break;
				}
			}
		}
		if (num == 0L)
		{
			return Array.Empty<byte>();
		}
		byte[] array = new byte[num];
		num2 = 0uL;
		foreach (var segment2 in segments)
		{
			if (segment2.Off > num2)
			{
				break;
			}
			int num5 = (int)Math.Max(0L, (long)(num2 - segment2.Off));
			int num6 = Math.Min(segment2.Data.Length - num5, (int)(num - num2));
			if (num6 > 0)
			{
				Buffer.BlockCopy(segment2.Data, num5, array, (int)num2, num6);
				num2 += (ulong)num6;
				if (num2 >= num)
				{
					break;
				}
			}
		}
		return array;
	}

	private static string? TryParseSniFromClientHelloPartial(ReadOnlySpan<byte> buf)
	{
		if (buf.Length < 4 || buf[0] != 1)
		{
			return null;
		}
		int num = (buf[1] << 16) | (buf[2] << 8) | buf[3];
		if (4 + num > buf.Length)
		{
			return null;
		}
		int num2 = 4;
		if (num2 + 2 + 32 > buf.Length)
		{
			return null;
		}
		num2 += 34;
		if (num2 + 1 > buf.Length)
		{
			return null;
		}
		int num3 = buf[num2++];
		if (num2 + num3 > buf.Length)
		{
			return null;
		}
		num2 += num3;
		if (num2 + 2 > buf.Length)
		{
			return null;
		}
		int num4 = (buf[num2] << 8) | buf[num2 + 1];
		num2 += 2;
		if (num2 + num4 > buf.Length)
		{
			return null;
		}
		num2 += num4;
		if (num2 + 1 > buf.Length)
		{
			return null;
		}
		int num5 = buf[num2++];
		if (num2 + num5 > buf.Length)
		{
			return null;
		}
		num2 += num5;
		if (num2 + 2 > buf.Length)
		{
			return null;
		}
		int num6 = (buf[num2] << 8) | buf[num2 + 1];
		num2 += 2;
		int num7 = num2 + num6;
		if (num7 > buf.Length)
		{
			return null;
		}
		while (num2 + 4 <= num7)
		{
			int num8 = (buf[num2] << 8) | buf[num2 + 1];
			num2 += 2;
			int num9 = (buf[num2] << 8) | buf[num2 + 1];
			num2 += 2;
			if (num2 + num9 > num7)
			{
				return null;
			}
			if (num8 == 0)
			{
				int num10 = num2;
				if (num10 + 2 > num2 + num9)
				{
					return null;
				}
				int num11 = (buf[num10] << 8) | buf[num10 + 1];
				num10 += 2;
				if (num10 + num11 > num2 + num9)
				{
					return null;
				}
				while (num10 + 3 <= num2 + num9)
				{
					byte b = buf[num10++];
					int num12 = (buf[num10] << 8) | buf[num10 + 1];
					num10 += 2;
					if (num10 + num12 > num2 + num9)
					{
						return null;
					}
					if (b == 0)
					{
						return Encoding.ASCII.GetString(buf.Slice(num10, num12));
					}
					num10 += num12;
				}
				return null;
			}
			num2 += num9;
		}
		return null;
	}

	private static bool TryReadVarInt(ReadOnlySpan<byte> buf, ref int p, out ulong v)
	{
		v = 0uL;
		if (p >= buf.Length)
		{
			return false;
		}
		byte b = buf[p];
		int num = b >> 6;
		int num2 = 1 << num;
		if (p + num2 > buf.Length)
		{
			return false;
		}
		switch (num2)
		{
		case 1:
			v = (ulong)(b & 0x3F);
			p++;
			return true;
		case 2:
			v = (ulong)(BinaryPrimitives.ReadUInt16BigEndian(buf.Slice(p, 2)) & 0x3FFF);
			p += 2;
			return true;
		case 4:
			v = BinaryPrimitives.ReadUInt32BigEndian(buf.Slice(p, 4)) & 0x3FFFFFFF;
			p += 4;
			return true;
		case 8:
			v = BinaryPrimitives.ReadUInt64BigEndian(buf.Slice(p, 8)) & 0x3FFFFFFFFFFFFFFFL;
			p += 8;
			return true;
		default:
			return false;
		}
	}

	private static byte[] HkdfExtract(byte[] salt, ReadOnlySpan<byte> ikm)
	{
		using HMACSHA256 hMACSHA = new HMACSHA256(salt);
		byte[] array = ArrayPool<byte>.Shared.Rent(ikm.Length);
		try
		{
			ikm.CopyTo(array);
			return hMACSHA.ComputeHash(array, 0, ikm.Length);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	private static byte[] HkdfExpand(byte[] prk, ReadOnlySpan<byte> info, int len)
	{
		using HMACSHA256 hMACSHA = new HMACSHA256(prk);
		byte[] array = new byte[len];
		int num = 0;
		Span<byte> span = default(Span<byte>);
		byte b = 1;
		byte[] array2 = ArrayPool<byte>.Shared.Rent(32 + info.Length + 1);
		try
		{
			while (num < len)
			{
				int count = span.Length + info.Length + 1;
				span.CopyTo(array2);
				info.CopyTo(array2.AsSpan(span.Length));
				array2[span.Length + info.Length] = b++;
				byte[] array3 = hMACSHA.ComputeHash(array2, 0, count);
				int num2 = Math.Min(array3.Length, len - num);
				Buffer.BlockCopy(array3, 0, array, num, num2);
				num += num2;
				span = array3;
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array2);
		}
		return array;
	}

	private static byte[] HkdfExpandLabel(byte[] secret, string label, int len)
	{
		string s = "tls13 " + label;
		byte[] bytes = Encoding.ASCII.GetBytes(s);
		Span<byte> span = stackalloc byte[3 + bytes.Length + 1];
		BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)len);
		span[2] = (byte)bytes.Length;
		bytes.CopyTo(span.Slice(3));
		span[span.Length - 1] = 0;
		return HkdfExpand(secret, span, len);
	}
}
