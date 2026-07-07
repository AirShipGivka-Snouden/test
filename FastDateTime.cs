using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace VpnHood.Core.Toolkit.Utils;

public static class FastDateTime
{
	private static readonly Lock Locker = new Lock();

	private static int _lastTickCount = Environment.TickCount;

	private static int _lastTickCountUtc = Environment.TickCount;

	[CompilerGenerated]
	private static DateTime _003CNow_003Ek__BackingField = DateTime.Now;

	[CompilerGenerated]
	private static DateTime _003CUtcNow_003Ek__BackingField = DateTime.UtcNow;

	public static TimeSpan Precision { get; set; } = TimeSpan.FromSeconds(1L);

	public static DateTime Now
	{
		get
		{
			using (Locker.EnterScope())
			{
				int tickCount = Environment.TickCount;
				if (tickCount - _lastTickCount >= Precision.Milliseconds || tickCount < _lastTickCount)
				{
					_003CNow_003Ek__BackingField = DateTime.Now;
					_lastTickCount = tickCount;
				}
				return _003CNow_003Ek__BackingField;
			}
		}
	}

	public static DateTime UtcNow
	{
		get
		{
			using (Locker.EnterScope())
			{
				int tickCount = Environment.TickCount;
				if (tickCount - _lastTickCountUtc >= Precision.Milliseconds || tickCount < _lastTickCountUtc)
				{
					_003CUtcNow_003Ek__BackingField = DateTime.UtcNow;
					_lastTickCountUtc = tickCount;
				}
				return _003CUtcNow_003Ek__BackingField;
			}
		}
	}
}
