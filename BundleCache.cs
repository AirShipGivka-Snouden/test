using System;
using System.IO;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Storage;

namespace Ciphra.VPN.Common.Services.OnChain;

public class BundleCache
{
	private class CachedBundle
	{
		public OnChainBundle? Bundle { get; set; }

		public DateTime FetchedAtUtc { get; set; }
	}

	private readonly string _cachePath;

	private readonly object _lock = new object();

	private CachedBundle? _memoryCache;

	public BundleCache(string appDataFolder)
	{
		_cachePath = Path.Combine(appDataFolder, "onchain_bundle.enc");
	}

	public void Save(OnChainBundle bundle)
	{
		lock (_lock)
		{
			EncryptedJsonStore.Write(data: _memoryCache = new CachedBundle
			{
				Bundle = bundle,
				FetchedAtUtc = DateTime.UtcNow
			}, filePath: _cachePath);
		}
	}

	public OnChainBundle? Load()
	{
		lock (_lock)
		{
			CachedBundle cachedBundle = _memoryCache ?? EncryptedJsonStore.Read<CachedBundle>(_cachePath);
			if (cachedBundle?.Bundle == null)
			{
				return null;
			}
			_memoryCache = cachedBundle;
			if (IsExpired(cachedBundle))
			{
				return null;
			}
			return cachedBundle.Bundle;
		}
	}

	public OnChainBundle? LoadEvenIfExpired()
	{
		lock (_lock)
		{
			return (_memoryCache = _memoryCache ?? EncryptedJsonStore.Read<CachedBundle>(_cachePath))?.Bundle;
		}
	}

	public bool HasValidCache()
	{
		lock (_lock)
		{
			CachedBundle cachedBundle = _memoryCache ?? EncryptedJsonStore.Read<CachedBundle>(_cachePath);
			return cachedBundle?.Bundle != null && !IsExpired(cachedBundle);
		}
	}

	public int GetTtlSeconds()
	{
		lock (_lock)
		{
			return (_memoryCache ?? EncryptedJsonStore.Read<CachedBundle>(_cachePath))?.Bundle?.BundleTtlSeconds ?? 3600;
		}
	}

	public void Clear()
	{
		lock (_lock)
		{
			_memoryCache = null;
			try
			{
				File.Delete(_cachePath);
			}
			catch
			{
			}
		}
	}

	private static bool IsExpired(CachedBundle cached)
	{
		int num = cached.Bundle?.BundleTtlSeconds ?? 3600;
		return DateTime.UtcNow - cached.FetchedAtUtc > TimeSpan.FromSeconds(num);
	}
}
