namespace VpnHood.Core.Toolkit.Net;

public readonly record struct IPEndPointPairValue(IpEndPointValue Source, IpEndPointValue Destination)
{
	public override string ToString()
	{
		return $"{Source}->{Destination}";
	}
}
