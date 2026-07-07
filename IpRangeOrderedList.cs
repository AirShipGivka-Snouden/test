using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace VpnHood.Core.Toolkit.Net;

public class IpRangeOrderedList : IOrderedEnumerable<IpRange>, IEnumerable<IpRange>, IEnumerable, IReadOnlyList<IpRange>, IReadOnlyCollection<IpRange>
{
	private class IpRangeSearchComparer : IComparer<IpRange>
	{
		public int Compare(IpRange? x, IpRange? y)
		{
			if (x == null && y == null)
			{
				return 0;
			}
			if (x == null)
			{
				return -1;
			}
			if (y == null)
			{
				return 1;
			}
			if (IPAddressUtil.Compare(x.FirstIpAddress, y.FirstIpAddress) <= 0 && IPAddressUtil.Compare(x.LastIpAddress, y.LastIpAddress) >= 0)
			{
				return 0;
			}
			if (IPAddressUtil.Compare(x.FirstIpAddress, y.FirstIpAddress) < 0)
			{
				return -1;
			}
			return 1;
		}
	}

	private readonly List<IpRange> _orderedList;

	public int Count => _orderedList.Count;

	public static IpRangeOrderedList Empty { get; } = new IpRangeOrderedList(new List<IpRange>());

	public IpRange this[int index] => _orderedList[index];

	public IOrderedEnumerable<IpRange> CreateOrderedEnumerable<TKey>(Func<IpRange, TKey> keySelector, IComparer<TKey>? comparer, bool descending)
	{
		if (!descending)
		{
			return _orderedList.OrderBy(keySelector, comparer);
		}
		return _orderedList.OrderByDescending(keySelector, comparer);
	}

	public IEnumerator<IpRange> GetEnumerator()
	{
		return _orderedList.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public IpRangeOrderedList()
	{
		_orderedList = new List<IpRange>();
	}

	public IpRangeOrderedList(IEnumerable<IpRange> ipRanges)
	{
		_orderedList = Sort(ipRanges);
	}

	private IpRangeOrderedList(List<IpRange> orderedList)
	{
		_orderedList = orderedList;
	}

	public void Serialize(Stream stream)
	{
		Span<byte> buffer = stackalloc byte[16];
		Span<byte> buffer2 = stackalloc byte[16];
		using BinaryWriter binaryWriter = new BinaryWriter(stream);
		binaryWriter.Write(_orderedList.Count);
		foreach (IpRange ordered in _orderedList)
		{
			Span<byte> addressBytesFast = ordered.FirstIpAddress.GetAddressBytesFast(buffer);
			Span<byte> addressBytesFast2 = ordered.LastIpAddress.GetAddressBytesFast(buffer2);
			binaryWriter.Write((byte)addressBytesFast.Length);
			binaryWriter.Write(addressBytesFast);
			binaryWriter.Write((byte)addressBytesFast2.Length);
			binaryWriter.Write(addressBytesFast2);
		}
	}

	public static IpRangeOrderedList Deserialize(Stream stream)
	{
		using BinaryReader binaryReader = new BinaryReader(stream);
		int num = binaryReader.ReadInt32();
		IpRange[] array = new IpRange[num];
		for (int i = 0; i < num; i++)
		{
			byte count = binaryReader.ReadByte();
			byte[] address = binaryReader.ReadBytes(count);
			byte count2 = binaryReader.ReadByte();
			byte[] address2 = binaryReader.ReadBytes(count2);
			array[i] = new IpRange(new IPAddress(address), new IPAddress(address2));
		}
		return new IpRangeOrderedList(array);
	}

	public bool IsAll()
	{
		return IpNetwork.All.ToIpRanges().SequenceEqual(this);
	}

	public bool IsAllV4()
	{
		return new IpNetwork[1] { IpNetwork.AllV4 }.ToIpRanges().SequenceEqual(this);
	}

	public bool IsAllV6()
	{
		return new IpNetwork[1] { IpNetwork.AllV6 }.ToIpRanges().SequenceEqual(this);
	}

	public bool Contains(IPAddress ipAddress)
	{
		if (ipAddress.IsIPv4MappedToIPv6)
		{
			ipAddress = ipAddress.MapToIPv4();
		}
		return _orderedList.BinarySearch(new IpRange(ipAddress, ipAddress), new IpRangeSearchComparer()) >= 0;
	}

	public bool IsNone()
	{
		return _orderedList.Count == 0;
	}

	public IOrderedEnumerable<IpNetwork> ToIpNetworks()
	{
		List<IpNetwork> list = new List<IpNetwork>();
		foreach (IpRange ordered in _orderedList)
		{
			list.AddRange(ordered.ToIpNetworks());
		}
		return list.OrderBy((IpNetwork x) => x.FirstIpAddress, new IPAddressComparer());
	}

	public IpRangeOrderedList Union(IPAddress ipAddress)
	{
		return Union(new IpRange(ipAddress));
	}

	public IpRangeOrderedList Union(IpRange ipRange)
	{
		return Union(new _003C_003Ez__ReadOnlySingleElementList<IpRange>(ipRange));
	}

	public IpRangeOrderedList Union(IEnumerable<IpRange> ipRanges)
	{
		if (!ipRanges.Any())
		{
			return this;
		}
		return new IpRangeOrderedList(_orderedList.Concat(ipRanges));
	}

	public IpRangeOrderedList Exclude(IPAddress ipAddress)
	{
		return Exclude(new IpRange(ipAddress));
	}

	public IpRangeOrderedList Exclude(IpRange ipRange)
	{
		return Exclude(new _003C_003Ez__ReadOnlySingleElementList<IpRange>(ipRange));
	}

	public IpRangeOrderedList Exclude(IEnumerable<IpRange> ipRanges)
	{
		return Exclude(ipRanges.ToOrderedList());
	}

	public IpRangeOrderedList Exclude(IpRangeOrderedList ipRanges)
	{
		return Intersect(ipRanges.Invert());
	}

	public IpRangeOrderedList Intersect(IEnumerable<IpRange> ipRanges)
	{
		return Intersect(ipRanges.ToOrderedList());
	}

	public IpRangeOrderedList Intersect(IpRangeOrderedList ipRanges)
	{
		return Intersect(this, ipRanges);
	}

	public IpRangeOrderedList Invert(bool includeIPv4 = true, bool includeIPv6 = true)
	{
		return Invert(this, includeIPv4, includeIPv6);
	}

	private static List<IpRange> Sort(IEnumerable<IpRange> ipRanges)
	{
		return Unify(ipRanges.OrderBy((IpRange x) => x.FirstIpAddress, new IPAddressComparer()));
	}

	private static List<IpRange> Unify(IOrderedEnumerable<IpRange> sortedIpRanges)
	{
		List<IpRange> list = new List<IpRange>();
		foreach (IpRange sortedIpRange in sortedIpRanges)
		{
			if (list.Count > 0)
			{
				if (sortedIpRange.AddressFamily == list[list.Count - 1].AddressFamily)
				{
					if (IPAddressUtil.Compare(IPAddressUtil.Decrement(sortedIpRange.FirstIpAddress), list[list.Count - 1].LastIpAddress) <= 0)
					{
						if (IPAddressUtil.Compare(sortedIpRange.LastIpAddress, list[list.Count - 1].LastIpAddress) > 0)
						{
							list[list.Count - 1] = new IpRange(list[list.Count - 1].FirstIpAddress, sortedIpRange.LastIpAddress);
						}
						continue;
					}
				}
			}
			list.Add(sortedIpRange);
		}
		return list;
	}

	private static IpRangeOrderedList Intersect(IpRangeOrderedList ipRanges1, IpRangeOrderedList ipRanges2)
	{
		if (ipRanges1.IsAll())
		{
			return ipRanges2;
		}
		if (ipRanges2.IsAll())
		{
			return ipRanges1;
		}
		IEnumerable<IpRange> orderedIpRanges = ipRanges1.Where((IpRange x) => x.AddressFamily == AddressFamily.InterNetwork);
		IEnumerable<IpRange> source = ipRanges2.Where((IpRange x) => x.AddressFamily == AddressFamily.InterNetwork);
		IEnumerable<IpRange> orderedIpRanges2 = ipRanges1.Where((IpRange x) => x.AddressFamily == AddressFamily.InterNetworkV6);
		IEnumerable<IpRange> source2 = ipRanges2.Where((IpRange x) => x.AddressFamily == AddressFamily.InterNetworkV6);
		IEnumerable<IpRange> first = IntersectInternal(orderedIpRanges, source.ToArray());
		IEnumerable<IpRange> second = IntersectInternal(orderedIpRanges2, source2.ToArray());
		return new IpRangeOrderedList(first.Concat(second));
	}

	private static IEnumerable<IpRange> IntersectInternal(IEnumerable<IpRange> orderedIpRanges1, IpRange[] orderedIpRanges2)
	{
		List<IpRange> list = new List<IpRange>();
		foreach (IpRange item in orderedIpRanges1)
		{
			foreach (IpRange ipRange in orderedIpRanges2)
			{
				if (item.IsInRange(ipRange.FirstIpAddress))
				{
					list.Add(new IpRange(ipRange.FirstIpAddress, IPAddressUtil.Min(item.LastIpAddress, ipRange.LastIpAddress)));
				}
				else if (item.IsInRange(ipRange.LastIpAddress))
				{
					list.Add(new IpRange(IPAddressUtil.Max(item.FirstIpAddress, ipRange.FirstIpAddress), ipRange.LastIpAddress));
				}
				else if (ipRange.IsInRange(item.FirstIpAddress))
				{
					list.Add(new IpRange(item.FirstIpAddress, IPAddressUtil.Min(item.LastIpAddress, ipRange.LastIpAddress)));
				}
				else if (ipRange.IsInRange(item.LastIpAddress))
				{
					list.Add(new IpRange(IPAddressUtil.Max(item.FirstIpAddress, ipRange.FirstIpAddress), item.LastIpAddress));
				}
			}
		}
		return list;
	}

	private static IpRangeOrderedList Invert(IpRangeOrderedList ipRanges, bool includeIPv4 = true, bool includeIPv6 = true)
	{
		List<IpRange> list = new List<IpRange>();
		if (includeIPv4)
		{
			IpRange[] array = ipRanges.Where((IpRange x) => x.AddressFamily == AddressFamily.InterNetwork).ToArray();
			if (array.Any())
			{
				list.AddRange(InvertInternal(array));
			}
			else
			{
				list.Add(new IpRange(IPAddressUtil.MinIPv4Value, IPAddressUtil.MaxIPv4Value));
			}
		}
		if (includeIPv6)
		{
			IpRange[] array2 = ipRanges.Where((IpRange x) => x.AddressFamily == AddressFamily.InterNetworkV6).ToArray();
			if (array2.Any())
			{
				list.AddRange(InvertInternal(array2));
			}
			else
			{
				list.Add(new IpRange(IPAddressUtil.MinIPv6Value, IPAddressUtil.MaxIPv6Value));
			}
		}
		return new IpRangeOrderedList(list);
	}

	private static IEnumerable<IpRange> InvertInternal(IpRange[] orderedIpRanges)
	{
		List<IpRange> list = new List<IpRange>();
		for (int i = 0; i < orderedIpRanges.Length; i++)
		{
			IpRange ipRange = orderedIpRanges[i];
			IPAddress firstIpAddress = ((ipRange.AddressFamily == AddressFamily.InterNetworkV6) ? IPAddressUtil.MinIPv6Value : IPAddressUtil.MinIPv4Value);
			IPAddress lastIpAddress = ((ipRange.AddressFamily == AddressFamily.InterNetworkV6) ? IPAddressUtil.MaxIPv6Value : IPAddressUtil.MaxIPv4Value);
			if (i == 0 && !IPAddressUtil.IsMinValue(ipRange.FirstIpAddress))
			{
				list.Add(new IpRange(firstIpAddress, IPAddressUtil.Decrement(ipRange.FirstIpAddress)));
			}
			if (i > 0)
			{
				list.Add(new IpRange(IPAddressUtil.Increment(orderedIpRanges[i - 1].LastIpAddress), IPAddressUtil.Decrement(ipRange.FirstIpAddress)));
			}
			if (i == orderedIpRanges.Length - 1 && !IPAddressUtil.IsMaxValue(ipRange.LastIpAddress))
			{
				list.Add(new IpRange(IPAddressUtil.Increment(ipRange.LastIpAddress), lastIpAddress));
			}
		}
		return list;
	}
}
