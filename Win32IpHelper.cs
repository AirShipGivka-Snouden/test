using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.VpnAdapters.WinTun.WinNative;

public static class Win32IpHelper
{
	public struct MIB_IPFORWARD_ROW2
	{
		public ulong InterfaceLuid;

		public uint InterfaceIndex;

		public IP_ADDRESS_PREFIX DestinationPrefix;

		public SOCKADDR_INET NextHop;

		public uint SitePrefixLength;

		public uint ValidLifetime;

		public uint PreferredLifetime;

		public byte OnLinkPrefixLength;

		public uint Metric;

		public uint Protocol;

		[MarshalAs(UnmanagedType.U1)]
		public bool Loopback;

		[MarshalAs(UnmanagedType.U1)]
		public bool AutoconfigureAddress;

		[MarshalAs(UnmanagedType.U1)]
		public bool Publish;

		[MarshalAs(UnmanagedType.U1)]
		public bool Immortal;

		public uint Age;

		public uint Origin;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 32)]
	public struct IP_ADDRESS_PREFIX
	{
		public SOCKADDR_INET Prefix;

		public byte PrefixLength;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SOCKADDR_INET
	{
		[FieldOffset(0)]
		public SOCKADDR_IN Ipv4;

		[FieldOffset(0)]
		public SOCKADDR_IN6 Ipv6;

		[FieldOffset(0)]
		public ushort si_family;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	public struct SOCKADDR_IN
	{
		public ushort sin_family;

		public ushort sin_port;

		public uint sin_addr;

		public ulong sin_zero;
	}

	[StructLayout(LayoutKind.Sequential, Size = 16)]
	public struct IN6_ADDR
	{
		public ulong lower;

		public ulong upper;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	public struct SOCKADDR_IN6
	{
		public ushort sin6_family;

		public ushort sin6_port;

		public uint sin6_flowinfo;

		public IN6_ADDR sin6_addr;

		public uint sin6_scope_id;
	}

	[DllImport("Iphlpapi.dll", SetLastError = true)]
	public static extern int CreateIpForwardEntry2(ref MIB_IPFORWARD_ROW2 pRoute);

	private static IN6_ADDR ToIN6_ADDR(IPAddress ip)
	{
		byte[] addressBytes = ip.GetAddressBytes();
		if (addressBytes.Length != 16)
		{
			throw new ArgumentException("Must be an IPv6 address.", "ip");
		}
		return new IN6_ADDR
		{
			lower = BitConverter.ToUInt64(addressBytes, 0),
			upper = BitConverter.ToUInt64(addressBytes, 8)
		};
	}

	public static void AddRoute(uint interfaceIndex, IpNetwork ipNetwork, CancellationToken cancellationToken)
	{
		if (ipNetwork.IsV4)
		{
			SOCKADDR_INET prefix = new SOCKADDR_INET
			{
				Ipv4 = new SOCKADDR_IN
				{
					sin_addr = BitConverter.ToUInt32(ipNetwork.Prefix.GetAddressBytes(), 0),
					sin_family = 2
				},
				si_family = 2
			};
			SOCKADDR_INET nextHop = new SOCKADDR_INET
			{
				Ipv4 = new SOCKADDR_IN
				{
					sin_addr = 0u,
					sin_family = 2
				},
				si_family = 2
			};
			MIB_IPFORWARD_ROW2 pRoute = new MIB_IPFORWARD_ROW2
			{
				InterfaceIndex = interfaceIndex,
				DestinationPrefix = new IP_ADDRESS_PREFIX
				{
					Prefix = prefix,
					PrefixLength = (byte)Math.Clamp(ipNetwork.PrefixLength, 0, 32)
				},
				NextHop = nextHop,
				SitePrefixLength = 0u,
				ValidLifetime = uint.MaxValue,
				PreferredLifetime = uint.MaxValue,
				Metric = 0u,
				Protocol = 3u,
				Loopback = false,
				AutoconfigureAddress = false,
				Publish = false,
				Immortal = true
			};
			int num = CreateIpForwardEntry2(ref pRoute);
			if (num != 0 && num != 5010)
			{
				throw new Win32Exception(num, $"Failed to add IPv4 route for {ipNetwork}");
			}
		}
		if (ipNetwork.IsV6)
		{
			SOCKADDR_INET prefix2 = new SOCKADDR_INET
			{
				Ipv6 = new SOCKADDR_IN6
				{
					sin6_addr = ToIN6_ADDR(ipNetwork.Prefix),
					sin6_family = 23
				},
				si_family = 23
			};
			SOCKADDR_INET nextHop2 = new SOCKADDR_INET
			{
				Ipv6 = new SOCKADDR_IN6
				{
					sin6_addr = new IN6_ADDR
					{
						lower = 0uL,
						upper = 0uL
					},
					sin6_family = 23
				},
				si_family = 23
			};
			MIB_IPFORWARD_ROW2 pRoute2 = new MIB_IPFORWARD_ROW2
			{
				InterfaceIndex = interfaceIndex,
				DestinationPrefix = new IP_ADDRESS_PREFIX
				{
					Prefix = prefix2,
					PrefixLength = (byte)Math.Clamp(ipNetwork.PrefixLength, 0, 128)
				},
				NextHop = nextHop2,
				SitePrefixLength = 0u,
				ValidLifetime = uint.MaxValue,
				PreferredLifetime = uint.MaxValue,
				Metric = 0u,
				Protocol = 3u,
				Loopback = false,
				AutoconfigureAddress = false,
				Publish = false,
				Immortal = true
			};
			int num2 = CreateIpForwardEntry2(ref pRoute2);
			if (num2 != 0 && num2 != 5010)
			{
				throw new Win32Exception(num2, $"Failed to add IPv6 route for {ipNetwork}");
			}
		}
	}
}
