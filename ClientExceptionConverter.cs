using System;
using VpnHood.Core.Client.Abstractions.Exceptions;
using VpnHood.Core.Client.VpnServices.Abstractions.Exceptions;
using VpnHood.Core.Common.Exceptions;
using VpnHood.Core.Toolkit.ApiClients;

namespace VpnHood.Core.Client.VpnServices.Abstractions;

public static class ClientExceptionConverter
{
	public static Exception? ApiErrorToException(ApiError apiError)
	{
		Exception ex = null;
		if (apiError.Is<MaintenanceException>())
		{
			ex = new MaintenanceException();
		}
		if (apiError.Is<SessionException>())
		{
			ex = new SessionException(apiError);
		}
		if (apiError.Is<UiContextNotAvailableException>())
		{
			ex = new UiContextNotAvailableException();
		}
		if (apiError.Is<AlwaysOnNotAllowedException>())
		{
			ex = new AlwaysOnNotAllowedException(apiError.Message);
		}
		if (apiError.Is<VpnServiceRevokedException>())
		{
			ex = new VpnServiceRevokedException(apiError.Message);
		}
		if (apiError.Is<ConnectionTimeoutException>())
		{
			ex = new ConnectionTimeoutException(apiError.Message);
		}
		if (apiError.Is<UnreachableServerException>())
		{
			ex = new UnreachableServerException(apiError.Message);
		}
		if (apiError.Is<UnreachableProxyServerException>())
		{
			ex = new UnreachableProxyServerException(apiError.Message);
		}
		if (apiError.Is<UnreachableServerLocationException>())
		{
			ex = new UnreachableServerLocationException(apiError.Message);
		}
		if (ex != null)
		{
			apiError.ExportData(ex.Data);
		}
		return ex;
	}
}
