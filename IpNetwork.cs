using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text.Json.Serialization;

namespace VpnHood.Core.Toolkit.Net;

[JsonConverter(typeof(IpNetworkConverter))]
public class IpNetwork
{
	private readonly BigInteger _firstIpAddressValue;

	private readonly BigInteger _lastIpAddressValue;

	public IPAddress Prefix { get; }

	public int PrefixLength { get; }

	public IPAddress SubnetMask => CidrToSubnetMask(PrefixLength, Prefix.AddressFamily);

	public AddressFamily AddressFamily => Prefix.AddressFamily;

	public bool IsV4 => Prefix.AddressFamily == AddressFamily.InterNetwork;

	public bool IsV6 => Prefix.AddressFamily == AddressFamily.InterNetworkV6;

	public IPAddress FirstIpAddress { get; }

	public IPAddress LastIpAddress { get; }

	public BigInteger Total => _lastIpAddressValue - _firstIpAddressValue + 1;

	public static IpNetwork AllV4 { get; } = Parse("0.0.0.0/0");

	public static IpNetwork[] LocalNetworksV4 { get; } = new IpNetwork[4]
	{
		Parse("10.0.0.0/8"),
		Parse("172.16.0.0/12"),
		Parse("192.168.0.0/16"),
		Parse("169.254.0.0/16")
	};

	public static IpNetwork MulticastNetworkV4 { get; } = new IpNetwork(IPAddress.Parse("224.0.0.0"), 4);

	public static IpNetwork MulticastNetworkV6 { get; } = new IpNetwork(IPAddress.Parse("ff00::"), 8);

	public static IpNetwork[] MulticastNetworks { get; } = new IpNetwork[2] { MulticastNetworkV4, MulticastNetworkV6 };

	public static IpNetwork LoopbackNetworkV4 { get; } = Parse("127.0.0.0/8");

	public static IpNetwork LoopbackNetworkV6 { get; } = Parse("::1/128");

	public static IpNetwork[] LoopbackNetworks { get; } = new IpNetwork[2] { LoopbackNetworkV4, LoopbackNetworkV6 };

	public static IpNetwork AllV6 { get; } = Parse("::/0");

	public static IpNetwork AllGlobalUnicastV6 { get; } = Parse("2000::/3");

	public static IpNetwork[] LocalNetworksV6 { get; } = AllGlobalUnicastV6.Invert().ToArray();

	public static IpNetwork[] LocalNetworks { get; } = LocalNetworksV4.Concat(LocalNetworksV6).ToArray();

	public static IpNetwork[] All { get; } = new IpNetwork[2] { AllV4, AllV6 };

	public static IpNetwork[] None { get; } = Array.Empty<IpNetwork>();

	public IpNetwork(IPAddress prefix)
		: this(prefix, (prefix.AddressFamily == AddressFamily.InterNetwork) ? 32 : 128)
	{
	}

	public IpNetwork(IPAddress prefix, int prefixLength)
	{
		IPAddressUtil.Verify(prefix);
		Prefix = prefix;
		PrefixLength = prefixLength;
		int num = ((prefix.AddressFamily == AddressFamily.InterNetworkV6) ? 128 : 32);
		BigInteger bigInteger = (new BigInteger(1) << prefixLength) - 1 << num - prefixLength;
		BigInteger bigInteger2 = (new BigInteger(1) << num - prefixLength) - 1;
		_firstIpAddressValue = IPAddressUtil.ToBigInteger(Prefix) & bigInteger;
		_lastIpAddressValue = _firstIpAddressValue | bigInteger2;
		FirstIpAddress = IPAddressUtil.FromBigInteger(_firstIpAddressValue, prefix.AddressFamily);
		LastIpAddress = IPAddressUtil.FromBigInteger(_lastIpAddressValue, prefix.AddressFamily);
	}

	public static IEnumerable<IpNetwork> FromRange(IPAddress firstIpAddress, IPAddress lastIpAddress)
	{
		if (firstIpAddress.AddressFamily != lastIpAddress.AddressFamily)
		{
			throw new ArgumentException("AddressFamilies don't match!");
		}
		AddressFamily addressFamily = firstIpAddress.AddressFamily;
		int bits = ((addressFamily == AddressFamily.InterNetworkV6) ? 128 : 32);
		BigInteger first = IPAddressUtil.ToBigInteger(firstIpAddress);
		BigInteger last = IPAddressUtil.ToBigInteger(lastIpAddress);
		if (first > last)
		{
			yield break;
		}
		++last;
		BigInteger mask = 1;
		int len = 0;
		while (first + mask <= last)
		{
			if ((first & mask) != 0L)
			{
				yield return new IpNetwork(IPAddressUtil.FromBigInteger(first, addressFamily), bits - len);
				first += mask;
			}
			mask <<= 1;
			len++;
		}
		while (first < last)
		{
			mask >>= 1;
			len--;
			if ((last & mask) != 0L)
			{
				yield return new IpNetwork(IPAddressUtil.FromBigInteger(first, addressFamily), bits - len);
				first += mask;
			}
		}
	}

	private static IPAddress CidrToSubnetMask(int prefixLength, AddressFamily addressFamily)
	{
		switch (addressFamily)
		{
		case AddressFamily.InterNetwork:
		{
			if ((prefixLength < 0 || prefixLength > 32) ? true : false)
			{
				throw new ArgumentOutOfRangeException("prefixLength", "Invalid CIDR prefix length for IPv4.");
			}
			byte[] bytes = BitConverter.GetBytes((uint)(-1 << 32 - prefixLength));
			bytes.Reverse();
			return new IPAddress(bytes);
		}
		case AddressFamily.InterNetworkV6:
		{
			if ((prefixLength < 0 || prefixLength > 128) ? true : false)
			{
				throw new ArgumentOutOfRangeException("prefixLength", "Invalid CIDR prefix length for IPv6.");
			}
			byte[] array = new byte[16];
			for (int i = 0; i < prefixLength / 8; i++)
			{
				array[i] = byte.MaxValue;
			}
			if (prefixLength % 8 > 0)
			{
				array[prefixLength / 8] = (byte)(255 << 8 - prefixLength % 8);
			}
			return new IPAddress(array);
		}
		default:
			throw new ArgumentException("Invalid Address Family. Use InterNetwork (IPv4) or InterNetworkV6 (IPv6).");
		}
	}

	public IOrderedEnumerable<IpNetwork> Invert()
	{
		return new IpNetwork[1] { this }.ToIpRanges().Invert(AddressFamily == AddressFamily.InterNetwork, AddressFamily == AddressFamily.InterNetworkV6).ToIpNetworks();
	}

	public IpRange ToIpRange()
	{
		return new IpRange(FirstIpAddress, LastIpAddress);
	}

	public bool Contains(IPAddress ipAddress)
	{
		if (IPAddressUtil.Compare(ipAddress, FirstIpAddress) >= 0)
		{
			return IPAddressUtil.Compare(ipAddress, LastIpAddress) <= 0;
		}
		return false;
	}

	public static IpNetwork Parse(string value)
	{
		try
		{
			string[] array = value.Split('/');
			return new IpNetwork(IPAddress.Parse(array[0]), int.Parse(array[1]));
		}
		catch
		{
			throw new FormatException("Could not parse IPNetwork from: " + value + ".");
		}
	}

	public override string ToString()
	{
		return $"{Prefix}/{PrefixLength}";
	}

	public override bool Equals(object? obj)
	{
		if (obj is IpNetwork ipNetwork && FirstIpAddress.Equals(ipNetwork.FirstIpAddress))
		{
			return LastIpAddress.Equals(ipNetwork.LastIpAddress);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(FirstIpAddress, LastIpAddress);
	}
}
