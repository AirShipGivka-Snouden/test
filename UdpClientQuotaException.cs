namespace VpnHood.Core.Tunneling.Exceptions;

public class UdpClientQuotaException : NetFilterException
{
	public UdpClientQuotaException(int maxUdpClient)
		: base($"Maximum UdpClient has been reached. MaxUdpClient: {maxUdpClient}")
	{
	}
}
