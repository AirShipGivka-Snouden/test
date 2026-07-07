using System;
using System.Runtime.InteropServices;
using ABI.Microsoft.UI.Xaml.Markup;

namespace WinRT.Ciphra_VPN_WinUIVtableClasses;

internal sealed class Ciphra_VPN_WinUI_Ciphra_VPN_WinUI_XamlTypeInfo_XamlMemberWinRTTypeDetails : IWinRTExposedTypeDetails
{
	public unsafe ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
	{
		return new ComWrappers.ComInterfaceEntry[1]
		{
			new ComWrappers.ComInterfaceEntry
			{
				IID = *(Guid*)IXamlMemberMethods.IID,
				Vtable = IXamlMemberMethods.AbiToProjectionVftablePtr
			}
		};
	}
}
