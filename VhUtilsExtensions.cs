using System;
using System.Diagnostics;

namespace VpnHood.Core.Toolkit.Utils;

public static class VhUtilsExtensions
{
	public static TimeSpan WhenNoDebugger(this TimeSpan value)
	{
		if (!Debugger.IsAttached || !VhUtils.DebuggerTimeout.HasValue)
		{
			return value;
		}
		return VhUtils.DebuggerTimeout.Value;
	}
}
