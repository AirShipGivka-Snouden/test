using System;
using System.Security.Cryptography;
using System.Text;

namespace Ciphra.VPN.Common.Services.OnChain;

public static class DeviceIdCrypto
{
	public static byte[] Transform(byte[] input)
	{
		if (input.Length > 16)
		{
			throw new ArgumentException("Input must be at most 16 bytes.", "input");
		}
		using Aes aes = Aes.Create();
		aes.Key = OnChainConfig.DeviceIdEncryptionKey;
		aes.Mode = CipherMode.ECB;
		aes.Padding = PaddingMode.None;
		using ICryptoTransform cryptoTransform = aes.CreateEncryptor();
		byte[] inputBuffer = new byte[16];
		byte[] array = new byte[16];
		cryptoTransform.TransformBlock(inputBuffer, 0, 16, array, 0);
		byte[] array2 = new byte[input.Length];
		for (int i = 0; i < input.Length; i++)
		{
			array2[i] = (byte)(input[i] ^ array[i]);
		}
		return array2;
	}

	public static byte[] EncryptDeviceId(string deviceIdString)
	{
		byte[] array = SHA256.HashData(Encoding.UTF8.GetBytes(deviceIdString));
		return Transform(array[..16]);
	}
}
