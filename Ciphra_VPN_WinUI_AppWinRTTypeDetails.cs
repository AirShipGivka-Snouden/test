using System;
using System.Runtime.InteropServices;
using ABI.Microsoft.UI.Xaml;
using ABI.Microsoft.UI.Xaml.Markup;

namespace WinRT.Ciphra_VPN_WinUIVtableClasses;

internal sealed class Ciphra_VPN_WinUI_AppWinRTTypeDetails : IWinRTExposedTypeDetails
{
	public unsafe ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
	{
		return new ComWrappers.ComInterfaceEntry[2]
		{
			new ComWrappers.ComInterfaceEntry
			{
				IID = *(Guid*)IApplicationOverridesMethods.IID,
				Vtable = IApplicationOverridesMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = *(Guid*)IXamlMetadataProviderMethods.IID,
				Vtable = IXamlMetadataProviderMethods.AbiToProjectionVftablePtr
			}
		};
	}
}
