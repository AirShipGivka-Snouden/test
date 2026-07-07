using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VpnHood.Core.Toolkit.Utils;

public static class FileUtils
{
	public static async Task WriteAllTextRetryAsync(string filePath, string content, TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken))
	{
		DateTime startTime = DateTime.Now;
		while (true)
		{
			try
			{
				await File.WriteAllTextAsync(filePath, content, cancellationToken);
				break;
			}
			catch (IOException) when (DateTime.Now - startTime < timeout)
			{
				await Task.Delay(100, cancellationToken);
			}
		}
	}

	public static async Task<string> ReadAllTextAsync(string filePath, TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken))
	{
		DateTime startTime = DateTime.Now;
		while (true)
		{
			try
			{
				string result;
				await using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					using StreamReader reader = new StreamReader(stream);
					result = await reader.ReadToEndAsync(cancellationToken);
				}
				return result;
			}
			catch (IOException) when (DateTime.Now - startTime < timeout)
			{
				await Task.Delay(100, cancellationToken);
			}
		}
	}
}
