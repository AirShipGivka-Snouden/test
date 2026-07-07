using System;
using System.Net.Sockets;
using VpnHood.Core.Toolkit.Exceptions;

namespace VpnHood.Core.Toolkit.ApiClients;

public static class ExceptionExtensions
{
	extension(Exception ex)
	{
		public ApiError ToApiError()
		{
			if (ex is ApiException ex2)
			{
				return ex2.ToApiError();
			}
			Type exceptionType = ex.GetExceptionType();
			string text = ex.Message;
			if (string.IsNullOrWhiteSpace(text))
			{
				text = ex.InnerException?.Message;
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				text = exceptionType.FullName;
			}
			ApiError apiError = new ApiError
			{
				TypeName = exceptionType.Name,
				TypeFullName = exceptionType.FullName,
				Message = (text ?? ""),
				InnerMessage = (string.IsNullOrWhiteSpace(ex.Message) ? null : ex.InnerException?.Message)
			};
			if (ex is SocketException ex3)
			{
				apiError.Data["SocketErrorCode"] = ex3.SocketErrorCode.ToString();
			}
			apiError.ImportData(ex.Data);
			return apiError;
		}

		private Type GetExceptionType()
		{
			if (AlreadyExistsException.Is(ex))
			{
				return typeof(AlreadyExistsException);
			}
			if (NotExistsException.Is(ex))
			{
				return typeof(NotExistsException);
			}
			return ex.GetType();
		}
	}
}
