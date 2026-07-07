using System;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Parameters;

namespace Ciphra.VPN.Common.Services.OnChain;

public static class BundleDecryptor
{
	private const int EphemeralPubSize = 32;

	private const int NonceSize = 12;

	private const int GcmTagSize = 16;

	public static string Decrypt(byte[] encryptedBundle, byte[] x25519PrivateKey)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		if (encryptedBundle.Length < 60)
		{
			throw new CryptographicException("Encrypted bundle too short.");
		}
		Span<byte> span = encryptedBundle.AsSpan(0, 32);
		Span<byte> span2 = encryptedBundle.AsSpan(32, 12);
		Span<byte> span3 = encryptedBundle.AsSpan(44);
		X25519PrivateKeyParameters val = new X25519PrivateKeyParameters(x25519PrivateKey, 0);
		X25519PublicKeyParameters val2 = new X25519PublicKeyParameters((ReadOnlySpan<byte>)span);
		X25519Agreement val3 = new X25519Agreement();
		val3.Init((ICipherParameters)(object)val);
		byte[] array = new byte[val3.AgreementSize];
		val3.CalculateAgreement((ICipherParameters)(object)val2, array, 0);
		byte[] array2 = null;
		try
		{
			array2 = HKDF.DeriveKey(HashAlgorithmName.SHA256, array, 32, new byte[32], Encoding.UTF8.GetBytes("ciphra-bundle-v1"));
			Span<byte> span4 = span3.Slice(0, span3.Length - 16);
			Span<byte> span5 = span3.Slice(span3.Length - 16);
			byte[] array3 = new byte[span4.Length];
			using AesGcm aesGcm = new AesGcm(array2, 16);
			aesGcm.Decrypt(span2, span4, span5, array3);
			return Encoding.UTF8.GetString(array3);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(array);
			if (array2 != null)
			{
				CryptographicOperations.ZeroMemory(array2);
			}
		}
	}

	public static string DecryptIpnsName(string encryptedHex, byte[] x25519PrivateKey)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		string s = (encryptedHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? encryptedHex.Substring(2) : encryptedHex);
		byte[] array = Convert.FromHexString(s);
		if (array.Length < 60)
		{
			throw new CryptographicException("Encrypted IPNS name too short.");
		}
		Span<byte> span = array.AsSpan(0, 32);
		Span<byte> span2 = array.AsSpan(32, 12);
		Span<byte> span3 = array.AsSpan(44);
		X25519PrivateKeyParameters val = new X25519PrivateKeyParameters(x25519PrivateKey, 0);
		X25519PublicKeyParameters val2 = new X25519PublicKeyParameters((ReadOnlySpan<byte>)span);
		X25519Agreement val3 = new X25519Agreement();
		val3.Init((ICipherParameters)(object)val);
		byte[] array2 = new byte[val3.AgreementSize];
		val3.CalculateAgreement((ICipherParameters)(object)val2, array2, 0);
		byte[] array3 = null;
		try
		{
			array3 = HKDF.DeriveKey(HashAlgorithmName.SHA256, array2, 32, new byte[32], Encoding.UTF8.GetBytes("ciphra-ipns-v1"));
			Span<byte> span4 = span3.Slice(0, span3.Length - 16);
			Span<byte> span5 = span3.Slice(span3.Length - 16);
			byte[] array4 = new byte[span4.Length];
			using AesGcm aesGcm = new AesGcm(array3, 16);
			aesGcm.Decrypt(span2, span4, span5, array4);
			return Encoding.UTF8.GetString(array4);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(array2);
			if (array3 != null)
			{
				CryptographicOperations.ZeroMemory(array3);
			}
		}
	}
}
