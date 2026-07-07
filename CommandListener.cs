using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Common;

public class CommandListener(string commandFilePath) : IDisposable
{
	private FileSystemWatcher? _fileSystemWatcher;

	public bool IsStarted => _fileSystemWatcher != null;

	public event EventHandler<CommandReceivedEventArgs>? CommandReceived;

	public void Start()
	{
		if (IsStarted)
		{
			throw new Exception("CommandListener is already started!");
		}
		try
		{
			if (File.Exists(commandFilePath))
			{
				File.Delete(commandFilePath);
			}
			string directoryName = Path.GetDirectoryName(commandFilePath);
			Directory.CreateDirectory(directoryName);
			_fileSystemWatcher = new FileSystemWatcher
			{
				Path = directoryName,
				NotifyFilter = NotifyFilters.LastWrite,
				Filter = Path.GetFileName(commandFilePath),
				IncludeSubdirectories = false,
				EnableRaisingEvents = true
			};
			_fileSystemWatcher.Changed += delegate(object _, FileSystemEventArgs e)
			{
				string commandLine = ReadAllTextAndWait(e.FullPath, 5L);
				OnCommand(VhUtils.ParseArguments(commandLine).ToArray());
			};
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogWarning(exception, "Could not start CommandListener! ");
		}
	}

	public void Stop()
	{
		_fileSystemWatcher?.Dispose();
		_fileSystemWatcher = null;
	}

	private static string ReadAllTextAndWait(string fileName, long retry = 5L)
	{
		Exception ex = new Exception("Could not read " + fileName);
		for (int i = 0; i < retry; i++)
		{
			try
			{
				return File.ReadAllText(fileName);
			}
			catch (IOException ex2)
			{
				ex = ex2;
				Thread.Sleep(500);
			}
		}
		throw ex;
	}

	public void SendCommand(string command)
	{
		VhLogger.Instance.LogInformation("Broadcasting a command. Command: {Command}", command);
		Directory.CreateDirectory(Path.GetDirectoryName(commandFilePath));
		File.WriteAllText(commandFilePath, command);
	}

	public void TrySendCommand(string command)
	{
		try
		{
			SendCommand(command);
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogError(exception, "Could not send command.");
		}
	}

	protected void OnCommand(string[] args)
	{
		this.CommandReceived?.Invoke(this, new CommandReceivedEventArgs(args));
	}

	public void Dispose()
	{
		_fileSystemWatcher?.Dispose();
	}
}
