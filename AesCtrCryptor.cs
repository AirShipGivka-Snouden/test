using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace VpnHood.Core.Tunneling.Cryptography;

internal sealed class AesCtrCryptor : ICryptor, IDisposable
{
	private readonly Aes _aes;

	private readonly ICryptoTransform _encryptor;

	public AesCtrCryptor(ReadOnlySpan<byte> key)
	{
		_aes = Aes.Create();
		_aes.Mode = CipherMode.ECB;
		_aes.Padding = PaddingMode.None;
		_aes.Key = key.ToArray();
		_encryptor = _aes.CreateEncryptor();
	}

	public void Encrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plainText, Span<byte> cipherText, Span<byte> tag, ReadOnlySpan<byte> associatedData)
	{
		Transform(nonce, plainText, cipherText);
	}

	public void Decrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> cipherText, ReadOnlySpan<byte> tag, Span<byte> plainText, ReadOnlySpan<byte> associatedData)
	{
		Transform(nonce, cipherText, plainText);
	}

	private void Transform(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> input, Span<byte> output)
	{
		if (nonce.Length != 12)
		{
			throw new ArgumentOutOfRangeException("nonce", "Nonce must be 12 bytes.");
		}
		byte[] array = new byte[16];
		byte[] array2 = new byte[16];
		nonce.CopyTo(array);
		int i = 0;
		uint num = 0u;
		int num3;
		for (; i < input.Length; i += num3)
		{
			BinaryPrimitives.WriteUInt32LittleEndian(array.AsSpan(12, 4), num++);
			_encryptor.TransformBlock(array, 0, 16, array2, 0);
			int num2 = input.Length - i;
			num3 = ((num2 > 16) ? 16 : num2);
			for (int j = 0; j < num3; j++)
			{
				output[i + j] = (byte)(input[i + j] ^ array2[j]);
			}
		}
	}

	public void Dispose()
	{
		_encryptor.Dispose();
		_aes.Dispose();
	}
}
