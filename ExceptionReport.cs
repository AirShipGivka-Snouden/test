namespace Ciphra.VPN.Common.Services;

public sealed class ExceptionReport
{
	public required string Message { get; init; }

	public required string StackTrace { get; init; }

	public required string TypeName { get; init; }

	public required string Source { get; init; }

	public required string TargetSite { get; init; }

	public required string MethodName { get; init; }

	public required bool Fatal { get; init; }

	public required string InnerExceptionMessages { get; init; }

	public required string OuterType { get; init; }

	public required int Occurrence { get; init; }
}
