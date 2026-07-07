namespace Ciphra.VPN.Common.Services.OnChain;

public readonly record struct OnChainRpcResult<T>(OnChainRpcOutcome Outcome, T? Value)
{
	public static OnChainRpcResult<T> NotFound { get; }

	public static OnChainRpcResult<T> Unavailable { get; }

	public bool IsFound => Outcome == OnChainRpcOutcome.Found;

	public bool IsUnavailable => Outcome == OnChainRpcOutcome.Unavailable;

	public static OnChainRpcResult<T> Of(T value)
	{
		return new OnChainRpcResult<T>(OnChainRpcOutcome.Found, value);
	}
}
