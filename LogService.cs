using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace VpnHood.Core.Toolkit.Logging;

public class LogService(string logFilePath) : IDisposable
{
	private ILogger? _logger;

	private readonly List<ILoggerProvider> _loggerProviders = new List<ILoggerProvider>();

	private int _isDisposed;

	private readonly Lock _isStoppingLock = new Lock();

	public string LogFilePath { get; } = logFilePath;

	public string[] LogEvents { get; private set; } = Array.Empty<string>();

	public bool Exists => File.Exists(LogFilePath);

	public bool IsStarted => _logger != null;

	public void Start(LogServiceOptions options, bool deleteOldReport = true)
	{
		ObjectDisposedException.ThrowIf(_isDisposed == 1, this);
		Stop();
		bool isAnonymousMode = ((options.LogAnonymous ?? true) ? true : false);
		VhLogger.IsAnonymousMode = isAnonymousMode;
		VhLogger.Instance = (_logger = CreateLogger(options, deleteOldReport));
		LogEvents = options.LogEventNames;
		VhLogger.MinLogLevel = options.MinLogLevel;
		if (options.MinLogLevel == LogLevel.Trace && !options.LogEventNames.Contains("*"))
		{
			LogEvents = LogEvents.Concat(new _003C_003Ez__ReadOnlySingleElementList<string>("*")).ToArray();
		}
		VhLogger.Instance.LogDebug("LogService has started. Options: {Options}", JsonSerializer.Serialize(options));
	}

	public void Stop()
	{
		using (_isStoppingLock.EnterScope())
		{
			VhLogger.Instance.LogDebug("LogService is stopping...");
			VhLogger.Instance = VhLogger.CreateConsoleLogger();
			foreach (ILoggerProvider loggerProvider in _loggerProviders)
			{
				loggerProvider.Dispose();
			}
			_loggerProviders.Clear();
			_logger = null;
		}
	}

	private ILogger CreateLogger(LogServiceOptions logServiceOptions, bool deleteOldReport)
	{
		using ILoggerFactory loggerFactory = CreateLoggerFactory(logServiceOptions, deleteOldReport);
		return new FilterLogger(loggerFactory.CreateLogger(logServiceOptions.CategoryName ?? ""), delegate(LogLevel _, EventId eventId)
		{
			if (logServiceOptions.LogEventNames.Contains<string>(eventId.Name, StringComparer.OrdinalIgnoreCase))
			{
				return true;
			}
			return eventId.Id == 0 || logServiceOptions.LogEventNames.Contains("*");
		});
	}

	private ILoggerFactory CreateLoggerFactory(LogServiceOptions logServiceOptions, bool deleteOldReport)
	{
		if (deleteOldReport && File.Exists(LogFilePath))
		{
			File.Delete(LogFilePath);
		}
		return LoggerFactory.Create(delegate(ILoggingBuilder builder)
		{
			if (logServiceOptions.LogToConsole)
			{
				VhConsoleLoggerProvider vhConsoleLoggerProvider = new VhConsoleLoggerProvider(includeScopes: true, logServiceOptions.SingleLineConsole);
				_loggerProviders.Add(vhConsoleLoggerProvider);
				builder.AddProvider(vhConsoleLoggerProvider);
			}
			if (logServiceOptions.LogToFile)
			{
				FileLoggerProvider fileLoggerProvider = new FileLoggerProvider(LogFilePath, includeScopes: true, logServiceOptions.AutoFlush);
				_loggerProviders.Add(fileLoggerProvider);
				builder.AddProvider(fileLoggerProvider);
			}
			builder.SetMinimumLevel(logServiceOptions.MinLogLevel);
		});
	}

	public static IEnumerable<string> GetLogEventNames(string[] currentNames, string debugCommand)
	{
		return currentNames.Concat(GetLogEventNames(debugCommand)).Distinct();
	}

	public static IEnumerable<string> GetLogEventNames(string debugCommand)
	{
		List<string> list = new List<string> { "*" };
		foreach (string item in from x in debugCommand.Split(' ')
			where x.Contains("/log:", StringComparison.OrdinalIgnoreCase)
			select x)
		{
			list.AddRange(item.Substring(5).Split(','));
		}
		return list.Distinct();
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, 1) != 1)
		{
			Stop();
		}
	}
}
