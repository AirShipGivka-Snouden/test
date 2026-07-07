using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Input;
using Ciphra.VPN.WinUI.Controls;
using Ciphra.VPN.WinUI.Extensions;
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using ReactiveUI;
using Serilog;
using WinRT;
using WinRT.Ciphra_VPN_WinUIVtableClasses;

namespace Ciphra.VPN.WinUI;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.Markup.IComponentConnector")]
[WinRTExposedType(typeof(Ciphra_VPN_WinUI_AppWindows_PaymentWindowWinRTTypeDetails))]
public sealed class MainWindow : Window, IComponentConnector
{
	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private interface IMainWindow_Bindings
	{
		void Initialize();

		void Update();

		void StopTracking();

		void DisconnectUnloadedObject(int connectionId);
	}

	private interface IMainWindow_BindingsScopeConnector
	{
		WeakReference Parent { get; set; }

		bool ContainsElement(int connectionId);

		void RegisterForElementConnection(int connectionId, IComponentConnector connector);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	private static class XamlBindingSetters
	{
		public static void Set_H_NotifyIcon_TaskbarIcon_DoubleClickCommand(TaskbarIcon obj, ICommand value, string targetNullValue)
		{
			if (value == null && targetNullValue != null)
			{
				value = (ICommand)XamlBindingHelper.ConvertValue(typeof(ICommand), (object)targetNullValue);
			}
			obj.DoubleClickCommand = value;
		}

		public static void Set_H_NotifyIcon_TaskbarIcon_LeftClickCommand(TaskbarIcon obj, ICommand value, string targetNullValue)
		{
			if (value == null && targetNullValue != null)
			{
				value = (ICommand)XamlBindingHelper.ConvertValue(typeof(ICommand), (object)targetNullValue);
			}
			obj.LeftClickCommand = value;
		}

		public static void Set_Microsoft_UI_Xaml_Controls_MenuFlyoutItem_Command(MenuFlyoutItem obj, ICommand value, string targetNullValue)
		{
			if (value == null && targetNullValue != null)
			{
				value = (ICommand)XamlBindingHelper.ConvertValue(typeof(ICommand), (object)targetNullValue);
			}
			obj.Command = value;
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	[WinRTRuntimeClassName("Microsoft.UI.Xaml.Markup.IComponentConnector")]
	[WinRTExposedType(typeof(Ciphra_VPN_WinUI_AppWindows_PaymentWindowWinRTTypeDetails))]
	private class MainWindow_obj1_Bindings : IComponentConnector, IMainWindow_Bindings
	{
		private MainWindow dataRoot;

		private bool initialized = false;

		private const int NOT_PHASED = int.MinValue;

		private const int DATA_CHANGED = 1073741824;

		private TaskbarIcon obj3;

		private MenuFlyoutItem obj4;

		private MenuFlyoutItem obj5;

		public void Connect(int connectionId, object target)
		{
			switch (connectionId)
			{
			case 3:
				obj3 = CastExtensions.As<TaskbarIcon>(target);
				break;
			case 4:
				obj4 = CastExtensions.As<MenuFlyoutItem>(target);
				break;
			case 5:
				obj5 = CastExtensions.As<MenuFlyoutItem>(target);
				break;
			}
		}

		[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
		[DebuggerNonUserCode]
		public IComponentConnector GetBindingConnector(int connectionId, object target)
		{
			return null;
		}

		public void Initialize()
		{
			if (!initialized)
			{
				Update();
			}
		}

		public void Update()
		{
			Update_(dataRoot, int.MinValue);
			initialized = true;
		}

		public void StopTracking()
		{
		}

		public void DisconnectUnloadedObject(int connectionId)
		{
			throw new ArgumentException("No unloadable elements to disconnect.");
		}

		public bool SetDataRoot(object newDataRoot)
		{
			if (newDataRoot != null)
			{
				dataRoot = CastExtensions.As<MainWindow>(newDataRoot);
				return true;
			}
			return false;
		}

		public void Activated(object obj, WindowActivatedEventArgs data)
		{
			Initialize();
		}

		public void Loading(FrameworkElement src, object data)
		{
			Initialize();
		}

		private void Update_(MainWindow obj, int phase)
		{
			if ((Window)(object)obj != (Window)null && (phase & -2147483647) != 0)
			{
				Update_Restore(obj.Restore, phase);
				Update_Exit(obj.Exit, phase);
			}
		}

		private void Update_Restore(ICommand obj, int phase)
		{
			if ((phase & -2147483647) != 0)
			{
				XamlBindingSetters.Set_H_NotifyIcon_TaskbarIcon_DoubleClickCommand(obj3, obj, null);
				XamlBindingSetters.Set_H_NotifyIcon_TaskbarIcon_LeftClickCommand(obj3, obj, null);
				XamlBindingSetters.Set_Microsoft_UI_Xaml_Controls_MenuFlyoutItem_Command(obj4, obj, null);
			}
		}

		private void Update_Exit(ICommand obj, int phase)
		{
			if ((phase & -2147483647) != 0)
			{
				XamlBindingSetters.Set_Microsoft_UI_Xaml_Controls_MenuFlyoutItem_Command(obj5, obj, null);
			}
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Grid AppLayoutGrid;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	public TaskbarIcon TaskbarIcon;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private bool _contentLoaded;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private IMainWindow_Bindings Bindings;

	public ICommand Restore { get; }

	public ICommand Exit { get; }

	public MainWindow(AppShell appShell)
	{
		if ((UserControl)(object)appShell == (UserControl)null)
		{
			throw new ArgumentNullException("appShell");
		}
		InitializeComponent();
		ReactiveCommand<Unit, Unit> val = ReactiveCommand.Create<Unit, Unit>((Func<Unit, Unit>)((Unit _) => _), (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> val2 = ReactiveCommand.Create((Action)RestoreWindow, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> val3 = ReactiveCommand.Create((Action)ExitApplication, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommandMixins.InvokeCommand<Unit, Unit>(Observable.ObserveOn<Unit>((IObservable<Unit>)val, RxSchedulers.MainThreadScheduler), (ReactiveCommandBase<Unit, Unit>)(object)val2);
		Restore = (ICommand)val;
		Exit = (ICommand)val3;
		((Panel)AppLayoutGrid).Children.Clear();
		((Panel)AppLayoutGrid).Children.Add((UIElement)(object)appShell);
	}

	public void ForceCreateTrayIcon()
	{
		try
		{
			if (!TaskbarIcon.IsCreated)
			{
				Log.Debug("Tray icon is not created. Creating tray icon");
				TaskbarIcon.ForceCreate(false);
				Log.Debug<bool>("Tray icon created: {TaskBarCreated}", TaskbarIcon.IsCreated);
			}
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Failed to create tray icon: {Message}", ex.Message);
		}
	}

	private void RestoreWindow()
	{
		try
		{
			if (!((Window)this).Visible)
			{
				WindowExtensions.Show((Window)(object)this, false);
			}
			((Window)this).Activate();
			((Window)(object)this).BringToFront();
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Failed to restore window: {Message}", ex.Message);
		}
	}

	private void ExitApplication()
	{
		try
		{
			App.HandleClosedEvents = false;
			MainWindow? mainWindow = App.MainWindow;
			if (mainWindow != null)
			{
				((Window)mainWindow).Close();
			}
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Failed to exit application: {Message}", ex.Message);
			Environment.Exit(0);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("ms-appx:///MainWindow.xaml");
			Application.LoadComponent((object)this, uri, (ComponentResourceLocation)0);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 2:
			AppLayoutGrid = CastExtensions.As<Grid>(target);
			break;
		case 3:
			TaskbarIcon = CastExtensions.As<TaskbarIcon>(target);
			break;
		}
		_contentLoaded = true;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public IComponentConnector GetBindingConnector(int connectionId, object target)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		IComponentConnector result = null;
		if (connectionId == 1)
		{
			Window val = (Window)target;
			MainWindow_obj1_Bindings mainWindow_obj1_Bindings = new MainWindow_obj1_Bindings();
			result = (IComponentConnector)(object)mainWindow_obj1_Bindings;
			mainWindow_obj1_Bindings.SetDataRoot(this);
			Bindings = mainWindow_obj1_Bindings;
			val.Activated += mainWindow_obj1_Bindings.Activated;
		}
		return result;
	}
}
