using System;
using System.Collections.Generic;

namespace VpnHood.Core.Toolkit.ApiClients;

public sealed class ApiException : Exception
{
	public int StatusCode { get; }

	public string? Response { get; }

	public string? ExceptionTypeName { get; }

	public string? ExceptionTypeFullName { get; }

	public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

	private static string BuildMessage(string message, int statusCode, string? response)
	{
		if (response == null || !ApiError.TryParse(response, out ApiError apiError))
		{
			return $"{message}\n\nStatus: {statusCode}\nResponse: \n{response?.Substring(0, Math.Min(512, response.Length))}";
		}
		return apiError.Message;
	}

	public ApiException(string message, int statusCode, string? response, IReadOnlyDictionary<string, IEnumerable<string>>? headers, Exception? innerException)
		: base(BuildMessage(message, statusCode, response), innerException)
	{
		StatusCode = statusCode;
		Response = response;
		Headers = headers ?? new Dictionary<string, IEnumerable<string>>();
		if (response == null || !ApiError.TryParse(response, out ApiError apiError))
		{
			return;
		}
		foreach (KeyValuePair<string, string> datum in apiError.Data)
		{
			Data.Add(datum.Key, datum.Value);
		}
		ExceptionTypeName = apiError.TypeName;
		ExceptionTypeFullName = apiError.TypeFullName;
	}

	public ApiException(ApiError apiError)
		: base(apiError.Message, new Exception(apiError.InnerMessage ?? ""))
	{
		StatusCode = 400;
		ExceptionTypeFullName = apiError.TypeFullName;
		ExceptionTypeName = apiError.TypeName;
		Headers = new Dictionary<string, IEnumerable<string>>();
		apiError.ExportData(Data);
	}

	public ApiError ToApiError()
	{
		ApiError obj = new ApiError
		{
			TypeName = (ExceptionTypeName ?? GetType().Name),
			TypeFullName = (ExceptionTypeFullName ?? GetType().FullName),
			Message = Message,
			InnerMessage = base.InnerException?.Message
		};
		obj.ImportData(Data);
		obj.Data.TryAdd("InnerStatusCode", StatusCode.ToString());
		return obj;
	}

	public override string ToString()
	{
		return "HTTP Response: \n\n" + Response + "\n\n" + base.ToString();
	}

	public bool Is<T>()
	{
		return ExceptionTypeName == typeof(T).Name;
	}
}
