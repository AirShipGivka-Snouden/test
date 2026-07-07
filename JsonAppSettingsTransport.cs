using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using Ciphra.VPN.Common.Storage;
using Serilog;

namespace Ciphra.VPN.Common.App;

public class JsonAppSettingsTransport : IDisposable
{
	private readonly string _filePath;

	private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

	private static readonly ConcurrentDictionary<string, object> _memoryCache = new ConcurrentDictionary<string, object>();

	private Dictionary<string, string>? _fileData;

	private volatile bool _disposed;

	private static readonly JsonSerializerOptions _valueJsonOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	public static int CachedSettingsCount => _memoryCache.Count;

	public JsonAppSettingsTransport(string filePath)
	{
		if (filePath == null)
		{
			throw new ArgumentNullException("filePath");
		}
		_filePath = filePath;
		try
		{
			_fileData = EncryptedJsonStore.Read<Dictionary<string, string>>(_filePath);
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Failed to load settings from {Path}. Starting with defaults.", _filePath);
			_fileData = null;
		}
	}

	public T? LoadOrInitProperty<T>(T? defaultValue, [CallerMemberName] string? key = null)
	{
		if (string.IsNullOrEmpty(key))
		{
			return defaultValue;
		}
		if (_memoryCache.TryGetValue(key, out object value))
		{
			if (value is T result)
			{
				return result;
			}
			try
			{
				if (value != null)
				{
					return (T)value;
				}
			}
			catch
			{
				_memoryCache.TryRemove(key, out object _);
			}
		}
		_semaphore.Wait();
		try
		{
			if (_disposed)
			{
				if (defaultValue != null)
				{
					_memoryCache.TryAdd(key, defaultValue);
				}
				return defaultValue;
			}
			if (_fileData != null && _fileData.TryGetValue(key, out string value3) && !string.IsNullOrEmpty(value3))
			{
				try
				{
					T value4 = JsonSerializer.Deserialize<T>(value3, _valueJsonOptions);
					if (value4 != null)
					{
						_memoryCache.AddOrUpdate(key, value4, (string k, object v) => value4);
					}
					return value4;
				}
				catch
				{
				}
			}
			T resultValue = defaultValue;
			if (resultValue != null)
			{
				_memoryCache.AddOrUpdate(key, resultValue, (string k, object v) => resultValue);
			}
			PersistUnderLock();
			return resultValue;
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Failed to load setting [{Key}]. Returning default.", key);
			return defaultValue;
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public void Save<T>(T value, [CallerMemberName] string? key = null)
	{
		if (string.IsNullOrEmpty(key))
		{
			return;
		}
		Log.Debug<string, T>("Saving setting [{Key}] with value: {Value}", key, value);
		_semaphore.Wait();
		try
		{
			if (_disposed)
			{
				if (value != null)
				{
					_memoryCache.AddOrUpdate(key, value, (string k, object v) => value);
				}
				Log.Debug<string>("Setting [{Key}] saved to cache only (disposed).", key);
				return;
			}
			_memoryCache.AddOrUpdate(key, value, (string k, object v) => value);
			try
			{
				PersistUnderLock();
			}
			catch (Exception ex)
			{
				Log.Warning<string>(ex, "Failed to persist setting [{Key}] to disk. Value is cached in memory.", key);
				return;
			}
			Log.Debug<string>("Setting [{Key}] saved successfully.", key);
		}
		catch
		{
			_memoryCache.TryRemove(key, out object _);
			Log.Error<string>("Failed to save setting {Key}. Removed from cache.", key);
			throw;
		}
		finally
		{
			_semaphore.Release();
		}
	}

	private void PersistUnderLock()
	{
		Dictionary<string, string> dictionary = ((_fileData != null) ? new Dictionary<string, string>(_fileData) : new Dictionary<string, string>());
		foreach (KeyValuePair<string, object> item in _memoryCache)
		{
			try
			{
				dictionary[item.Key] = JsonSerializer.Serialize(item.Value, item.Value.GetType(), _valueJsonOptions);
			}
			catch
			{
			}
		}
		EncryptedJsonStore.Write(_filePath, dictionary);
		_fileData = dictionary;
	}

	public static void ClearMemoryCache()
	{
		_memoryCache.Clear();
	}

	public static void RemoveFromCache(string key)
	{
		if (!string.IsNullOrEmpty(key))
		{
			_memoryCache.TryRemove(key, out object _);
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_semaphore.Wait();
		try
		{
			if (!_disposed)
			{
				_disposed = true;
			}
		}
		finally
		{
			_semaphore.Release();
		}
	}
}
