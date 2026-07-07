using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace VpnHood.Core.Toolkit.Logging;

public class SyncLogger(ILogger logger) : ILogger
{
	private readonly Lock _lock = new Lock();

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull
	{
		using (_lock.EnterScope())
		{
			return logger.BeginScope(state);
		}
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		using (_lock.EnterScope())
		{
			return logger.IsEnabled(logLevel);
		}
	}

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		using (_lock.EnterScope())
		{
			try
			{
				logger.Log(logLevel, eventId, state, exception, formatter);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Logger error! Could not write into logger. Error: " + ex.Message);
			}
		}
	}
}
