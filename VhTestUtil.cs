using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Toolkit.ApiClients;
using VpnHood.Core.Toolkit.Exceptions;

namespace VpnHood.Core.Toolkit.Utils;

public static class VhTestUtil
{
	public class AssertException : Exception
	{
		public AssertException(string? message = null, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}

	private static async Task<TValue> WaitForValue<TValue>(TValue expectedValue, Func<TValue> valueFactory, TimeSpan timeout, CancellationToken cancellationToken)
	{
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout);
		TValue val = valueFactory();
		while (!cancellationTokenSource.IsCancellationRequested)
		{
			if (object.Equals(expectedValue, val))
			{
				return val;
			}
			await Task.Delay(100, cancellationToken);
			val = valueFactory();
		}
		return val;
	}

	private static async Task<TValue> WaitForValue<TValue>(TValue expectedValue, Func<Task<TValue>> valueFactory, TimeSpan timeout, CancellationToken cancellationToken)
	{
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout);
		TValue val = await valueFactory();
		while (!cancellationTokenSource.IsCancellationRequested)
		{
			if (object.Equals(expectedValue, val))
			{
				return val;
			}
			await Task.Delay(100, cancellationToken);
			val = await valueFactory();
		}
		return val;
	}

	private static void AssertEquals(object? expected, object? actual, string? message)
	{
		if (message == null)
		{
			message = "Unexpected Value";
		}
		if (!object.Equals(expected, actual))
		{
			throw new AssertException($"{message}. Expected: {expected}, Actual: {actual}");
		}
	}

	public static async Task AssertEqualsWait<TValue>(TValue expectedValue, Func<TValue> valueFactory, string? message = null, int timeout = 5000, bool noTimeoutOnDebugger = true, CancellationToken cancellationToken = default(CancellationToken))
	{
		TimeSpan timeout2 = ((noTimeoutOnDebugger && Debugger.IsAttached) ? TimeSpan.FromDays(1) : TimeSpan.FromMilliseconds(timeout));
		AssertEquals(expectedValue, await WaitForValue(expectedValue, valueFactory, timeout2, cancellationToken), message);
	}

	public static async Task AssertEqualsWait<TValue>(TValue expectedValue, Func<Task<TValue>> valueFactory, string? message = null, int timeout = 5000, bool noTimeoutOnDebugger = true, CancellationToken cancellationToken = default(CancellationToken))
	{
		TimeSpan timeout2 = ((noTimeoutOnDebugger && Debugger.IsAttached) ? TimeSpan.FromDays(1) : TimeSpan.FromMilliseconds(timeout));
		AssertEquals(expectedValue, await WaitForValue(expectedValue, valueFactory, timeout2, cancellationToken), message);
	}

	public static async Task AssertEqualsWait<TValue>(TValue expectedValue, Task<TValue> task, string? message = null, int timeout = 5000, bool noTimeoutOnDebugger = true, CancellationToken cancellationToken = default(CancellationToken))
	{
		TimeSpan timeout2 = ((noTimeoutOnDebugger && Debugger.IsAttached) ? TimeSpan.FromDays(1) : TimeSpan.FromMilliseconds(timeout));
		AssertEquals(expectedValue, await WaitForValue(expectedValue, () => task, timeout2, cancellationToken), message);
	}

	public static Task<Exception> AssertApiException(HttpStatusCode expectedStatusCode, Task task, string? message = null)
	{
		return AssertApiException((int)expectedStatusCode, task, message);
	}

	private static void AssertExceptionContains(Exception ex, string? contains)
	{
		if (contains != null && !ex.Message.Contains(contains, StringComparison.OrdinalIgnoreCase))
		{
			throw new Exception("Actual error message does not contain \"" + contains + "\".");
		}
	}

	public static async Task<Exception> AssertApiException(int expectedStatusCode, Task task, string? message = null, string? contains = null)
	{
		try
		{
			await task;
			throw new AssertException($"Expected {expectedStatusCode} but the actual was OK. {message}");
		}
		catch (ApiException ex)
		{
			if (ex.StatusCode != expectedStatusCode)
			{
				throw new Exception($"Expected {expectedStatusCode} but the actual was {ex.StatusCode}. {message}");
			}
			AssertExceptionContains(ex, contains);
			return ex;
		}
	}

	public static Task<Exception> AssertApiException<T>(Task task, string? message = null, string? contains = null)
	{
		return AssertApiException(typeof(T).Name, task, message, contains);
	}

	public static async Task<Exception> AssertApiException(string expectedExceptionType, Task task, string? message = null, string? contains = null)
	{
		try
		{
			await task;
			throw new AssertException("Expected " + expectedExceptionType + " exception but was OK. " + message);
		}
		catch (ApiException ex)
		{
			if (ex.ExceptionTypeName != expectedExceptionType)
			{
				throw new AssertException($"Expected {expectedExceptionType} but was {ex.ExceptionTypeName}. {message}");
			}
			AssertExceptionContains(ex, contains);
			return ex;
		}
		catch (Exception ex2) when (!(ex2 is AssertException))
		{
			if (ex2.GetType().Name != expectedExceptionType)
			{
				throw new AssertException($"Expected {expectedExceptionType} but was {ex2.GetType().Name}. {message}", ex2);
			}
			AssertExceptionContains(ex2, contains);
			return ex2;
		}
	}

	public static async Task<Exception> AssertNotExistsException(Task task, string? message = null, string? contains = null)
	{
		try
		{
			await task;
			throw new AssertException("Expected kind of NotExistsException but was OK. " + message);
		}
		catch (ApiException ex)
		{
			if (ex.ExceptionTypeName != "NotExistsException")
			{
				throw new AssertException($"Expected {"NotExistsException"} but was {ex.ExceptionTypeName}. {message}");
			}
			AssertExceptionContains(ex, contains);
			return ex;
		}
		catch (Exception ex2) when (!(ex2 is AssertException))
		{
			if (!NotExistsException.Is(ex2))
			{
				throw new AssertException($"Expected kind of {"NotExistsException"} but was {ex2.GetType().Name}. {message}", ex2);
			}
			AssertExceptionContains(ex2, contains);
			return ex2;
		}
	}

	public static async Task<Exception> AssertAlreadyExistsException(Task task, string? message = null, string? contains = null)
	{
		try
		{
			await task;
			throw new AssertException("Expected kind of AlreadyExistsException but was OK. " + message);
		}
		catch (ApiException ex)
		{
			if (ex.ExceptionTypeName != "AlreadyExistsException")
			{
				throw new AssertException($"Expected {"AlreadyExistsException"} but was {ex.ExceptionTypeName}. {message}");
			}
			AssertExceptionContains(ex, contains);
			return ex;
		}
		catch (Exception ex2) when (!(ex2 is AssertException))
		{
			if (!AlreadyExistsException.Is(ex2))
			{
				throw new AssertException($"Expected kind of {"AlreadyExistsException"} but was {ex2.GetType().Name}. {message}", ex2);
			}
			AssertExceptionContains(ex2, contains);
			return ex2;
		}
	}
}
