using System;

namespace VpnHood.Core.Toolkit.Utils;

public class AutoDispose(Action disposeAction) : IDisposable
{
	public void Dispose()
	{
		disposeAction();
	}
}
