using System;
using System.CodeDom.Compiler;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.ViewModels;
using Ciphra.VPN.Common.ViewModels.DialogViewModels;
using Ciphra.VPN.WinUI.Dialogs;
using Ciphra.VPN.WinUI.Extensions;
using Ciphra.VPN.WinUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using ReactiveUI;
using Serilog;
using WinRT;
using WinRT.Ciphra_VPN_WinUIVtableClasses;
using Windows.System;

namespace Ciphra.VPN.WinUI.Controls;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(Ciphra_VPN_WinUI_Controls_Icons_VpnIconWinRTTypeDetails))]
public sealed class AppShell : UserControl, IViewFor<AppShellViewModel>, IViewFor, IActivatableView, IComponentConnector
{
	private readonly DialogController _dialogController;

	private readonly NavigationService _navigation;

	private readonly IAppAnalytics _analytics;

	private readonly IDictionary<string, Action> _navigationCommands;

	private readonly ReactiveCommand<Unit, Unit> _showSubscribeDialogCommand;

	private Task? _showTrialTask;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private NavigationView AppShellNavigationView;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private NavigationViewItem MainPageNavigationViewItem;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private NavigationViewItem LocationsNavigationViewItem;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private NavigationViewItem SettingsNavigationViewItem;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private Frame ViewFrame;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	private bool _contentLoaded;

	object? IViewFor.ViewModel
	{
		get
		{
			return ViewModel;
		}
		set
		{
			ViewModel = (AppShellViewModel)value;
		}
	}

	public AppShellViewModel? ViewModel { get; set; }

	public AppShell(AppShellViewModel viewModel, DialogController dialogController, NavigationService navigation, IAppAnalytics analytics)
	{
		//IL_04d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dc: Expected O, but got Unknown
		ViewModel = viewModel ?? throw new ArgumentNullException("viewModel");
		_dialogController = dialogController ?? throw new ArgumentNullException("dialogController");
		_navigation = navigation ?? throw new ArgumentNullException("navigation");
		_analytics = analytics ?? throw new ArgumentNullException("analytics");
		InitializeComponent();
		ObservableExtensions.Subscribe<NavigationArgs>(Observable.ObserveOn<NavigationArgs>(navigation.NavigatedObservable, RxSchedulers.MainThreadScheduler), (Action<NavigationArgs>)delegate(NavigationArgs t)
		{
			switch (t.Intent)
			{
			case NavigationIntent.MainPage:
				((NavigationViewItemBase)MainPageNavigationViewItem).IsSelected = true;
				break;
			case NavigationIntent.LocationsPage:
				((NavigationViewItemBase)LocationsNavigationViewItem).IsSelected = true;
				break;
			case NavigationIntent.SettingsPage:
				((NavigationViewItemBase)SettingsNavigationViewItem).IsSelected = true;
				ShowPage(NavigationIntent.SettingsPage);
				break;
			case NavigationIntent.SplitTunnelPage:
				((NavigationViewItemBase)SettingsNavigationViewItem).IsSelected = true;
				ShowPage(NavigationIntent.SplitTunnelPage);
				break;
			default:
				Log.Warning<NavigationIntent>("Unhandled navigation intent: {Intent}", t.Intent);
				break;
			}
		});
		ReactiveCommand<Unit, Unit> val = ReactiveCommand.CreateFromTask((Func<Task>)ShowErrorDialog, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> val2 = ReactiveCommand.CreateFromTask((Func<Task>)ShowRateUsDialog, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> val3 = ReactiveCommand.CreateFromTask((Func<Task>)ShowAuthDialog, (IObservable<bool>)null, (IScheduler)null);
		_showSubscribeDialogCommand = ReactiveCommand.CreateFromTask((Func<Task>)ShowSubscribeDialog, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> val4 = ReactiveCommand.CreateFromTask((Func<Task>)ShowElevationPromptDialog, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> val5 = ReactiveCommand.CreateFromTask((Func<Task>)ShowPromoDialog, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> val6 = ReactiveCommand.CreateFromTask((Func<Task>)ShowTrialDialog, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> val7 = ReactiveCommand.CreateFromTask((Func<Task>)ShowTrafficLimitDialog, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> val8 = ReactiveCommand.CreateFromTask((Func<Task>)VisitWebsite, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommand<Unit, Unit> val9 = ReactiveCommand.CreateFromTask((Func<Task>)VisitSubscriptionsPage, (IObservable<bool>)null, (IScheduler)null);
		ReactiveCommandMixins.InvokeCommand<Unit, Unit>(Observable.Select<ShowDialogIntent, Unit>(Observable.Where<ShowDialogIntent>(dialogController.ShowDialogObservable, (Func<ShowDialogIntent, bool>)((ShowDialogIntent d) => d == ShowDialogIntent.AuthDialog)), (Func<ShowDialogIntent, Unit>)((ShowDialogIntent _) => Unit.Default)), (ReactiveCommandBase<Unit, Unit>)(object)val3);
		ReactiveCommandMixins.InvokeCommand<Unit, Unit>(Observable.Select<ShowDialogIntent, Unit>(Observable.Where<ShowDialogIntent>(dialogController.ShowDialogObservable, (Func<ShowDialogIntent, bool>)((ShowDialogIntent d) => d == ShowDialogIntent.ErrorDialog)), (Func<ShowDialogIntent, Unit>)((ShowDialogIntent _) => Unit.Default)), (ReactiveCommandBase<Unit, Unit>)(object)val);
		ReactiveCommandMixins.InvokeCommand<Unit, Unit>(Observable.Select<ShowDialogIntent, Unit>(Observable.Where<ShowDialogIntent>(dialogController.ShowDialogObservable, (Func<ShowDialogIntent, bool>)((ShowDialogIntent d) => d == ShowDialogIntent.RateUsDialog)), (Func<ShowDialogIntent, Unit>)((ShowDialogIntent _) => Unit.Default)), (ReactiveCommandBase<Unit, Unit>)(object)val2);
		ReactiveCommandMixins.InvokeCommand<Unit, Unit>(Observable.Select<ShowDialogIntent, Unit>(Observable.Where<ShowDialogIntent>(dialogController.ShowDialogObservable, (Func<ShowDialogIntent, bool>)((ShowDialogIntent d) => d == ShowDialogIntent.SubscribeDialog)), (Func<ShowDialogIntent, Unit>)((ShowDialogIntent _) => Unit.Default)), (ReactiveCommandBase<Unit, Unit>)(object)_showSubscribeDialogCommand);
		ReactiveCommandMixins.InvokeCommand<Unit, Unit>(Observable.Select<ShowDialogIntent, Unit>(Observable.Where<ShowDialogIntent>(dialogController.ShowDialogObservable, (Func<ShowDialogIntent, bool>)((ShowDialogIntent d) => d == ShowDialogIntent.ElevationDialog)), (Func<ShowDialogIntent, Unit>)((ShowDialogIntent _) => Unit.Default)), (ReactiveCommandBase<Unit, Unit>)(object)val4);
		ReactiveCommandMixins.InvokeCommand<Unit, Unit>(Observable.Select<ShowDialogIntent, Unit>(Observable.Where<ShowDialogIntent>(dialogController.ShowDialogObservable, (Func<ShowDialogIntent, bool>)((ShowDialogIntent d) => d == ShowDialogIntent.PromotionDialog)), (Func<ShowDialogIntent, Unit>)((ShowDialogIntent _) => Unit.Default)), (ReactiveCommandBase<Unit, Unit>)(object)val5);
		ReactiveCommandMixins.InvokeCommand<Unit, Unit>(Observable.Select<ShowDialogIntent, Unit>(dialogController.ShowTrialDialogObservable, (Func<ShowDialogIntent, Unit>)((ShowDialogIntent _) => Unit.Default)), (ReactiveCommandBase<Unit, Unit>)(object)val6);
		ReactiveCommandMixins.InvokeCommand<Unit, Unit>(Observable.Select<ShowDialogIntent, Unit>(dialogController.ShowTrafficLimitDialogObservable, (Func<ShowDialogIntent, Unit>)((ShowDialogIntent _) => Unit.Default)), (ReactiveCommandBase<Unit, Unit>)(object)val7);
		ReactiveCommandMixins.InvokeCommand<Unit, Unit>(Observable.Select<ShowDialogIntent, Unit>(Observable.Where<ShowDialogIntent>(dialogController.ShowDialogObservable, (Func<ShowDialogIntent, bool>)((ShowDialogIntent d) => d == ShowDialogIntent.VisitCiphraIntent)), (Func<ShowDialogIntent, Unit>)((ShowDialogIntent _) => Unit.Default)), (ReactiveCommandBase<Unit, Unit>)(object)val8);
		ReactiveCommandMixins.InvokeCommand<Unit, Unit>(Observable.Select<ShowDialogIntent, Unit>(Observable.Where<ShowDialogIntent>(dialogController.ShowDialogObservable, (Func<ShowDialogIntent, bool>)((ShowDialogIntent d) => d == ShowDialogIntent.VisitSubscriptionsPageIntent)), (Func<ShowDialogIntent, Unit>)((ShowDialogIntent _) => Unit.Default)), (ReactiveCommandBase<Unit, Unit>)(object)val9);
		_navigationCommands = new Dictionary<string, Action>
		{
			{
				"MainPage",
				delegate
				{
					ShowPage(NavigationIntent.MainPage);
				}
			},
			{
				"Locations",
				delegate
				{
					ShowPage(NavigationIntent.LocationsPage);
				}
			},
			{
				"Settings",
				delegate
				{
					ShowPage(NavigationIntent.SettingsPage);
				}
			}
		}.ToFrozenDictionary();
		((FrameworkElement)this).Loaded += (RoutedEventHandler)delegate
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			ViewModel?.Init.Execute(Unit.Default);
			if (!PlatformHelpers.IsRunningElevated())
			{
				ShowElevationPromptDialog();
			}
		};
	}

	private void ShowPage(NavigationIntent intent)
	{
		try
		{
			Type view = App.GetView(intent);
			if (view != null)
			{
				ViewFrame.Navigate(view);
			}
			string text = intent.ToString();
			Log.Information<string>("Navigating to {PageName}", text);
			_analytics.SendView(text);
		}
		catch (Exception ex)
		{
			Log.Error<NavigationIntent>(ex, "Failed to navigate to {Intent}", intent);
		}
	}

	private void AppShellNavigationView_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		try
		{
			NavigationViewItemBase selectedItemContainer = args.SelectedItemContainer;
			NavigationViewItem val = (NavigationViewItem)(object)((selectedItemContainer is NavigationViewItem) ? selectedItemContainer : null);
			if (val != null && ((FrameworkElement)val).Tag is string key && _navigationCommands.TryGetValue(key, out Action value))
			{
				value();
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error during navigation item invocation");
		}
	}

	private async Task ShowRateUsDialog()
	{
		try
		{
			while (((UIElement)this).XamlRoot == (XamlRoot)null)
			{
				await Task.Delay(100);
			}
			RateUsDialogPage page = App.GetInstance<RateUsDialogPage>();
			ContentDialog dialog = new ContentDialog
			{
				XamlRoot = ((UIElement)this).XamlRoot,
				Content = page,
				Title = Loc.Get("RateUsDialogTitle"),
				PrimaryButtonText = Loc.Get("RateUsLikeButton"),
				SecondaryButtonText = Loc.Get("RateUsDislikeButton"),
				CloseButtonText = Loc.Get("LaterButton"),
				DefaultButton = (ContentDialogButton)1,
				PrimaryButtonCommand = page.ViewModel?.Like,
				SecondaryButtonCommand = page.ViewModel?.Dislike,
				CloseButtonCommand = page.ViewModel?.Later
			};
			await dialog.ShowAsyncQueue();
			SendDialogView("Rate Us Dialog");
			((ContentControl)dialog).Content = null;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Error showing rate dialog");
		}
	}

	private async Task ShowErrorDialog()
	{
		try
		{
			while (((UIElement)this).XamlRoot == (XamlRoot)null)
			{
				await Task.Delay(100);
			}
			ErrorDialogPage page = App.GetInstance<ErrorDialogPage>();
			ContentDialog dialog = new ContentDialog
			{
				Title = Loc.Get("ErrorDialogTitle"),
				XamlRoot = ((UIElement)this).XamlRoot,
				Content = page,
				CloseButtonText = Loc.Get("CloseButton"),
				DefaultButton = (ContentDialogButton)3
			};
			await dialog.ShowAsyncQueue();
			SendDialogView("Error Dialog");
			((ContentControl)dialog).Content = null;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Error showing error dialog");
		}
	}

	private async Task ShowAuthDialog()
	{
		try
		{
			while (((UIElement)this).XamlRoot == (XamlRoot)null)
			{
				await Task.Delay(100);
			}
			AuthDialogPage page = App.GetInstance<AuthDialogPage>();
			ContentDialog dialog = new ContentDialog
			{
				XamlRoot = ((UIElement)this).XamlRoot,
				Content = page,
				PrimaryButtonText = Loc.Get("AuthDialogActivateButton"),
				PrimaryButtonCommand = page.ViewModel?.Activate,
				DefaultButton = (ContentDialogButton)1
			};
			IDisposable canActivateSub = ObservableExtensions.Subscribe<bool>(Observable.ObserveOn<bool>(WhenAnyMixin.WhenAnyValue<AuthDialogViewModel, bool>(page.ViewModel, (Expression<Func<AuthDialogViewModel, bool>>)((AuthDialogViewModel t) => t.IsTokenValid)), RxSchedulers.MainThreadScheduler), (Action<bool>)delegate(bool t)
			{
				dialog.IsPrimaryButtonEnabled = t;
			});
			await dialog.ShowAsyncQueue();
			SendDialogView("Auth Dialog");
			((ContentControl)dialog).Content = null;
			canActivateSub.Dispose();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Error showing auth dialog");
		}
	}

	private async Task ShowSubscribeDialog()
	{
		try
		{
			while (((UIElement)this).XamlRoot == (XamlRoot)null)
			{
				await Task.Delay(100);
			}
			SubscribeDialogPage page = App.GetInstance<SubscribeDialogPage>();
			if (!((Page)(object)page == (Page)null) && page.ViewModel != null)
			{
				ContentDialog dialog = new ContentDialog
				{
					XamlRoot = ((UIElement)this).XamlRoot,
					Content = page,
					CloseButtonText = Loc.Get("GoBackButton")
				};
				await dialog.ShowAsyncQueue();
				SendDialogView("Subscribe Dialog");
				((ContentControl)dialog).Content = null;
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Error showing subscribe dialog");
		}
	}

	private async Task ShowElevationPromptDialog()
	{
		try
		{
			while (((UIElement)this).XamlRoot == (XamlRoot)null)
			{
				await Task.Delay(100);
			}
			ElevationPromptDialog page = App.GetInstance<ElevationPromptDialog>();
			ContentDialog dialog = new ContentDialog
			{
				XamlRoot = ((UIElement)this).XamlRoot,
				Content = page,
				PrimaryButtonText = Loc.Get("ElevationUnderstandButton"),
				DefaultButton = (ContentDialogButton)1
			};
			await dialog.ShowAsyncQueue();
			SendDialogView("Elevation Prompt Dialog");
			((ContentControl)dialog).Content = null;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Error showing elevation prompt dialog");
		}
	}

	private async Task ShowPromoDialog()
	{
		try
		{
			while (((UIElement)this).XamlRoot == (XamlRoot)null)
			{
				await Task.Delay(100);
			}
			PromoDialogPage page = App.GetInstance<PromoDialogPage>();
			ContentDialog dialog = new ContentDialog
			{
				XamlRoot = ((UIElement)this).XamlRoot,
				Content = page,
				PrimaryButtonText = Loc.Get("PromoUpgradeNowButton"),
				CloseButtonText = Loc.Get("LaterButton"),
				PrimaryButtonCommand = (ICommand)_showSubscribeDialogCommand,
				DefaultButton = (ContentDialogButton)1
			};
			await dialog.ShowAsyncQueue();
			SendDialogView("Promo Dialog");
			((ContentControl)dialog).Content = null;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Error showing promo dialog");
		}
	}

	private async Task ShowTrialDialog()
	{
		try
		{
			while (((UIElement)this).XamlRoot == (XamlRoot)null)
			{
				await Task.Delay(100);
			}
			if (_showTrialTask == null || _showTrialTask.IsCompleted)
			{
				TrialDialogPage page = App.GetInstance<TrialDialogPage>();
				if (!((Page)(object)page == (Page)null) && page.ViewModel != null)
				{
					ContentDialog dialog = new ContentDialog
					{
						XamlRoot = ((UIElement)this).XamlRoot,
						Content = page,
						PrimaryButtonText = Loc.Get("GoPremiumButton"),
						CloseButtonText = Loc.Get("LaterButton"),
						PrimaryButtonCommand = (ICommand)_showSubscribeDialogCommand,
						DefaultButton = (ContentDialogButton)1
					};
					_showTrialTask = dialog.ShowAsyncQueue();
					await _showTrialTask;
					SendDialogView("Trial Dialog");
					((ContentControl)dialog).Content = null;
					_showTrialTask = null;
				}
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Error showing trial dialog");
		}
	}

	private async Task ShowTrafficLimitDialog()
	{
		try
		{
			while (((UIElement)this).XamlRoot == (XamlRoot)null)
			{
				await Task.Delay(100);
			}
			TrafficLimitDialogPage page = App.GetInstance<TrafficLimitDialogPage>();
			ContentDialog dialog = new ContentDialog
			{
				Title = Loc.Get("TrafficLimitDialogTitle"),
				XamlRoot = ((UIElement)this).XamlRoot,
				Content = page,
				PrimaryButtonText = Loc.Get("GoPremiumButton"),
				CloseButtonText = Loc.Get("CloseButton"),
				PrimaryButtonCommand = (ICommand)_showSubscribeDialogCommand,
				DefaultButton = (ContentDialogButton)1
			};
			await dialog.ShowAsyncQueue();
			SendDialogView("Traffic Limit Dialog");
			((ContentControl)dialog).Content = null;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Error showing traffic limit dialog");
		}
	}

	private async Task VisitWebsite()
	{
		try
		{
			Uri uri = new Uri("https://www.ciphravpn.com?utm_source=ciphra_standalone_app");
			TaskAwaiter<bool> taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter<bool>(Launcher.LaunchUriAsync(uri));
			if (!taskAwaiter.IsCompleted)
			{
				await taskAwaiter;
				TaskAwaiter<bool> taskAwaiter2 = default(TaskAwaiter<bool>);
				taskAwaiter = taskAwaiter2;
			}
			taskAwaiter.GetResult();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Failed to open website");
		}
	}

	private async Task VisitSubscriptionsPage()
	{
		try
		{
			Uri uri = new Uri("https://billing.stripe.com/p/login/9B6aEZ4ZAgZcbnKcs5fUQ00");
			TaskAwaiter<bool> taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter<bool>(Launcher.LaunchUriAsync(uri));
			if (!taskAwaiter.IsCompleted)
			{
				await taskAwaiter;
				TaskAwaiter<bool> taskAwaiter2 = default(TaskAwaiter<bool>);
				taskAwaiter = taskAwaiter2;
			}
			taskAwaiter.GetResult();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Failed to open website");
		}
	}

	private void SendDialogView(string dialogName)
	{
		try
		{
			if (!string.IsNullOrEmpty(dialogName))
			{
				_analytics.SendEvent("Dialog View", ("DialogName", dialogName));
				Log.Debug<string>("Dialog view: {DialogName}", dialogName);
			}
		}
		catch (Exception ex)
		{
			Log.Error<string>(ex, "Failed to send dialog view for {DialogName}", dialogName);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri uri = new Uri("ms-appx:///Controls/AppShell.xaml");
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
			AppShellNavigationView = CastExtensions.As<NavigationView>(target);
			AppShellNavigationView.SelectionChanged += AppShellNavigationView_OnSelectionChanged;
			break;
		case 3:
			MainPageNavigationViewItem = CastExtensions.As<NavigationViewItem>(target);
			break;
		case 4:
			LocationsNavigationViewItem = CastExtensions.As<NavigationViewItem>(target);
			break;
		case 5:
			SettingsNavigationViewItem = CastExtensions.As<NavigationViewItem>(target);
			break;
		case 6:
			ViewFrame = CastExtensions.As<Frame>(target);
			break;
		}
		_contentLoaded = true;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
	[DebuggerNonUserCode]
	public IComponentConnector GetBindingConnector(int connectionId, object target)
	{
		return null;
	}
}
