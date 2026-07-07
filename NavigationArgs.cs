namespace Ciphra.VPN.Common.App;

public class NavigationArgs
{
	public NavigationIntent Intent { get; }

	public object? Parameter { get; }

	public NavigationArgs(NavigationIntent intent, object? parameter)
	{
		Parameter = parameter;
		Intent = intent;
	}
}
