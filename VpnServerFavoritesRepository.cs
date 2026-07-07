using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Storage;

namespace Ciphra.VPN.Common.Services;

public class VpnServerFavoritesRepository : IDisposable
{
	private readonly string _filePath;

	private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

	private static readonly ConcurrentDictionary<string, VpnServerIdDto> MemoryCache = new ConcurrentDictionary<string, VpnServerIdDto>();

	public VpnServerFavoritesRepository(string filePath)
	{
		_filePath = filePath ?? throw new ArgumentNullException("filePath");
	}

	public bool AddFavorite(string serverId)
	{
		if (string.IsNullOrEmpty(serverId))
		{
			throw new ArgumentException("Server ID cannot be null or empty.", "serverId");
		}
		if (MemoryCache.ContainsKey(serverId))
		{
			return false;
		}
		Semaphore.Wait();
		try
		{
			if (MemoryCache.ContainsKey(serverId))
			{
				return false;
			}
			MemoryCache.TryAdd(serverId, new VpnServerIdDto
			{
				ServerId = serverId
			});
			PersistUnderLock();
			return true;
		}
		finally
		{
			Semaphore.Release();
		}
	}

	public bool RemoveFavorite(string serverId)
	{
		if (string.IsNullOrEmpty(serverId))
		{
			throw new ArgumentException("Server ID cannot be null or empty.", "serverId");
		}
		Semaphore.Wait();
		try
		{
			VpnServerIdDto value;
			bool flag = MemoryCache.TryRemove(serverId, out value);
			if (flag)
			{
				PersistUnderLock();
			}
			return flag;
		}
		finally
		{
			Semaphore.Release();
		}
	}

	public List<string> GetFavoriteServerIds()
	{
		if (MemoryCache.IsEmpty)
		{
			LoadFavoritesFromFile();
		}
		return (from dto in MemoryCache.Values
			where !string.IsNullOrEmpty(dto.ServerId)
			select dto.ServerId).ToList();
	}

	public bool IsFavorite(string serverId)
	{
		if (string.IsNullOrEmpty(serverId))
		{
			return false;
		}
		if (MemoryCache.ContainsKey(serverId))
		{
			return true;
		}
		if (MemoryCache.IsEmpty)
		{
			LoadFavoritesFromFile();
			return MemoryCache.ContainsKey(serverId);
		}
		return false;
	}

	public void ClearAllFavorites()
	{
		Semaphore.Wait();
		try
		{
			MemoryCache.Clear();
			EncryptedJsonStore.Write(_filePath, new List<VpnServerIdDto>());
		}
		finally
		{
			Semaphore.Release();
		}
	}

	public int GetFavoritesCount()
	{
		if (MemoryCache.IsEmpty)
		{
			LoadFavoritesFromFile();
		}
		return MemoryCache.Count;
	}

	private void LoadFavoritesFromFile()
	{
		Semaphore.Wait();
		try
		{
			List<VpnServerIdDto> list = EncryptedJsonStore.Read<List<VpnServerIdDto>>(_filePath);
			if (list == null)
			{
				return;
			}
			foreach (VpnServerIdDto item in list)
			{
				if (!string.IsNullOrEmpty(item.ServerId))
				{
					MemoryCache.TryAdd(item.ServerId, item);
				}
			}
		}
		finally
		{
			Semaphore.Release();
		}
	}

	private void PersistUnderLock()
	{
		EncryptedJsonStore.Write(_filePath, MemoryCache.Values.ToList());
	}

	public void Dispose()
	{
	}
}
