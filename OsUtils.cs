using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;

namespace VpnHood.Core.Toolkit.Utils;

public static class OsUtils
{
	public static string ExecuteCommand(string fileName, string command)
	{
		VhLogger.Instance.LogDebug("Executing: " + fileName + " " + command);
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = fileName,
			Arguments = command,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};
		using Process process = new Process();
		process.StartInfo = startInfo;
		process.Start();
		string message = process.StandardError.ReadToEnd();
		string result = process.StandardOutput.ReadToEnd();
		process.WaitForExit();
		if (process.ExitCode != 0)
		{
			throw new ExternalException(message, process.ExitCode);
		}
		return result;
	}

	public static async Task<string> ExecuteCommandAsync(string fileName, string command, CancellationToken cancellationToken)
	{
		VhLogger.Instance.LogDebug("Executing: " + fileName + " " + command);
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = fileName,
			Arguments = command,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};
		using Process process = new Process();
		process.StartInfo = startInfo;
		process.Start();
		string error = await process.StandardError.ReadToEndAsync(cancellationToken);
		string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
		await WaitForExitAsync(process, cancellationToken);
		if (process.ExitCode != 0)
		{
			error += $". Command: {fileName} {command}.";
			throw new ExternalException(error, process.ExitCode);
		}
		return output;
	}

	private static async Task WaitForExitAsync(Process process, CancellationToken cancellationToken)
	{
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		process.Exited += delegate
		{
			tcs.TrySetResult(result: true);
		};
		process.EnableRaisingEvents = true;
		await tcs.Task.WaitAsync(cancellationToken);
	}
}
