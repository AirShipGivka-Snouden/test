using Ciphra.VPN.Common.App;

namespace Ciphra.VPN.WinUI.Services;

public class WinUiImageUriFormatProvider : IImageUriFormatProvider
{
	public string GetImageUri(string imageName)
	{
		return "ms-appx:///Assets/Flags/" + imageName.ToLowerInvariant() + ".png";
	}
}
