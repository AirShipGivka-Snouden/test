using System;

namespace Ciphra.VPN.Common.App;

public interface INavigationService
{
	IObservable<NavigationArgs> NavigatedObservable { get; }

	NavigationArgs NavigateTo(NavigationIntent intent, object parameter = null);
}
