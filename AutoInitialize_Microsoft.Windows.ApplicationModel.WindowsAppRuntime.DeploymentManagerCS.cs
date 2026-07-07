using System;

namespace Microsoft.Windows.ApplicationModel.WindowsAppRuntime.DeploymentManagerCS;

internal class AutoInitialize
{
	internal static DeploymentInitializeOptions Options
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Expected O, but got Unknown
			DeploymentInitializeOptions val = new DeploymentInitializeOptions();
			val.OnErrorShowUI = true;
			return val;
		}
	}

	internal static void AccessWindowsAppSDK()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		DeploymentInitializeOptions options = Options;
		DeploymentResult val = DeploymentManager.Initialize(options);
		if ((int)val.Status != 1)
		{
			int hResult = val.ExtendedError.HResult;
			Environment.Exit(hResult);
			Environment.FailFast("WindowsAppRuntime.DeploymentManager.Initialize error 0x{hr:X}");
		}
	}
}
