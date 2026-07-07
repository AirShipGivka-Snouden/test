using System;

namespace Ciphra.VPN.Common.App;

public interface IExceptionHandler
{
	void HandleException(Exception ex, string source, bool fatal);
}
