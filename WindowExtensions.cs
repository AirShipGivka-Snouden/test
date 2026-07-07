using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using Windows.Graphics;

namespace Ciphra.VPN.WinUI.Extensions;

public static class WindowExtensions
{
	public static void BringToFront(this Window window)
	{
		try
		{
			if (!(((window != null) ? window.AppWindow : null) == (AppWindow)null))
			{
				window.AppWindow.Show(true);
				AppWindowPresenter presenter = window.AppWindow.Presenter;
				OverlappedPresenter val = (OverlappedPresenter)(object)((presenter is OverlappedPresenter) ? presenter : null);
				if (val != (OverlappedPresenter)null)
				{
					val.IsAlwaysOnTop = true;
					val.IsAlwaysOnTop = false;
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static void SetDialogProperties(this Window window, int width, int height, bool resizable = false)
	{
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!(((window != null) ? window.AppWindow : null) == (AppWindow)null))
			{
				double scaleFactor = ScaleHelper.GetScaleFactor();
				double num = 100.0;
				double num2 = 100.0;
				if (!App.MainWindowClosed && (Window)(object)App.MainWindow != (Window)null)
				{
					Rect bounds = ((Window)App.MainWindow).Bounds;
					num = ((Rect)(ref bounds)).X + (((Rect)(ref bounds)).Width - (double)width / scaleFactor) / 2.0;
					num2 = ((Rect)(ref bounds)).Y + (((Rect)(ref bounds)).Height - (double)height / scaleFactor) / 2.0;
				}
				window.AppWindow.MoveAndResize(new RectInt32((int)(num * scaleFactor), (int)(num2 * scaleFactor), (int)((double)width * scaleFactor), (int)((double)height * scaleFactor)));
				AppWindowPresenter presenter = window.AppWindow.Presenter;
				OverlappedPresenter val = (OverlappedPresenter)(object)((presenter is OverlappedPresenter) ? presenter : null);
				if (val != (OverlappedPresenter)null)
				{
					val.IsResizable = resizable;
					val.IsMaximizable = resizable;
					val.IsMinimizable = true;
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
