using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Storage;

namespace Ciphra.VPN.Common.Services;

public class VpnServersCache : IDisposable
{
	private readonly string _filePath;

	private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

	private readonly ConcurrentDictionary<string, VpnServerDto> MemoryCache = new ConcurrentDictionary<string, VpnServerDto>();

	public VpnServersCache(string filePath)
	{
		_filePath = filePath ?? throw new ArgumentNullException("filePath");
	}

	public void CacheServers(List<VpnServerDto> servers)
	{
		if (servers == null)
		{
			throw new ArgumentNullException("servers");
		}
		Semaphore.Wait();
		try
		{
			List<VpnServerDto> list = servers.Where((VpnServerDto s) => s.Id != null).ToList();
			foreach (VpnServerDto server in list)
			{
				MemoryCache.AddOrUpdate(server.Id, server, (string id, VpnServerDto existing) => server);
			}
			PersistUnderLock();
		}
		finally
		{
			Semaphore.Release();
		}
	}

	public List<VpnServerDto> GetAllServers()
	{
		if (MemoryCache.IsEmpty)
		{
			Semaphore.Wait();
			try
			{
				List<VpnServerDto> list = EncryptedJsonStore.Read<List<VpnServerDto>>(_filePath);
				if (list != null)
				{
					foreach (VpnServerDto item in list)
					{
						if (item.Id != null)
						{
							MemoryCache.TryAdd(item.Id, item);
						}
					}
				}
			}
			finally
			{
				Semaphore.Release();
			}
		}
		return new List<VpnServerDto>(MemoryCache.Values);
	}

	public bool RemoveServer(string serverId)
	{
		if (serverId == null)
		{
			throw new ArgumentException("Server ID cannot be null.", "serverId");
		}
		Semaphore.Wait();
		try
		{
			VpnServerDto value;
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

	public void ClearAll()
	{
		Semaphore.Wait();
		try
		{
			MemoryCache.Clear();
			EncryptedJsonStore.Write(_filePath, new List<VpnServerDto>());
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
