using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System;
using ABI.System.ComponentModel;
using ABI.System.Windows.Input;

namespace WinRT.Ciphra_VPN_WinUIGenericHelpers;

internal static class GlobalVtableLookup
{
	[ModuleInitializer]
	internal static void InitializeGlobalVtableLookup()
	{
		ComWrappersSupport.RegisterTypeComInterfaceEntriesLookup((Func<Type, ComWrappers.ComInterfaceEntry[]>)LookupVtableEntries);
		ComWrappersSupport.RegisterTypeRuntimeClassNameLookup((Func<Type, string>)LookupRuntimeClassName);
	}

	private static ComWrappers.ComInterfaceEntry[] LookupVtableEntries(Type type)
	{
		string text = type.ToString();
		switch (text)
		{
		default:
			if (!(text == "Ciphra.VPN.Common.ViewModels.DialogViewModels.TrialDialogViewModel"))
			{
				switch (text)
				{
				default:
					if (!(text == "System.Drawing.Icon"))
					{
						if (text == "ReactiveUI.ReactiveCommand`2[System.Reactive.Unit,System.Reactive.Unit]")
						{
							return new ComWrappers.ComInterfaceEntry[2]
							{
								new ComWrappers.ComInterfaceEntry
								{
									IID = IDisposableMethods.IID,
									Vtable = IDisposableMethods.AbiToProjectionVftablePtr
								},
								new ComWrappers.ComInterfaceEntry
								{
									IID = ICommandMethods.IID,
									Vtable = ICommandMethods.AbiToProjectionVftablePtr
								}
							};
						}
						return null;
					}
					goto case "VpnHood.Core.VpnAdapters.WinTun.WinTunVpnAdapter";
				case "VpnHood.Core.VpnAdapters.WinTun.WinTunVpnAdapter":
				case "System.IO.Pipes.NamedPipeServerStream":
				case "H.NotifyIcon.Core.TrayIcon":
				case "System.Threading.Tasks.Task`1[Microsoft.UI.Xaml.Controls.ContentDialogResult]":
					return new ComWrappers.ComInterfaceEntry[1]
					{
						new ComWrappers.ComInterfaceEntry
						{
							IID = IDisposableMethods.IID,
							Vtable = IDisposableMethods.AbiToProjectionVftablePtr
						}
					};
				}
			}
			goto case "Ciphra.VPN.Common.ViewModels.SplitTunnelPageViewModel";
		case "Ciphra.VPN.Common.ViewModels.SplitTunnelPageViewModel":
		case "Ciphra.VPN.Common.ViewModels.MainPageViewModel":
		case "Ciphra.VPN.Common.ViewModels.DialogViewModels.TrafficLimitDialogViewModel":
		case "ReactiveUI.ReactiveObject":
		case "Ciphra.VPN.Common.ViewModels.AppShellViewModel":
		case "Ciphra.VPN.Common.ViewModels.LocationsPageViewModel":
		case "Ciphra.VPN.Common.ViewModels.DialogViewModels.AuthDialogViewModel":
		case "Ciphra.VPN.Common.ViewModels.SettingsPageViewModel":
		case "Ciphra.VPN.Common.ViewModels.DialogViewModels.ErrorDialogViewModel":
			return new ComWrappers.ComInterfaceEntry[1]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = INotifyPropertyChangedMethods.IID,
					Vtable = INotifyPropertyChangedMethods.AbiToProjectionVftablePtr
				}
			};
		}
	}

	private static string LookupRuntimeClassName(Type type)
	{
		string text = type.ToString();
		switch (text)
		{
		default:
			if (!(text == "Ciphra.VPN.Common.ViewModels.DialogViewModels.TrialDialogViewModel"))
			{
				switch (text)
				{
				default:
					if (!(text == "System.Drawing.Icon"))
					{
						return null;
					}
					goto case "VpnHood.Core.VpnAdapters.WinTun.WinTunVpnAdapter";
				case "VpnHood.Core.VpnAdapters.WinTun.WinTunVpnAdapter":
				case "System.IO.Pipes.NamedPipeServerStream":
				case "ReactiveUI.ReactiveCommand`2[System.Reactive.Unit,System.Reactive.Unit]":
				case "H.NotifyIcon.Core.TrayIcon":
				case "System.Threading.Tasks.Task`1[Microsoft.UI.Xaml.Controls.ContentDialogResult]":
					return "Windows.Foundation.IClosable";
				}
			}
			goto case "Ciphra.VPN.Common.ViewModels.SplitTunnelPageViewModel";
		case "Ciphra.VPN.Common.ViewModels.SplitTunnelPageViewModel":
		case "Ciphra.VPN.Common.ViewModels.MainPageViewModel":
		case "Ciphra.VPN.Common.ViewModels.DialogViewModels.TrafficLimitDialogViewModel":
		case "ReactiveUI.ReactiveObject":
		case "Ciphra.VPN.Common.ViewModels.AppShellViewModel":
		case "Ciphra.VPN.Common.ViewModels.LocationsPageViewModel":
		case "Ciphra.VPN.Common.ViewModels.DialogViewModels.AuthDialogViewModel":
		case "Ciphra.VPN.Common.ViewModels.SettingsPageViewModel":
		case "Ciphra.VPN.Common.ViewModels.DialogViewModels.ErrorDialogViewModel":
			return "Microsoft.UI.Xaml.Data.INotifyPropertyChanged";
		}
	}
}
