using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace VpnHood.Core.Toolkit.Logging;

public class FileLogger(string filePath, bool includeScopes = true, bool autoFlush = false, string? categoryName = null) : TextLogger(includeScopes, categoryName), IDisposable
{
	private const int DefaultBufferSize = 1024;

	private readonly Lock _lock = new Lock();

	private readonly StreamWriter _streamWriter = new StreamWriter(new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read), Encoding.UTF8, 1024);

	public override void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		string value = FormatLog(logLevel, eventId, state, exception, formatter);
		using (_lock.EnterScope())
		{
			try
			{
				_streamWriter.WriteLine(value);
				if (autoFlush || logLevel >= LogLevel.Error)
				{
					_streamWriter.Flush();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: Could not write the log. " + ex.Message);
			}
		}
	}

	public void Dispose()
	{
		using (_lock.EnterScope())
		{
			_streamWriter.Dispose();
		}
	}
}
