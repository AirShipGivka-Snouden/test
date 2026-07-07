using System;
using System.CodeDom.Compiler;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Input;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Services;
using Ciphra.VPN.Common.Services.Interfaces;
using Ciphra.VPN.Common.Services.OnChain;
using Ciphra.VPN.Common.Utils;
using Ciphra.VPN.Common.ViewModels;
using Ciphra.VPN.Common.ViewModels.DataViewModels;
using Ciphra.VPN.Common.ViewModels.DialogViewModels;
using Ciphra.VPN.WinUI.Ciphra_VPN_WinUI_XamlTypeInfo;
using Ciphra.VPN.WinUI.Controls;
using Ciphra.VPN.WinUI.Dialogs;
using Ciphra.VPN.WinUI.Extensions;
using Ciphra.VPN.WinUI.Pages;
using Ciphra.VPN.WinUI.Services;
using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Microsoft.Windows.AppLifecycle;
using ReactiveUI.Builder;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.File;
using WinRT;
using WinRT.Ciphra_VPN_WinUIVtableClasses;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace Ciphra.VPN.WinUI;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IApplicationOverrides")]
[WinRTExposedType(typeof(Ciphra_VPN_WinUI_AppWinRTTypeDetails))]
public class App : Application, IXamlMetadataProvider
{
	private IpcServer _ipcServer;

	private string? _pendingProtocolUri;

	private const string settingsDbName = "settings.enc";

	private const string serversCacheDbName = "servers.enc";

	private const string favoritesFileName = "favorites.enc";

	private static readonly IDictionary<NavigationIntent, Type> RoutingDictionary = new Dictionary<NavigationIntent, Type>
	{
		[NavigationIntent.MainPage] = typeof(MainPage),
		[NavigationIntent.LocationsPage] = typeof(LocationsPage),
		[NavigationIntent.SettingsPage] = typeof(SettingsPage),
		[NavigationIntent.SplitTunnelPage] = typeof(SplitTunnelPage)
	}.ToFrozenDictionary();

	private IContainer _container;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private bool _contentLoaded;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private XamlMetaDataProvider __appProvider;

	public static bool MainWindowClosed { get; private set; }

	public static MainWindow? MainWindow { get; set; }

	public static bool HandleClosedEvents { get; set; } = true;

	public static ICommand? ExitApplication { get; private set; }

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	private XamlMetaDataProvider _AppProvider
	{
		get
		{
			if (__appProvider == null)
			{
				__appProvider = new XamlMetaDataProvider();
			}
			return __appProvider;
		}
	}

	public App()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		RegisterLogger();
		InitializeComponent();
		WinUIReactiveUIBuilderExtensions.WithWinUI((IReactiveUIBuilder)(object)RxAppBuilder.CreateReactiveUIBuilder()).WithExceptionHandler(Observer.Create<Exception>((Action<Exception>)delegate(Exception ex)
		{
			HandleException(ex, "ReactiveUI", fatal: false);
		})).BuildApp();
		((Application)this).UnhandledException += (UnhandledExceptionEventHandler)delegate(object s, UnhandledExceptionEventArgs e)
		{
			if (IsBenignException(e.Exception))
			{
				Log.Debug<string>(e.Exception, "Suppressed benign {Source}", "UnhandledException");
				e.Handled = true;
			}
			else
			{
				HandleException(e.Exception, "UnhandledException", fatal: true);
			}
		};
		TaskScheduler.UnobservedTaskException += delegate(object? s, UnobservedTaskExceptionEventArgs e)
		{
			try
			{
				AggregateException ex = e.Exception.Flatten();
				if (IsBenignNetworkException(ex))
				{
					Log.Debug((Exception)ex, "Suppressed benign UnobservedTaskException from external library");
				}
				else
				{
					HandleException(ex, "UnobservedTaskException", fatal: false);
				}
			}
			finally
			{
				e.SetObserved();
			}
		};
		AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is Exception ex)
			{
				if (!e.IsTerminating && IsBenignException(ex))
				{
					Log.Debug<string>(ex, "Suppressed benign {Source}", "UnhandledException");
				}
				else
				{
					HandleException(ex, "UnhandledException", fatal: true);
				}
			}
		};
	}

	private static bool IsBenignException(Exception ex)
	{
		if (ex is SocketException)
		{
			string? stackTrace = ex.StackTrace;
			if (stackTrace == null || !stackTrace.Contains("TunVpnAdapter"))
			{
				string? stackTrace2 = ex.StackTrace;
				if (stackTrace2 == null || !stackTrace2.Contains("ProtectSocket"))
				{
					goto IL_004a;
				}
			}
			return true;
		}
		goto IL_004a;
		IL_00b8:
		if (BenignExceptionFilter.IsBenignTeardownException(ex))
		{
			return true;
		}
		return false;
		IL_004a:
		if (ex is SocketException { SocketErrorCode: SocketError.AddressNotAvailable })
		{
			return true;
		}
		if (ex is ObjectDisposedException ex3)
		{
			string objectName = ex3.ObjectName;
			if (objectName == null || !objectName.Contains("WinTun"))
			{
				string objectName2 = ex3.ObjectName;
				if (objectName2 == null || !objectName2.Contains("VpnAdapter"))
				{
					goto IL_00b8;
				}
			}
			return true;
		}
		goto IL_00b8;
	}

	private static bool IsBenignNetworkException(AggregateException agg)
	{
		foreach (Exception innerException in agg.InnerExceptions)
		{
			string? stackTrace = innerException.StackTrace;
			if (stackTrace != null && stackTrace.Contains("VpnHood.Core.TcpStack"))
			{
				return true;
			}
			if (innerException is IOException ex && (ex.Message.Contains("forcibly closed by the remote host") || ex.Message.Contains("transport connection")))
			{
				return true;
			}
			if (innerException.Message.Contains("IpPacket") || innerException.Message.Contains("The decryption operation failed"))
			{
				return true;
			}
			if (innerException is ObjectDisposedException ex2)
			{
				string objectName = ex2.ObjectName;
				if (objectName == null || !objectName.Contains("NetworkStream"))
				{
					string objectName2 = ex2.ObjectName;
					if (objectName2 == null || !objectName2.Contains("WebSocketStream"))
					{
						string objectName3 = ex2.ObjectName;
						if (objectName3 == null || !objectName3.Contains("ChunkStream"))
						{
							string objectName4 = ex2.ObjectName;
							if (objectName4 == null || !objectName4.Contains("VpnHood.Core.TcpStack"))
							{
								string objectName5 = ex2.ObjectName;
								if (objectName5 == null || !objectName5.Contains("LocalTcpStream"))
								{
									goto IL_0152;
								}
							}
						}
					}
				}
				return true;
			}
			goto IL_0152;
			IL_0152:
			if (innerException is SocketException { SocketErrorCode: SocketError.AddressNotAvailable })
			{
				return true;
			}
			if (innerException is ChannelClosedException)
			{
				return true;
			}
			if (innerException is NullReferenceException)
			{
				string? stackTrace2 = innerException.StackTrace;
				if (stackTrace2 != null && stackTrace2.Contains("VpnHood"))
				{
					return true;
				}
			}
			if (innerException is ArgumentNullException { ParamName: "_startOptions" })
			{
				string? stackTrace3 = innerException.StackTrace;
				if (stackTrace3 != null && stackTrace3.Contains("TunVpnAdapter"))
				{
					return true;
				}
			}
			if (innerException is ObjectDisposedException ex5)
			{
				string objectName6 = ex5.ObjectName;
				if (objectName6 != null && objectName6.Contains("WinTunVpnAdapter"))
				{
					return true;
				}
			}
			if (innerException.GetType().Name == "PInvokeException")
			{
				string? stackTrace4 = innerException.StackTrace;
				if (stackTrace4 != null && stackTrace4.Contains("WinTunVpnAdapter"))
				{
					return true;
				}
			}
			if (innerException is IOException ex6 && ex6.Message.Contains("unexpected EOF") && ex6.Message.Contains("transport stream"))
			{
				return true;
			}
			if (innerException is InvalidOperationException ex7 && (ex7.Message.Contains("another write operation is pending") || ex7.Message.Contains("another read operation is pending")))
			{
				return true;
			}
			if (innerException is WebSocketException || (innerException is IOException ex8 && ex8.Message.Contains("Cannot determine the frame size")))
			{
				return true;
			}
			if (innerException.Message.Contains("netsh interface") && innerException.Message.Contains("mtu="))
			{
				return true;
			}
			if (innerException is TaskSchedulerException ex9 && ex9.InnerException is OutOfMemoryException)
			{
				return true;
			}
			if (innerException is OutOfMemoryException)
			{
				string? stackTrace5 = innerException.StackTrace;
				if (stackTrace5 != null && stackTrace5.Contains("TaskPoolScheduler"))
				{
					return true;
				}
			}
			if (BenignExceptionFilter.IsBenignTeardownException(innerException))
			{
				return true;
			}
		}
		return false;
	}

	protected override async void OnLaunched(LaunchActivatedEventArgs args)
	{
		try
		{
			Log.Information<string>("Application launched with arguments: {Args}", args.Arguments);
			_pendingProtocolUri = null;
			string protocolUri = TryGetProtocolActivationUri();
			IList<AppInstance> instances = AppInstance.GetInstances();
			if (instances.Count == 1)
			{
				Log.Information("Single Ciphra VPN instance is running, activating...");
				_pendingProtocolUri = protocolUri;
				HandleKeyInstanceActivation();
				return;
			}
			bool isCurrentElevated = PlatformHelpers.IsRunningElevated();
			Log.Information<bool>("New instance of Ciphra VPN is running elevated: {IsElevated}", isCurrentElevated);
			if (!isCurrentElevated)
			{
				Log.Information("Current instance of Ciphra VPN is not elevated, exiting.");
				if (!string.IsNullOrWhiteSpace(protocolUri))
				{
					await IpcServer.SendIpcAsync(new IpcMessage("OAuthCallback", null, protocolUri));
				}
				Environment.Exit(0);
				return;
			}
			Log.Information("New instance of Ciphra VPN is elevated, looking for other elevated instances");
			foreach (AppInstance appInstance in instances)
			{
				uint pid = appInstance.ProcessId;
				bool? isProcessElevated = PlatformHelpers.TryIsProcessElevated(pid);
				if (isProcessElevated == true && !appInstance.IsCurrent)
				{
					Log.Information("Found elevated instance of Ciphra VPN, exiting.");
					if (!string.IsNullOrWhiteSpace(protocolUri))
					{
						await IpcServer.SendIpcAsync(new IpcMessage("OAuthCallback", null, protocolUri));
					}
					await IpcServer.SendIpcAsync(new IpcMessage("ShowWindow", null));
					Environment.Exit(0);
					return;
				}
				if (isProcessElevated == false && !appInstance.IsCurrent)
				{
					Log.Information<uint>("Shutting down non-elevated instance of Ciphra VPN: {ProcessId}", pid);
					await IpcServer.SendIpcAsync(new IpcMessage("SetAdminFlag", isCurrentElevated));
				}
			}
			Log.Information("Activating elevated instance of Ciphra VPN.");
			_pendingProtocolUri = protocolUri;
			HandleKeyInstanceActivation();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Error during application launch");
			HandleException(ex2, "OnLaunched", fatal: true);
			Environment.Exit(1);
		}
	}

	private void HandleKeyInstanceActivation()
	{
		Log.Information("Activating key instance");
		_ipcServer = new IpcServer();
		_ipcServer.Start();
		RegisterComponents();
		if ((Window)(object)MainWindow == (Window)null)
		{
			MainWindow = ResolutionExtensions.Resolve<MainWindow>((IComponentContext)(object)_container);
			((Window)MainWindow).Closed += M_window_Closed;
			((Window)MainWindow).ExtendsContentIntoTitleBar = true;
			((Window)MainWindow).SetTitleBar((UIElement)(object)new AppTitleBar());
			((Window)MainWindow).AppWindow.SetIcon("Assets\\ciphra.ico");
			((Window)MainWindow).AppWindow.Title = "Ciphra VPN";
			((Window)(object)MainWindow).SetDialogProperties(400, 650);
			WindowExtensions.Show((Window)(object)MainWindow, true);
			((Window)MainWindow).Activate();
			MainWindowClosed = false;
			SubscriptionStatusBridge subscriptionStatusBridge = ResolutionExtensions.Resolve<SubscriptionStatusBridge>((IComponentContext)(object)_container);
			MainWindow.ForceCreateTrayIcon();
			ExitApplication = MainWindow.Exit;
			Task.Run((Action)TrySweepOrphanedAdapters);
		}
		else
		{
			WindowExtensions.Show((Window)(object)MainWindow, true);
			((Window)MainWindow).Activate();
			MainWindowClosed = false;
		}
		DispatchPendingProtocolUri();
	}

	private void DispatchPendingProtocolUri()
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		string uri = _pendingProtocolUri;
		_pendingProtocolUri = null;
		if (string.IsNullOrWhiteSpace(uri))
		{
			return;
		}
		MainWindow? mainWindow = MainWindow;
		DispatcherQueue val = ((mainWindow != null) ? ((Window)mainWindow).DispatcherQueue : null);
		if (val == (DispatcherQueue)null)
		{
			Log.Warning("MainWindow.DispatcherQueue unavailable; dispatching inline");
			InvokeHandleCallback(uri);
		}
		else
		{
			val.TryEnqueue((DispatcherQueueHandler)delegate
			{
				InvokeHandleCallback(uri);
			});
		}
	}

	private static void InvokeHandleCallback(string uri)
	{
		try
		{
			Log.Information("Dispatching OAuth callback from protocol activation");
			GetInstance<OAuthAccountViewModel>()?.HandleCallback(uri);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed dispatching protocol activation URI");
		}
	}

	private static string? TryGetProtocolActivationUri()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		try
		{
			AppActivationArguments activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
			if (activatedEventArgs == null || (int)activatedEventArgs.Kind != 4)
			{
				return null;
			}
			object data = activatedEventArgs.Data;
			IProtocolActivatedEventArgs e = (IProtocolActivatedEventArgs)((data is IProtocolActivatedEventArgs) ? data : null);
			if (e != null)
			{
				return e.Uri?.ToString();
			}
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to read protocol activation args");
		}
		return null;
	}

	private static void TrySweepOrphanedAdapters()
	{
		if (!OperatingSystem.IsWindows())
		{
			return;
		}
		try
		{
			Settings instance = GetInstance<Settings>();
			IVpnServiceManagerFactory instance2 = GetInstance<IVpnServiceManagerFactory>();
			if (instance == null || instance2 == null)
			{
				return;
			}
			HashSet<string> hashSet = new HashSet<string>(instance.KnownWintunAdapterNames);
			foreach (string winTunAdapterSeedName in instance2.WinTunAdapterSeedNames)
			{
				hashSet.Add(winTunAdapterSeedName);
			}
			IReadOnlyList<string> readOnlyList = WinTunAdapterCleaner.SweepOrphanedAdapters(hashSet, null);
			if (readOnlyList.Count > 0)
			{
				Log.Information<int, string>("Startup sweep removed {Count} orphaned WinTun adapter(s): {Names}", readOnlyList.Count, string.Join(", ", readOnlyList));
				instance.PruneKnownWintunAdapterNames(readOnlyList);
			}
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Startup WinTun sweep failed.");
		}
	}

	private void M_window_Closed(object sender, WindowEventArgs args)
	{
		if (HandleClosedEvents)
		{
			args.Handled = true;
			try
			{
				MainWindow? mainWindow = MainWindow;
				if (mainWindow != null)
				{
					WindowExtensions.Hide((Window)(object)mainWindow, false);
				}
				return;
			}
			catch (COMException ex)
			{
				Log.Warning((Exception)ex, "COMException during window hide (EfficiencyMode not supported on this OS build)");
				return;
			}
		}
		MainWindowClosed = true;
		Environment.Exit(0);
	}

	private void HandleException(Exception ex, string source, bool fatal)
	{
		if (fatal)
		{
			Log.Fatal<string>(ex, "Fatal exception in {Source}", source);
		}
		else
		{
			Log.Error<string>(ex, "Exception in {Source}", source);
		}
		if (_container == null)
		{
			return;
		}
		try
		{
			IAppAnalytics appAnalytics = ResolutionExtensions.Resolve<IAppAnalytics>((IComponentContext)(object)_container);
			appAnalytics.SendException(ex, source, fatal);
			if (fatal)
			{
				appAnalytics.FlushAsync().Wait(TimeSpan.FromSeconds(4L));
			}
		}
		catch (Exception ex2)
		{
			Log.Debug<string>("Failed to send exception to analytics: {Message}", ex2.Message);
		}
	}

	public static Type? GetView(NavigationIntent intent)
	{
		if (RoutingDictionary.TryGetValue(intent, out Type value))
		{
			return value;
		}
		return null;
	}

	public static T? GetInstance<T>() where T : class
	{
		try
		{
			App obj = Application.Current as App;
			return ((obj != null) ? ResolutionExtensions.Resolve<T>((IComponentContext)(object)obj._container) : null) ?? null;
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Failed to resolve instance of type {TypeName}", typeof(T).Name);
			throw ex;
		}
	}

	private void RegisterComponents()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		try
		{
			if (_container == null)
			{
				Log.Information("Initializing application components...");
				ContainerBuilder val = new ContainerBuilder();
				RegisterAnalytics(val);
				RegisterVpnService(val);
				RegisterServices(val);
				RegisterViewModels(val);
				RegisterViews(val);
				RegistrationExtensions.AsSelf<MainWindow, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<MainWindow>(val)).SingleInstance();
				_container = val.Build((ContainerBuildOptions)0);
				Log.Information("Components initialized successfully.");
			}
		}
		catch (Exception ex)
		{
			Log.Fatal(ex, "Failed to initialize application components.");
			throw;
		}
	}

	private void RegisterLogger()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		string appDataFolder = GetAppDataFolder();
		string text = Path.Combine(appDataFolder, "Logs");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		LoggerSinkConfiguration writeTo = LoggerSinkConfigurationDebugExtensions.Debug(new LoggerConfiguration().MinimumLevel.Debug().WriteTo, (LogEventLevel)0, "{Timestamp:HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}", (IFormatProvider)null, (LoggingLevelSwitch)null).WriteTo;
		string text2 = text + "\\app-.log";
		int? num = 7;
		Log.Logger = (ILogger)(object)FileLoggerConfigurationExtensions.File(writeTo, text2, (LogEventLevel)0, "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", (IFormatProvider)null, (long?)10000000L, (LoggingLevelSwitch)null, false, false, (TimeSpan?)null, (RollingInterval)3, true, num, (Encoding)null, (FileLifecycleHooks)null, (TimeSpan?)null).CreateLogger();
		Log.Logger.Information("Logger initialized successfully.");
	}

	private void RegisterAnalytics(ContainerBuilder builder)
	{
		RegistrationExtensions.RegisterType<OpenPanelAnalytics>(builder).As<IAppAnalytics>().SingleInstance();
	}

	private void RegisterVpnService(ContainerBuilder builder)
	{
		RegistrationExtensions.RegisterType<WinUiVpnServiceManagerFactory>(builder).As<IVpnServiceManagerFactory>().SingleInstance();
	}

	private void RegisterServices(ContainerBuilder builder)
	{
		string appDataFolder = GetAppDataFolder();
		RegistrationExtensions.AsSelf<JsonAppSettingsTransport, SimpleActivatorData>(RegistrationExtensions.Register<JsonAppSettingsTransport>(builder, (Func<IComponentContext, JsonAppSettingsTransport>)((IComponentContext c) => new JsonAppSettingsTransport(Path.Combine(appDataFolder, "settings.enc"))))).SingleInstance();
		RegistrationExtensions.AsSelf<Settings, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<Settings>(builder)).SingleInstance();
		RegistrationExtensions.RegisterType<WinUiDeviceIdService>(builder).As<IDeviceIdService>().SingleInstance();
		RegistrationExtensions.RegisterType<WinUiVpnStorageFolderProvider>(builder).As<IVpnStorageFolderProvider>().SingleInstance();
		RegistrationExtensions.AsSelf<OnChainKeyStore, SimpleActivatorData>(RegistrationExtensions.Register<OnChainKeyStore>(builder, (Func<IComponentContext, OnChainKeyStore>)((IComponentContext _) => new OnChainKeyStore(appDataFolder)))).SingleInstance();
		RegistrationExtensions.AsSelf<OnChainBundlePoller, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<OnChainBundlePoller>(builder).As<IOnChainBundlePoller>()).SingleInstance();
		RegistrationExtensions.AsSelf<OnChainRegistrar, SimpleActivatorData>(RegistrationExtensions.Register<OnChainRegistrar>(builder, (Func<IComponentContext, OnChainRegistrar>)((IComponentContext c) => new OnChainRegistrar(ResolutionExtensions.Resolve<OnChainBundlePoller>(c))))).SingleInstance();
		RegistrationExtensions.AsSelf<BundleCache, SimpleActivatorData>(RegistrationExtensions.Register<BundleCache>(builder, (Func<IComponentContext, BundleCache>)((IComponentContext _) => new BundleCache(appDataFolder)))).SingleInstance();
		RegistrationExtensions.AsSelf<OnChainCredentialService, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<OnChainCredentialService>(builder)).SingleInstance().OnActivated((Action<IActivatedEventArgs<OnChainCredentialService>>)delegate(IActivatedEventArgs<OnChainCredentialService> e)
		{
			e.Instance.Analytics = ResolutionExtensions.Resolve<IAppAnalytics>(e.Context);
		});
		RegistrationExtensions.AsSelf<AuthProvider, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<AuthProvider>(builder)).SingleInstance();
		RegistrationExtensions.AsSelf<BlockchainRelayResolver, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<BlockchainRelayResolver>(builder)).SingleInstance();
		RegistrationExtensions.AsSelf<RelayDomainManager, SimpleActivatorData>(RegistrationExtensions.Register<RelayDomainManager>(builder, (Func<IComponentContext, RelayDomainManager>)((IComponentContext c) => new RelayDomainManager(ResolutionExtensions.Resolve<Settings>(c), ResolutionExtensions.Resolve<BlockchainRelayResolver>(c))))).SingleInstance().OnActivated((Action<IActivatedEventArgs<RelayDomainManager>>)delegate(IActivatedEventArgs<RelayDomainManager> e)
		{
			e.Instance.Analytics = ResolutionExtensions.Resolve<IAppAnalytics>(e.Context);
		});
		RegistrationExtensions.Register<HttpClient>(builder, (Func<IComponentContext, HttpClient>)((IComponentContext c) => new HttpClient(new RelayFallbackHandler(ResolutionExtensions.Resolve<RelayDomainManager>(c))
		{
			InnerHandler = new HttpClientHandler()
		}))).As<HttpClient>().SingleInstance();
		RegistrationExtensions.AsSelf<ApiService, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<ApiService>(builder)).SingleInstance();
		RegistrationExtensions.AsSelf<VpnServerRepository, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<VpnServerRepository>(builder)).SingleInstance();
		RegistrationExtensions.AsSelf<VpnServersCache, SimpleActivatorData>(RegistrationExtensions.Register<VpnServersCache>(builder, (Func<IComponentContext, VpnServersCache>)((IComponentContext c) => new VpnServersCache(Path.Combine(appDataFolder, "servers.enc"))))).SingleInstance();
		RegistrationExtensions.RegisterType<SmartHealService>(builder).As<ISmartHealService>().SingleInstance();
		RegistrationExtensions.AsSelf<VpnService, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<VpnService>(builder).As<IVpnService>()).SingleInstance();
		RegistrationExtensions.AsSelf<TrafficAggregator, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<TrafficAggregator>(builder)).SingleInstance();
		RegistrationExtensions.AsSelf<ExternalIpService, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<ExternalIpService>(builder)).SingleInstance();
		RegistrationExtensions.AutoActivate<AggregateExceptionHandler, ConcreteReflectionActivatorData, SingleRegistrationStyle>(RegistrationExtensions.AsSelf<AggregateExceptionHandler, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<AggregateExceptionHandler>(builder))).As<IExceptionHandler>().SingleInstance();
		RegistrationExtensions.AsSelf<DialogController, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<DialogController>(builder)).SingleInstance();
		RegistrationExtensions.AsSelf<NavigationService, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<NavigationService>(builder).As<INavigationService>()).SingleInstance();
		RegistrationExtensions.AsSelf<RateNotifier, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<RateNotifier>(builder)).SingleInstance();
		RegistrationExtensions.AsSelf<PromoNotifier, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<PromoNotifier>(builder)).SingleInstance();
		RegistrationExtensions.RegisterType<FeedbackLauncher>(builder).As<IFeedbackLauncher>().SingleInstance();
		RegistrationExtensions.AsImplementedInterfaces<SubscriptionStatusBridge, ConcreteReflectionActivatorData>(RegistrationExtensions.AsSelf<SubscriptionStatusBridge, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<SubscriptionStatusBridge>(builder))).SingleInstance();
		RegistrationExtensions.RegisterType<StartUpController>(builder).As<IStartUpController>().SingleInstance();
		RegistrationExtensions.AsSelf<VpnServerFavoritesRepository, SimpleActivatorData>(RegistrationExtensions.Register<VpnServerFavoritesRepository>(builder, (Func<IComponentContext, VpnServerFavoritesRepository>)((IComponentContext c) => new VpnServerFavoritesRepository(Path.Combine(appDataFolder, "favorites.enc"))))).SingleInstance();
		RegistrationExtensions.AsSelf<ApiStoreService, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<ApiStoreService>(builder).As<IStoreService>()).SingleInstance();
		RegistrationExtensions.RegisterType<WinUiStripeUrlLauncher>(builder).As<IStripeUrlLauncher>().SingleInstance();
		RegistrationExtensions.RegisterType<WinUiOAuthLoginLauncher>(builder).As<IOAuthLoginLauncher>().SingleInstance();
		RegistrationExtensions.RegisterType<WinUiOAuthSecureStorage>(builder).As<IOAuthSecureStorage>().SingleInstance();
		RegistrationExtensions.AsSelf<OAuthApiClient, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<OAuthApiClient>(builder)).SingleInstance();
		RegistrationExtensions.AsSelf<WinUiImageUriFormatProvider, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<WinUiImageUriFormatProvider>(builder)).As<IImageUriFormatProvider>().SingleInstance();
		RegistrationExtensions.RegisterType<TrialService>(builder).As<ITrialService>().SingleInstance();
	}

	private void RegisterViewModels(ContainerBuilder builder)
	{
		RegistrationExtensions.RegisterType<VpnServerViewModel>(builder);
		RegistrationExtensions.AsSelf<AppShellViewModel, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<AppShellViewModel>(builder)).SingleInstance();
		RegistrationExtensions.AsSelf<AccountInfoViewModel, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<AccountInfoViewModel>(builder)).As<IExceptionBroadcaster>().SingleInstance();
		RegistrationExtensions.AsSelf<OAuthAccountViewModel, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<OAuthAccountViewModel>(builder)).SingleInstance();
		RegistrationExtensions.AsSelf<SmartHealResultViewModel, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<SmartHealResultViewModel>(builder)).SingleInstance();
		RegistrationExtensions.AsSelf<MainPageViewModel, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<MainPageViewModel>(builder)).As<IExceptionBroadcaster>().SingleInstance();
		RegistrationExtensions.AsSelf<LocationsPageViewModel, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<LocationsPageViewModel>(builder)).As<IExceptionBroadcaster>().SingleInstance();
		RegistrationExtensions.AsSelf<SettingsPageViewModel, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<SettingsPageViewModel>(builder)).As<IExceptionBroadcaster>().SingleInstance();
		RegistrationExtensions.AsSelf<SplitTunnelPageViewModel, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<SplitTunnelPageViewModel>(builder)).As<IExceptionBroadcaster>().SingleInstance();
		RegistrationExtensions.AsSelf<SubscriptionManager, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<SubscriptionManager>(builder)).As<IExceptionBroadcaster>().SingleInstance();
		RegistrationExtensions.AsSelf<ErrorDialogViewModel, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<ErrorDialogViewModel>(builder)).SingleInstance();
		RegistrationExtensions.AsSelf<TrafficLimitDialogViewModel, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<TrafficLimitDialogViewModel>(builder)).SingleInstance();
		RegistrationExtensions.AsSelf<RateUsDialogViewModel, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<RateUsDialogViewModel>(builder)).As<IExceptionBroadcaster>().SingleInstance();
		RegistrationExtensions.AsSelf<AuthDialogViewModel, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<AuthDialogViewModel>(builder)).As<IExceptionBroadcaster>().SingleInstance();
		RegistrationExtensions.AsSelf<TrialDialogViewModel, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<TrialDialogViewModel>(builder)).As<IExceptionBroadcaster>().SingleInstance();
		RegistrationExtensions.AsSelf<SubscriptionDialogViewModel, ConcreteReflectionActivatorData>(RegistrationExtensions.RegisterType<SubscriptionDialogViewModel>(builder)).SingleInstance();
	}

	private void RegisterViews(ContainerBuilder builder)
	{
		RegistrationExtensions.RegisterType<AppShell>(builder).SingleInstance();
		RegistrationExtensions.RegisterType<AuthDialogPage>(builder).SingleInstance();
		RegistrationExtensions.RegisterType<ErrorDialogPage>(builder).SingleInstance();
		RegistrationExtensions.RegisterType<RateUsDialogPage>(builder).SingleInstance();
		RegistrationExtensions.RegisterType<SubscribeDialogPage>(builder).SingleInstance();
		RegistrationExtensions.RegisterType<ElevationPromptDialog>(builder).SingleInstance();
		RegistrationExtensions.RegisterType<PromoDialogPage>(builder).SingleInstance();
		RegistrationExtensions.RegisterType<TrialDialogPage>(builder).SingleInstance();
		RegistrationExtensions.RegisterType<TrafficLimitDialogPage>(builder).SingleInstance();
	}

	private string GetAppDataFolder()
	{
		string path = ApplicationData.Current.LocalFolder.Path;
		string text = Path.Combine(path, "data");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return text;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("ms-appx:///App.xaml");
			Application.LoadComponent((object)this, uri);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public IXamlType GetXamlType(Type type)
	{
		return _AppProvider.GetXamlType(type);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public IXamlType GetXamlType(string fullName)
	{
		return _AppProvider.GetXamlType(fullName);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public XmlnsDefinition[] GetXmlnsDefinitions()
	{
		return _AppProvider.GetXmlnsDefinitions();
	}
}
