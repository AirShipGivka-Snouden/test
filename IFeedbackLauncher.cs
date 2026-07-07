using System.Threading.Tasks;

namespace Ciphra.VPN.Common.App;

public interface IFeedbackLauncher
{
	Task LaunchStoreReviewFormAsync();

	Task LaunchFeedbackFormAsync();
}
