using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Ciphra.VPN.Common.App;

public class NavigationService : INavigationService
{
	private readonly Subject<NavigationArgs> _navigatedSubject = new Subject<NavigationArgs>();

	public IObservable<NavigationArgs> NavigatedObservable => Observable.AsObservable<NavigationArgs>((IObservable<NavigationArgs>)_navigatedSubject);

	public NavigationArgs NavigateTo(NavigationIntent intent, object parameter = null)
	{
		NavigationArgs navigationArgs = new NavigationArgs(intent, parameter);
		((SubjectBase<NavigationArgs>)(object)_navigatedSubject).OnNext(navigationArgs);
		return navigationArgs;
	}
}
