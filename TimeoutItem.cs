using System;

namespace VpnHood.Core.Toolkit.Collections;

public class TimeoutItem : ITimeoutItem, IDisposable
{
	public DateTime LastUsedTime { get; set; }

	public bool IsDisposed { get; private set; }

	protected virtual void Dispose(bool disposing)
	{
		IsDisposed = true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
public sealed class TimeoutItem<T>(T value, bool autoDispose = false) : TimeoutItem
{
	public T Value { get; set; } = value;

	protected override void Dispose(bool disposing)
	{
		if (!base.IsDisposed)
		{
			if (autoDispose && Value is IDisposable disposable)
			{
				disposable.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
