using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Ciphra.VPN.Common.App;
using Serilog;
using Windows.ApplicationModel;

namespace Ciphra.VPN.WinUI.Services;

public class StartUpController : IStartUpController
{
	private const string taskId = "532082d9-07cc-4742-a2cd-1b0ecce37212";

	public async Task<StartupState> GetStartupTaskState()
	{
		TaskAwaiter<StartupTask> taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter<StartupTask>(StartupTask.GetAsync("532082d9-07cc-4742-a2cd-1b0ecce37212"));
		if (!taskAwaiter.IsCompleted)
		{
			await taskAwaiter;
			TaskAwaiter<StartupTask> taskAwaiter2 = default(TaskAwaiter<StartupTask>);
			taskAwaiter = taskAwaiter2;
		}
		StartupTask result = taskAwaiter.GetResult();
		StartupTask task = result;
		return ConvertToStartupState(task.State);
	}

	public async Task ToggleStartUpAsync(bool enable)
	{
		TaskAwaiter<StartupTask> taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter<StartupTask>(StartupTask.GetAsync("532082d9-07cc-4742-a2cd-1b0ecce37212"));
		if (!taskAwaiter.IsCompleted)
		{
			await taskAwaiter;
			TaskAwaiter<StartupTask> taskAwaiter2 = default(TaskAwaiter<StartupTask>);
			taskAwaiter = taskAwaiter2;
		}
		StartupTask result = taskAwaiter.GetResult();
		StartupTask startupTask = result;
		if (enable)
		{
			StartupTaskState state = startupTask.State;
			StartupTaskState val = state;
			switch ((int)val)
			{
			case 0:
			{
				TaskAwaiter<StartupTaskState> taskAwaiter3 = WindowsRuntimeSystemExtensions.GetAwaiter<StartupTaskState>(startupTask.RequestEnableAsync());
				if (!taskAwaiter3.IsCompleted)
				{
					await taskAwaiter3;
					TaskAwaiter<StartupTaskState> taskAwaiter4 = default(TaskAwaiter<StartupTaskState>);
					taskAwaiter3 = taskAwaiter4;
				}
				StartupTaskState result2 = taskAwaiter3.GetResult();
				StartupTaskState newState = result2;
				Log.Information<StartupTaskState>("Request to enable startup, result = {0}", newState);
				break;
			}
			case 1:
				Log.Information("Task is disabled and user must enable it manually.");
				break;
			case 3:
				Log.Information("Startup disabled by group policy, or not supported on this device");
				break;
			case 2:
				Log.Information("Startup is enabled, nothing to do.");
				break;
			}
		}
		else
		{
			StartupTaskState state2 = startupTask.State;
			StartupTaskState val2 = state2;
			switch ((int)val2)
			{
			case 0:
			case 1:
			case 3:
				Log.Information("Startup already disabled on this device");
				break;
			case 2:
				startupTask.Disable();
				Log.Information("Startup disabled");
				break;
			}
		}
	}

	private StartupState ConvertToStartupState(StartupTaskState state)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected I4, but got Unknown
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if (1 == 0)
		{
		}
		StartupState result = (int)state switch
		{
			2 => StartupState.Enabled, 
			0 => StartupState.Disabled, 
			1 => StartupState.DisabledByUser, 
			3 => StartupState.DisabledByPolicy, 
			_ => throw new ArgumentOutOfRangeException("state", state, null), 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
