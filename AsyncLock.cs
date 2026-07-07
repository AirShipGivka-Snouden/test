using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace VpnHood.Core.Toolkit.Utils;

public sealed class AsyncLock
{
	public interface ILockAsyncResult : IDisposable
	{
		bool Succeeded { get; }
	}

	private class SemaphoreSlimEx : SemaphoreSlim
	{
		public int ReferenceCount { get; set; }

		public SemaphoreSlimEx(int initialCount, int maxCount)
			: base(initialCount, maxCount)
		{
		}
	}

	private class SemaphoreLock(SemaphoreSlimEx semaphoreSlimEx, bool succeeded, string? name) : ILockAsyncResult, IDisposable
	{
		private bool _disposed;

		public bool Succeeded { get; } = succeeded;

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			if (Succeeded)
			{
				semaphoreSlimEx.Release();
			}
			lock (SemaphoreSlims)
			{
				semaphoreSlimEx.ReferenceCount--;
				if (semaphoreSlimEx.ReferenceCount == 0 && name != null)
				{
					SemaphoreSlims.TryRemove(name, out SemaphoreSlimEx _);
				}
			}
		}
	}

	private readonly SemaphoreSlimEx _semaphoreSlimEx = new SemaphoreSlimEx(1, 1);

	private static readonly ConcurrentDictionary<string, SemaphoreSlimEx> SemaphoreSlims = new ConcurrentDictionary<string, SemaphoreSlimEx>();

	public bool IsLocked => _semaphoreSlimEx.CurrentCount == 0;

	public async Task<ILockAsyncResult> LockAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		await _semaphoreSlimEx.WaitAsync(cancellationToken).Vhc();
		return new SemaphoreLock(_semaphoreSlimEx, succeeded: true, null);
	}

	public async Task<ILockAsyncResult> LockAsync(TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken))
	{
		bool succeeded = await _semaphoreSlimEx.WaitAsync(timeout, cancellationToken).Vhc();
		return new SemaphoreLock(_semaphoreSlimEx, succeeded, null);
	}

	public static Task<ILockAsyncResult> LockAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
	{
		return LockAsync(name, Timeout.InfiniteTimeSpan, cancellationToken);
	}

	public static async Task<ILockAsyncResult> LockAsync(string name, TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken))
	{
		SemaphoreSlimEx semaphoreSlim;
		lock (SemaphoreSlims)
		{
			semaphoreSlim = SemaphoreSlims.GetOrAdd(name, (string _) => new SemaphoreSlimEx(1, 1));
			semaphoreSlim.ReferenceCount++;
		}
		try
		{
			return new SemaphoreLock(semaphoreSlim, await semaphoreSlim.WaitAsync(timeout, cancellationToken).Vhc(), name);
		}
		catch
		{
			lock (SemaphoreSlims)
			{
				semaphoreSlim.ReferenceCount--;
				if (semaphoreSlim.ReferenceCount == 0)
				{
					SemaphoreSlims.TryRemove(name, out SemaphoreSlimEx _);
				}
			}
			throw;
		}
	}
}
