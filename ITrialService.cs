using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ciphra.VPN.Common.Services.Interfaces;

public interface ITrialService
{
	Task<DateTime?> CheckTrialExpiryDateAsync(CancellationToken ct);
}
