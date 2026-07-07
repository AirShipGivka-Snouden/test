namespace VpnHood.Core.Filtering.DomainFiltering.SniExtractors;

public readonly struct PacketSniResult
{
	public string? DomainName { get; init; }

	public bool NeedMore { get; init; }

	public object? State { get; init; }

	public static PacketSniResult NotFound => new PacketSniResult
	{
		DomainName = null,
		NeedMore = false,
		State = null
	};

	public static PacketSniResult Found(string domainName)
	{
		return new PacketSniResult
		{
			DomainName = domainName,
			NeedMore = false,
			State = null
		};
	}

	public static PacketSniResult Pending(object state)
	{
		return new PacketSniResult
		{
			DomainName = null,
			NeedMore = true,
			State = state
		};
	}
}
