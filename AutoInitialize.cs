using System.Runtime.CompilerServices;
using Microsoft.Windows.ApplicationModel.WindowsAppRuntime.DeploymentManagerCS;

namespace Microsoft.Windows.ApplicationModel.WindowsAppRuntime.Common;

internal class AutoInitialize
{
	[ModuleInitializer]
	internal static void InitializeWindowsAppSDK()
	{
		Microsoft.Windows.ApplicationModel.WindowsAppRuntime.DeploymentManagerCS.AutoInitialize.AccessWindowsAppSDK();
	}
}
