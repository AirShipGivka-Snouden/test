using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Microsoft.UI.Xaml.Markup;
using WinRT;
using WinRT.Ciphra_VPN_WinUIVtableClasses;
using Windows.Foundation.Metadata;

namespace Ciphra.VPN.WinUI.Ciphra_VPN_WinUI_XamlTypeInfo;

[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2605")]
[DebuggerNonUserCode]
[WinRTRuntimeClassName("Microsoft.UI.Xaml.Markup.IXamlMetadataProvider")]
[WinRTExposedType(typeof(Ciphra_VPN_WinUI_Ciphra_VPN_WinUI_XamlTypeInfo_XamlMetaDataProviderWinRTTypeDetails))]
public sealed class XamlMetaDataProvider : IXamlMetadataProvider
{
	private XamlTypeInfoProvider _provider = null;

	private XamlTypeInfoProvider Provider
	{
		get
		{
			if (_provider == null)
			{
				_provider = new XamlTypeInfoProvider();
			}
			return _provider;
		}
	}

	[DefaultOverload]
	public IXamlType GetXamlType(Type type)
	{
		return Provider.GetXamlTypeByType(type);
	}

	public IXamlType GetXamlType(string fullName)
	{
		return Provider.GetXamlTypeByName(fullName);
	}

	public XmlnsDefinition[] GetXmlnsDefinitions()
	{
		return (XmlnsDefinition[])(object)new XmlnsDefinition[0];
	}
}
