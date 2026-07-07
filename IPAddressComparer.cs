using System.Collections.Generic;
using System.Net;

namespace VpnHood.Core.Toolkit.Net;

public class IPAddressComparer : IComparer<IPAddress>
{
	public int Compare(IPAddress? x, IPAddress? y)
	{
		return IPAddressUtil.Compare(x, y);
	}
}
