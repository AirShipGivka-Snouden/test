using System;

namespace Ciphra.VPN.Common.App;

public interface IExceptionBroadcaster
{
	IObservable<(Exception ex, string source)> ExceptionObservable { get; }
}
