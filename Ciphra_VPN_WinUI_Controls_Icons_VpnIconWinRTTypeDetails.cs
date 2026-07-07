using System;
using System.Runtime.InteropServices;
using ABI.Microsoft.UI.Composition;
using ABI.Microsoft.UI.Xaml;
using ABI.Microsoft.UI.Xaml.Controls;
using ABI.Microsoft.UI.Xaml.Markup;

namespace WinRT.Ciphra_VPN_WinUIVtableClasses;

internal sealed class Ciphra_VPN_WinUI_Controls_Icons_VpnIconWinRTTypeDetails : IWinRTExposedTypeDetails
{
	public unsafe ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
	{
		return new ComWrappers.ComInterfaceEntry[7]
		{
			new ComWrappers.ComInterfaceEntry
			{
				IID = *(Guid*)IUIElementOverridesMethods.IID,
				Vtable = IUIElementOverridesMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = *(Guid*)IAnimationObjectMethods.IID,
				Vtable = IAnimationObjectMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = *(Guid*)IVisualElementMethods.IID,
				Vtable = IVisualElementMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = *(Guid*)IVisualElement2Methods.IID,
				Vtable = IVisualElement2Methods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = *(Guid*)IFrameworkElementOverridesMethods.IID,
				Vtable = IFrameworkElementOverridesMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = *(Guid*)IControlOverridesMethods.IID,
				Vtable = IControlOverridesMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = *(Guid*)IComponentConnectorMethods.IID,
				Vtable = IComponentConnectorMethods.AbiToProjectionVftablePtr
			}
		};
	}
}
