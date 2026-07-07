using Microsoft.Windows.ApplicationModel.Resources;

namespace Ciphra.VPN.WinUI.Services;

internal static class Loc
{
	private static readonly ResourceLoader Loader = new ResourceLoader();

	public static string Get(string key)
	{
		return Loader.GetString(key);
	}
}
