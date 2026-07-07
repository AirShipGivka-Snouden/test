using System.Threading.Tasks;

namespace Ciphra.VPN.Common.Services.Interfaces;

public interface IDeviceIdService
{
	Task<string> GetDeviceId();

	string GetPlatform();
}
