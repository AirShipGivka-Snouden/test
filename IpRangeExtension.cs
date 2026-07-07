using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace VpnHood.Core.Toolkit.Net;

public static class IpRangeExtension
{
	extension(IEnumerable<IpRange> ipRanges)
	{
		public IpRangeOrderedList ToOrderedList()
		{
			return new IpRangeOrderedList(ipRanges);
		}

		public IEnumerable<IpRange> Intersect(IEnumerable<IpRange> second)
		{
			throw new NotSupportedException("Use IpRangeOrderedList.Intersect.");
		}

		public string ToText()
		{
			return string.Join(Environment.NewLine, ipRanges.Select((IpRange x) => x.ToString()));
		}
	}

	extension(IEnumerable<IPAddress> iPAddresses)
	{
		public IEnumerable<IpRange> ToIpRanges()
		{
			return iPAddresses.Select((IPAddress x) => new IpRange(x));
		}

		public IpRangeOrderedList ToOrderedIpRanges()
		{
			return new IpRangeOrderedList(iPAddresses.ToIpRanges());
		}
	}
}
