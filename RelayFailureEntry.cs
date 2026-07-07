using System;

namespace Ciphra.VPN.Common.Models;

public record RelayFailureEntry(string Domain, DateTime FailedAt, string FailureReason, int RequestsServed, TimeSpan TimeInUse, string ResolutionOutcome);
