using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace VpnHood.Core.Toolkit.Logging;

public abstract class TextLogger(bool includeScopes, string? categoryName) : ILogger
{
	private readonly LoggerExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

	public IDisposable BeginScope<TState>(TState state) where TState : notnull
	{
		return _scopeProvider.Push(state);
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		return true;
	}

	public abstract void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter);

	protected void WriteScopeInformation(StringBuilder stringBuilder)
	{
		int length = stringBuilder.Length;
		_scopeProvider.ForEachScope(delegate(object? scope, (StringBuilder stringBuilder, int initialLength) state)
		{
			(StringBuilder stringBuilder, int initialLength) tuple = state;
			StringBuilder item = tuple.stringBuilder;
			bool flag = tuple.initialLength == item.Length;
			item.Append(flag ? " " : " => ").Append(scope);
		}, (stringBuilder, length));
	}

	protected virtual string FormatLog<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		string value = DateTime.Now.ToString("HH:mm:ss.ffff");
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2);
		handler.AppendFormatted(value);
		handler.AppendLiteral(" | ");
		stringBuilder3.Append(ref handler);
		if (!string.IsNullOrEmpty(categoryName))
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2);
			handler.AppendFormatted(categoryName);
			handler.AppendLiteral(" | ");
			stringBuilder4.Append(ref handler);
		}
		if (includeScopes)
		{
			stringBuilder.Append(logLevel.ToString().Substring(0, 4) + " |");
			WriteScopeInformation(stringBuilder);
			stringBuilder.AppendLine();
		}
		if (!string.IsNullOrEmpty(eventId.Name))
		{
			stringBuilder.Append(eventId.Name);
			stringBuilder.Append(" | ");
		}
		string text = formatter(state, exception);
		if (exception != null)
		{
			text = text + "\r\nException: " + exception;
		}
		stringBuilder.Append(text);
		stringBuilder.AppendLine();
		return stringBuilder.ToString();
	}
}
