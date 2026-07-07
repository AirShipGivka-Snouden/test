using System;
using System.Collections.Generic;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Toolkit.Collections;

public static class TimeoutItemUtil
{
	public static void CleanupTimeoutList<T>(List<T> list, TimeSpan timeout) where T : ITimeoutItem
	{
		DateTime now = FastDateTime.Now;
		for (int num = list.Count - 1; num >= 0; num--)
		{
			T val = list[num];
			if (val.IsDisposed || now - val.LastUsedTime > timeout)
			{
				val.Dispose();
				list.RemoveAt(num);
			}
		}
	}
}
