using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace VpnHood.Core.Toolkit.Logging;

public class VhConsoleLoggerProvider(bool includeScopes = true, bool singleLine = true) : ILoggerProvider, IDisposable
{
	private readonly ConcurrentDictionary<string, VhConsoleLogger> _loggers = new ConcurrentDictionary<string, VhConsoleLogger>();

	public ILogger CreateLogger(string categoryName)
	{
		return _loggers.GetOrAdd(categoryName, (string name) => new VhConsoleLogger(includeScopes, singleLine, name));
	}

	public void Dispose()
	{
		_loggers.Clear();
	}
}
