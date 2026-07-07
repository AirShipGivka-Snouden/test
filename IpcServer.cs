using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;

namespace Ciphra.VPN.WinUI.Services;

public class IpcServer
{
	private CancellationTokenSource _ipcCts = new CancellationTokenSource();

	private Task? _ipcLoop;

	public static async Task SendIpcAsync(IpcMessage msg, int timeoutMs = 800)
	{
		try
		{
			using NamedPipeClientStream client = new NamedPipeClientStream(".", "CiphraVpn.IpcPipe", PipeDirection.Out, PipeOptions.Asynchronous);
			using CancellationTokenSource cts = new CancellationTokenSource(timeoutMs);
			await client.ConnectAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			string json = JsonSerializer.Serialize(msg);
			byte[] bytes = Encoding.UTF8.GetBytes(json);
			using BinaryWriter bw = new BinaryWriter(client, Encoding.UTF8, leaveOpen: true);
			bw.Write(bytes.Length);
			bw.Write(bytes);
			await client.FlushAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Error(ex2, "Failed to send ipc message");
		}
	}

	public void Start()
	{
		_ipcLoop = Task.Run(async delegate
		{
			try
			{
				while (!_ipcCts.IsCancellationRequested)
				{
					try
					{
						using (NamedPipeServerStream server = new NamedPipeServerStream("CiphraVpn.IpcPipe", PipeDirection.In, 10, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
						{
							await server.WaitForConnectionAsync(_ipcCts.Token).ConfigureAwait(continueOnCapturedContext: false);
							using CancellationTokenSource readCts = CancellationTokenSource.CreateLinkedTokenSource(_ipcCts.Token);
							readCts.CancelAfter(TimeSpan.FromSeconds(5L));
							byte[] lengthBuf = new byte[4];
							await ReadExactAsync(server, lengthBuf, readCts.Token).ConfigureAwait(continueOnCapturedContext: false);
							int len = BitConverter.ToInt32(lengthBuf, 0);
							if (len > 0 && len <= 65536)
							{
								byte[] bytes = new byte[len];
								await ReadExactAsync(server, bytes, readCts.Token).ConfigureAwait(continueOnCapturedContext: false);
								string json = Encoding.UTF8.GetString(bytes);
								IpcMessage msg = JsonSerializer.Deserialize<IpcMessage>(json);
								Log.Information<string, bool?>("Received IPC message: {Kind}, Flag: {Flag}", msg?.Kind, msg?.Flag);
								if (msg?.Kind == "SetAdminFlag" && msg.Flag == true)
								{
									Environment.Exit(0);
								}
								if (msg?.Kind == "ShowWindow")
								{
									Log.Debug("Restoring main window via IPC message");
									if ((Window)(object)App.MainWindow != (Window)null)
									{
										App.MainWindow.Restore.Execute(Unit.Default);
									}
								}
								if (msg?.Kind == "OAuthCallback" && !string.IsNullOrWhiteSpace(msg.Payload))
								{
									Log.Information("Dispatching OAuth callback from IPC");
									string payload = msg.Payload;
									MainWindow? mainWindow = App.MainWindow;
									DispatcherQueue queue = ((mainWindow != null) ? ((Window)mainWindow).DispatcherQueue : null);
									if (queue != (DispatcherQueue)null)
									{
										queue.TryEnqueue((DispatcherQueueHandler)delegate
										{
											DispatchCallbackOnUiThread(payload);
										});
									}
									else
									{
										DispatchCallbackOnUiThread(payload);
									}
								}
								goto end_IL_011a;
							}
							Log.Warning<int>("IPC message size {Len} out of range, dropping", len);
							goto end_IL_003b;
							end_IL_011a:;
						}
						end_IL_003b:;
					}
					catch (OperationCanceledException) when (_ipcCts.IsCancellationRequested)
					{
						break;
					}
					catch (Exception ex2)
					{
						Log.Warning<string>(ex2, "IPC server connection error: {Message}", ex2.Message);
						if (!_ipcCts.IsCancellationRequested)
						{
							await Task.Delay(100, _ipcCts.Token).ConfigureAwait(continueOnCapturedContext: false);
						}
					}
				}
			}
			catch (Exception ex3)
			{
				Exception ex4 = ex3;
				Log.Error(ex4, "IPC server loop terminated unexpectedly");
			}
			finally
			{
				Log.Debug("IPC server loop ended");
			}
		}, _ipcCts.Token);
		_ipcLoop.ContinueWith(delegate(Task task)
		{
			if (task.IsFaulted && task.Exception != null)
			{
				Log.Error((Exception)task.Exception, "IPC server task failed");
			}
		}, TaskContinuationOptions.OnlyOnFaulted);
	}

	private static void DispatchCallbackOnUiThread(string payload)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			App.GetInstance<OAuthAccountViewModel>()?.HandleCallback(payload);
			if ((Window)(object)App.MainWindow != (Window)null)
			{
				App.MainWindow.Restore.Execute(Unit.Default);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to dispatch OAuth callback");
		}
	}

	private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
	{
		int read;
		for (int offset = 0; offset < buffer.Length; offset += read)
		{
			read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct).ConfigureAwait(continueOnCapturedContext: false);
			if (read <= 0)
			{
				throw new EndOfStreamException();
			}
		}
	}

	public void Stop()
	{
		_ipcCts.Cancel();
		try
		{
			_ipcLoop?.Wait(TimeSpan.FromSeconds(2L));
		}
		catch (AggregateException ex) when (ex.InnerExceptions.All((Exception e) => e is OperationCanceledException))
		{
		}
		catch (Exception ex2)
		{
			Log.Warning(ex2, "Error while stopping IPC server");
		}
		finally
		{
			_ipcLoop = null;
		}
	}
}
