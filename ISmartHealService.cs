using System.Collections.Generic;
using Ciphra.VPN.Common.Models;

namespace Ciphra.VPN.Common.Services.Interfaces;

public interface ISmartHealService
{
	IReadOnlyList<SmartHealCandidate> Candidates { get; }
}
