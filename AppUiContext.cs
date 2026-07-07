using System;
using System.Runtime.CompilerServices;
using VpnHood.Core.Common.Exceptions;

namespace VpnHood.Core.Client.Device.UiContexts;

public static class AppUiContext
{
	[CompilerGenerated]
	private static IUiContext? _003CContext_003Ek__BackingField;

	public static IUiContext? Context
	{
		[CompilerGenerated]
		get
		{
			return _003CContext_003Ek__BackingField;
		}
		set
		{
			if (_003CContext_003Ek__BackingField != value)
			{
				_003CContext_003Ek__BackingField = value;
				AppUiContext.OnChanged?.Invoke(null, EventArgs.Empty);
			}
		}
	}

	public static IUiContext RequiredContext => Context ?? throw new UiContextNotAvailableException();

	public static bool IsPartialIntentRunning => PartialIntentScope.IsRunning;

	public static event EventHandler? OnChanged;

	public static IDisposable CreatePartialIntentScope()
	{
		return new PartialIntentScope();
	}
}
