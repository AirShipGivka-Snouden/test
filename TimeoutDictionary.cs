using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Toolkit.Collections;

public sealed class TimeoutDictionary<TKey, TValue>(TimeSpan? timeout = null) : IDisposable where TKey : notnull where TValue : ITimeoutItem
{
	private readonly ConcurrentDictionary<TKey, TValue> _items = new ConcurrentDictionary<TKey, TValue>();

	private DateTime _lastCleanupTime = DateTime.MinValue;

	private bool _disposed;

	private readonly Lock _cleanupLock = new Lock();

	public bool AutoCleanup { get; set; } = true;

	public TimeSpan? Timeout { get; set; } = timeout;

	public int Count
	{
		get
		{
			AutoCleanupInternal();
			return _items.Count;
		}
	}

	public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
	{
		AutoCleanupInternal();
		lock (_items)
		{
			TValue val = _items.AddOrUpdate(key, valueFactory, delegate(TKey k, TValue oldValue)
			{
				if (!IsExpired(oldValue))
				{
					return oldValue;
				}
				oldValue.Dispose();
				return valueFactory(k);
			});
			ref TValue reference = ref val;
			TValue val2 = default(TValue);
			if (val2 == null)
			{
				val2 = reference;
				reference = ref val2;
			}
			DateTime now = FastDateTime.Now;
			reference.LastUsedTime = now;
			val2 = val;
			return val2;
		}
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		AutoCleanupInternal();
		if (!_items.TryGetValue(key, out value))
		{
			return false;
		}
		TValue value2;
		if (IsExpired(value))
		{
			value = default(TValue);
			TryRemove(key, out value2);
			return false;
		}
		ref TValue reference = ref value;
		value2 = default(TValue);
		if (value2 == null)
		{
			value2 = reference;
			reference = ref value2;
		}
		DateTime now = FastDateTime.Now;
		reference.LastUsedTime = now;
		return true;
	}

	public TValue AddOrUpdate(TKey key, TValue value)
	{
		AutoCleanupInternal();
		lock (_items)
		{
			TValue val = _items.AddOrUpdate(key, value, delegate(TKey _, TValue oldValue)
			{
				oldValue.Dispose();
				return value;
			});
			ref TValue reference = ref val;
			TValue val2 = default(TValue);
			if (val2 == null)
			{
				val2 = reference;
				reference = ref val2;
			}
			DateTime now = FastDateTime.Now;
			reference.LastUsedTime = now;
			val2 = val;
			return val2;
		}
	}

	public bool TryAdd(TKey key, TValue value)
	{
		AutoCleanupInternal();
		if (!_items.TryAdd(key, value))
		{
			return false;
		}
		value.LastUsedTime = FastDateTime.Now;
		return true;
	}

	public bool TryRemove(TKey key, out TValue? value)
	{
		bool num = _items.TryRemove(key, out value);
		if (num)
		{
			ref TValue reference = ref value;
			TValue val = default(TValue);
			if (val == null)
			{
				val = reference;
				reference = ref val;
				if (val == null)
				{
					return num;
				}
			}
			reference.Dispose();
		}
		return num;
	}

	private bool IsExpired(ITimeoutItem item)
	{
		if (!item.IsDisposed)
		{
			if (Timeout.HasValue)
			{
				TimeSpan value = FastDateTime.Now - item.LastUsedTime;
				TimeSpan? timeout = Timeout;
				return value > timeout;
			}
			return false;
		}
		return true;
	}

	private void AutoCleanupInternal()
	{
		if (AutoCleanup)
		{
			Cleanup();
		}
	}

	public void Cleanup(bool force = false)
	{
		if (!Timeout.HasValue)
		{
			return;
		}
		using (_cleanupLock.EnterScope())
		{
			if (!force)
			{
				TimeSpan value = FastDateTime.Now - _lastCleanupTime;
				TimeSpan? timeSpan = Timeout / 3.0;
				if (value < timeSpan)
				{
					return;
				}
			}
			_lastCleanupTime = FastDateTime.Now;
			foreach (KeyValuePair<TKey, TValue> item in _items.Where((KeyValuePair<TKey, TValue> x) => IsExpired(x.Value)))
			{
				TryRemove(item.Key, out TValue _);
			}
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			RemoveAll();
		}
	}

	public void RemoveAll()
	{
		foreach (TValue value in _items.Values)
		{
			value.Dispose();
		}
		_items.Clear();
	}

	public void RemoveOldest()
	{
		DateTime dateTime = DateTime.MaxValue;
		TKey val = default(TKey);
		TValue value;
		foreach (KeyValuePair<TKey, TValue> item in _items)
		{
			value = item.Value;
			if (value.LastUsedTime < dateTime)
			{
				value = item.Value;
				dateTime = value.LastUsedTime;
				val = item.Key;
			}
		}
		if (val != null)
		{
			TryRemove(val, out value);
		}
	}
}
