using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text.Json.Serialization;

namespace VpnHood.Core.Toolkit.Net;

[JsonConverter(typeof(IpRangeConverter))]
public class IpRange
{
	public bool IsIPv4MappedToIPv6 => FirstIpAddress.IsIPv4MappedToIPv6;

	public AddressFamily AddressFamily => FirstIpAddress.AddressFamily;

	public IPAddress FirstIpAddress { get; }

	public IPAddress LastIpAddress { get; }

	public BigInteger Total
	{
		get
		{
			IPAddress lastIpAddress = LastIpAddress;
			Span<byte> buffer = stackalloc byte[16];
			BigInteger bigInteger = new BigInteger(lastIpAddress.GetAddressBytesFast(buffer), isUnsigned: true, isBigEndian: true);
			IPAddress firstIpAddress = FirstIpAddress;
			Span<byte> buffer2 = stackalloc byte[16];
			return bigInteger - new BigInteger(firstIpAddress.GetAddressBytesFast(buffer2), isUnsigned: true, isBigEndian: true) + 1;
		}
	}

	public IpRange(IPAddress ipAddress)
		: this(ipAddress, ipAddress)
	{
	}

	public IpRange(long firstIpAddress, long lastIpAddress)
		: this(IPAddressUtil.FromLong(firstIpAddress), IPAddressUtil.FromLong(lastIpAddress))
	{
	}

	public IpRange(IPAddress firstIpAddress, IPAddress lastIpAddress)
	{
		if (firstIpAddress.AddressFamily != lastIpAddress.AddressFamily)
		{
			throw new InvalidOperationException("Both ipAddress must have a same address family!");
		}
		if (IPAddressUtil.Compare(firstIpAddress, lastIpAddress) > 0)
		{
			throw new InvalidOperationException("lastIpAddress must be equal or greater than firstIpAddress");
		}
		FirstIpAddress = firstIpAddress;
		LastIpAddress = lastIpAddress;
	}

	public static IpRange FromIpAddress(IPAddress ipAddress)
	{
		return new IpRange(ipAddress);
	}

	public IpRange MapToIPv4()
	{
		return new IpRange(FirstIpAddress.MapToIPv4(), LastIpAddress.MapToIPv4());
	}

	public IpRange MapToIPv6()
	{
		return new IpRange(FirstIpAddress.MapToIPv6(), LastIpAddress.MapToIPv6());
	}

	public IEnumerable<IpNetwork> ToIpNetworks()
	{
		return IpNetwork.FromRange(FirstIpAddress, LastIpAddress);
	}

	public static IpRange Parse(string value)
	{
		if (value.IndexOf('/') != -1)
		{
			return IpNetwork.Parse(value).ToIpRange();
		}
		string[] array = value.Replace("to", "-").Split('-');
		return array.Length switch
		{
			1 => new IpRange(ParseIpAddress(array[0].Trim())), 
			2 => new IpRange(ParseIpAddress(array[0].Trim()), IPAddress.Parse(array[1].Trim())), 
			_ => throw new FormatException("Could not parse the IpRange from: " + value), 
		};
	}

	private static IPAddress ParseIpAddress(string value)
	{
		if (IPAddress.TryParse(value, out IPAddress address))
		{
			return address;
		}
		throw new FormatException("Could not parse the IpAddress from: " + value)
		{
			Data = { 
			{
				(object)"IpAddress",
				(object?)value
			} }
		};
	}

	public override string ToString()
	{
		if (FirstIpAddress.Equals(LastIpAddress))
		{
			return $"{FirstIpAddress}";
		}
		return $"{FirstIpAddress}-{LastIpAddress}";
	}

	public override bool Equals(object? obj)
	{
		if (obj is IpRange ipRange && FirstIpAddress.Equals(ipRange.FirstIpAddress))
		{
			return LastIpAddress.Equals(ipRange.LastIpAddress);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(FirstIpAddress, LastIpAddress);
	}

	public bool IsInRange(IPAddress ipAddress)
	{
		if (IPAddressUtil.Compare(ipAddress, FirstIpAddress) >= 0)
		{
			return IPAddressUtil.Compare(ipAddress, LastIpAddress) <= 0;
		}
		return false;
	}
}
