using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace VpnHood.Core.Toolkit.Logging;

public class FileLoggerProvider(string filePath, bool includeScopes = true, bool autoFlush = false) : ILoggerProvider, IDisposable
{
	private readonly ConcurrentDictionary<string, FileLogger> _loggers = new ConcurrentDictionary<string, FileLogger>();

	public ILogger CreateLogger(string categoryName)
	{
		return _loggers.GetOrAdd(categoryName, (string name) => new FileLogger(filePath, includeScopes, autoFlush, name));
	}

	public void Dispose()
	{
		foreach (FileLogger value in _loggers.Values)
		{
			value.Dispose();
		}
		_loggers.Clear();
	}
}
