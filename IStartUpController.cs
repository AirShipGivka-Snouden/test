using System.Threading.Tasks;

namespace Ciphra.VPN.Common.App;

public interface IStartUpController
{
	Task<StartupState> GetStartupTaskState();

	Task ToggleStartUpAsync(bool enable);
}
