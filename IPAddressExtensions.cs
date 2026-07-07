using System;
using System.Net;
using System.Net.Sockets;

namespace VpnHood.Core.Toolkit.Net;

public static class IPAddressExtensions
{
	extension(IPAddress ipAddress)
	{
		public bool IsV4()
		{
			return ipAddress.AddressFamily == AddressFamily.InterNetwork;
		}

		public bool IsV6()
		{
			return ipAddress.AddressFamily == AddressFamily.InterNetworkV6;
		}

		public bool IsMulticast()
		{
			if (!ipAddress.IsV4() || !IpNetwork.MulticastNetworkV4.Contains(ipAddress))
			{
				if (ipAddress.IsV6())
				{
					return ipAddress.IsIPv6Multicast;
				}
				return false;
			}
			return true;
		}

		public bool IsBroadcast()
		{
			return ipAddress.Equals(IPAddress.Broadcast);
		}

		public bool IsLoopback()
		{
			if (ipAddress.IsV4())
			{
				Span<byte> destination = stackalloc byte[4];
				if (ipAddress.TryWriteBytes(destination, out var _))
				{
					return destination[0] == 127;
				}
				return false;
			}
			if (ipAddress.IsV6())
			{
				return ipAddress.Equals(IPAddress.IPv6Loopback);
			}
			return false;
		}

		public bool SpanEquals(ReadOnlySpan<byte> ipAddressSpan)
		{
			if (ipAddress.IsV4() && ipAddressSpan.Length != 4)
			{
				return false;
			}
			if (ipAddress.IsV6() && ipAddressSpan.Length != 16)
			{
				return false;
			}
			Span<byte> buffer = stackalloc byte[16];
			return ((ReadOnlySpan<byte>)ipAddress.GetAddressBytesFast(buffer)).SequenceEqual(ipAddressSpan);
		}

		public Span<byte> GetAddressBytesFast(Span<byte> buffer)
		{
			if (!ipAddress.TryWriteBytes(buffer, out var bytesWritten))
			{
				throw new ArgumentException($"buffer is not big enough to hold the IP address. BufferLength: {buffer.Length}, IPAddress: {ipAddress}.");
			}
			return buffer.Slice(0, bytesWritten);
		}

		public bool IsTestNetwork()
		{
			Span<byte> destination = stackalloc byte[16];
			if (!ipAddress.TryWriteBytes(destination, out var bytesWritten))
			{
				return false;
			}
			switch (bytesWritten)
			{
			case 4:
				if (destination[0] == 198)
				{
					return (destination[1] & 0xFE) == 18;
				}
				return false;
			case 16:
				if (destination[0] == 32 && destination[1] == 1 && destination[2] == 0)
				{
					return destination[3] == 2;
				}
				return false;
			default:
				return false;
			}
		}
	}

	extension(IPEndPoint ipEndPoint)
	{
		public bool IsV4()
		{
			return ipEndPoint.AddressFamily == AddressFamily.InterNetwork;
		}

		public bool IsV6()
		{
			return ipEndPoint.AddressFamily == AddressFamily.InterNetworkV6;
		}
	}

	extension(IpEndPointValue ipEndPoint)
	{
		public bool IsV4()
		{
			return ipEndPoint.Address.AddressFamily == AddressFamily.InterNetwork;
		}

		public bool IsV6()
		{
			return ipEndPoint.Address.AddressFamily == AddressFamily.InterNetworkV6;
		}
	}

	extension(AddressFamily addressFamily)
	{
		public bool IsV4()
		{
			return addressFamily == AddressFamily.InterNetwork;
		}

		public bool IsV6()
		{
			return addressFamily == AddressFamily.InterNetworkV6;
		}
	}
}
