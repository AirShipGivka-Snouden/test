using System;
using System.IO;
using Ciphra.VPN.Common.Storage;
using Nethereum.Signer;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Serilog;

namespace Ciphra.VPN.Common.Services.OnChain;

public class OnChainKeyStore
{
	private class KeyData
	{
		public byte[]? X25519PrivateKey { get; set; }

		public byte[]? X25519PublicKey { get; set; }

		public byte[]? Secp256k1PrivateKey { get; set; }

		public string? EoaAddress { get; set; }

		public string? SmartAccountAddress { get; set; }

		public string? CachedIpnsName { get; set; }

		public byte[]? IpnsCachedForDeviceId { get; set; }

		public byte[]? IpnsCachedForPubKey { get; set; }
	}

	private readonly string _storePath;

	private readonly object _lock = new object();

	private KeyData? _cached;

	public string? SmartAccountAddress
	{
		get
		{
			lock (_lock)
			{
				return Load().SmartAccountAddress;
			}
		}
		set
		{
			lock (_lock)
			{
				KeyData keyData = Load();
				keyData.SmartAccountAddress = value;
				Save(keyData);
			}
		}
	}

	public bool HasKeys
	{
		get
		{
			lock (_lock)
			{
				KeyData keyData = Load();
				return keyData.X25519PrivateKey != null && keyData.Secp256k1PrivateKey != null;
			}
		}
	}

	public OnChainKeyStore(string appDataFolder)
	{
		_storePath = Path.Combine(appDataFolder, "onchain_keys.enc");
	}

	public (byte[] PrivateKey, byte[] PublicKey) GetOrCreateX25519Keypair()
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		lock (_lock)
		{
			KeyData keyData = Load();
			if (keyData.X25519PrivateKey != null && keyData.X25519PublicKey != null)
			{
				return (PrivateKey: keyData.X25519PrivateKey, PublicKey: keyData.X25519PublicKey);
			}
			Log.Information("Generating new X25519 keypair for on-chain credentials.");
			X25519KeyPairGenerator val = new X25519KeyPairGenerator();
			val.Init((KeyGenerationParameters)new X25519KeyGenerationParameters(new SecureRandom()));
			AsymmetricCipherKeyPair val2 = val.GenerateKeyPair();
			keyData.X25519PrivateKey = ((X25519PrivateKeyParameters)val2.Private).GetEncoded();
			keyData.X25519PublicKey = ((X25519PublicKeyParameters)val2.Public).GetEncoded();
			Save(keyData);
			return (PrivateKey: keyData.X25519PrivateKey, PublicKey: keyData.X25519PublicKey);
		}
	}

	public (byte[] PrivateKey, string Address) GetOrCreateSecp256k1Keypair()
	{
		lock (_lock)
		{
			KeyData keyData = Load();
			if (keyData.Secp256k1PrivateKey != null && keyData.EoaAddress != null)
			{
				return (PrivateKey: keyData.Secp256k1PrivateKey, Address: keyData.EoaAddress);
			}
			Log.Information("Generating new secp256k1 keypair for on-chain credentials.");
			EthECKey val = EthECKey.GenerateKey();
			keyData.Secp256k1PrivateKey = val.GetPrivateKeyAsBytes();
			keyData.EoaAddress = val.GetPublicAddress();
			Save(keyData);
			return (PrivateKey: keyData.Secp256k1PrivateKey, Address: keyData.EoaAddress);
		}
	}

	public string? GetCachedIpnsName(byte[] encryptedDeviceId, byte[] currentX25519Pub)
	{
		lock (_lock)
		{
			KeyData keyData = Load();
			if (string.IsNullOrEmpty(keyData.CachedIpnsName))
			{
				return null;
			}
			if (keyData.IpnsCachedForDeviceId == null || keyData.IpnsCachedForPubKey == null)
			{
				return null;
			}
			if (!((ReadOnlySpan<byte>)keyData.IpnsCachedForDeviceId.AsSpan()).SequenceEqual((ReadOnlySpan<byte>)encryptedDeviceId))
			{
				return null;
			}
			if (!((ReadOnlySpan<byte>)keyData.IpnsCachedForPubKey.AsSpan()).SequenceEqual((ReadOnlySpan<byte>)currentX25519Pub))
			{
				return null;
			}
			return keyData.CachedIpnsName;
		}
	}

	public void CacheIpnsName(string ipnsName, byte[] encryptedDeviceId, byte[] x25519Pub)
	{
		if (string.IsNullOrEmpty(ipnsName))
		{
			return;
		}
		lock (_lock)
		{
			KeyData keyData = Load();
			keyData.CachedIpnsName = ipnsName;
			keyData.IpnsCachedForDeviceId = (byte[])encryptedDeviceId.Clone();
			keyData.IpnsCachedForPubKey = (byte[])x25519Pub.Clone();
			Save(keyData);
		}
	}

	public void InvalidateIpnsCache()
	{
		lock (_lock)
		{
			KeyData keyData = Load();
			if (keyData.CachedIpnsName != null || keyData.IpnsCachedForDeviceId != null || keyData.IpnsCachedForPubKey != null)
			{
				keyData.CachedIpnsName = null;
				keyData.IpnsCachedForDeviceId = null;
				keyData.IpnsCachedForPubKey = null;
				Save(keyData);
			}
		}
	}

	public void ResetX25519Keypair()
	{
		lock (_lock)
		{
			KeyData keyData = Load();
			keyData.X25519PrivateKey = null;
			keyData.X25519PublicKey = null;
			keyData.CachedIpnsName = null;
			keyData.IpnsCachedForDeviceId = null;
			keyData.IpnsCachedForPubKey = null;
			Save(keyData);
		}
	}

	public void ClearAll()
	{
		lock (_lock)
		{
			_cached = null;
			try
			{
				File.Delete(_storePath);
			}
			catch
			{
			}
		}
	}

	private KeyData Load()
	{
		if (_cached != null)
		{
			return _cached;
		}
		_cached = EncryptedJsonStore.Read<KeyData>(_storePath) ?? new KeyData();
		return _cached;
	}

	private void Save(KeyData data)
	{
		_cached = data;
		EncryptedJsonStore.Write(_storePath, data);
	}
}
