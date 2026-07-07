using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace VpnHood.Core.Toolkit.Logging;

public class VhConsoleLogger(bool includeScopes = true, bool singleLine = true, string? categoryName = null) : TextLogger(includeScopes, categoryName)
{
	private static bool? _isColorSupported;

	private readonly Lock _lock = new Lock();

	private static bool IsColorSupported
	{
		get
		{
			if (!_isColorSupported.HasValue)
			{
				try
				{
					_ = Console.ForegroundColor;
					_isColorSupported = true;
				}
				catch
				{
					_isColorSupported = false;
				}
			}
			return _isColorSupported.Value;
		}
	}

	public override void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		string text = FormatLog(logLevel, eventId, state, exception, formatter);
		if (singleLine)
		{
			text = text.Replace("\n", " ").Replace("\r", "").Trim();
		}
		using (_lock.EnterScope())
		{
			if (IsColorSupported)
			{
				ConsoleColor foregroundColor = Console.ForegroundColor;
				Console.ForegroundColor = GetColor(logLevel);
				Console.WriteLine(text);
				Console.ForegroundColor = foregroundColor;
			}
			else
			{
				Console.WriteLine(text);
			}
		}
	}

	public ConsoleColor GetColor(LogLevel logLevel)
	{
		return logLevel switch
		{
			LogLevel.Trace => ConsoleColor.Gray, 
			LogLevel.Debug => ConsoleColor.Gray, 
			LogLevel.Information => ConsoleColor.White, 
			LogLevel.Warning => ConsoleColor.Yellow, 
			LogLevel.Error => ConsoleColor.Red, 
			LogLevel.Critical => ConsoleColor.DarkRed, 
			_ => ConsoleColor.White, 
		};
	}
}
