using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;

namespace Ciphra.VPN.WinUI;

public static class Program
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static ApplicationInitializationCallback _003C_003E9__0_0;

		internal void _003CMain_003Eb__0_0(ApplicationInitializationCallbackParams p)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Expected O, but got Unknown
			DispatcherQueueSynchronizationContext synchronizationContext = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
			SynchronizationContext.SetSynchronizationContext((SynchronizationContext?)(object)synchronizationContext);
			new App();
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	[STAThread]
	private static void Main(string[] args)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		ComWrappersSupport.InitializeComWrappers((ComWrappers)null);
		object obj = _003C_003Ec._003C_003E9__0_0;
		if (obj == null)
		{
			ApplicationInitializationCallback val = delegate
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Expected O, but got Unknown
				DispatcherQueueSynchronizationContext synchronizationContext = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
				SynchronizationContext.SetSynchronizationContext((SynchronizationContext?)(object)synchronizationContext);
				new App();
			};
			_003C_003Ec._003C_003E9__0_0 = val;
			obj = (object)val;
		}
		Application.Start((ApplicationInitializationCallback)obj);
	}
}
