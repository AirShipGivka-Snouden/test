using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Toolkit.Net;

public static class IPAddressUtil
{
	public static IPAddress[] GoogleDnsServers { get; } = new IPAddress[4]
	{
		IPAddress.Parse("8.8.8.8"),
		IPAddress.Parse("8.8.4.4"),
		IPAddress.Parse("2001:4860:4860::8888"),
		IPAddress.Parse("2001:4860:4860::8844")
	};

	public static IPAddress[] KidsSafeCloudflareDnsServers { get; } = new IPAddress[4]
	{
		IPAddress.Parse("1.1.1.3"),
		IPAddress.Parse("1.0.0.3"),
		IPAddress.Parse("2606:4700:4700::1113"),
		IPAddress.Parse("2606:4700:4700::1003")
	};

	public static IPAddress[] ReliableDnsServers { get; } = new IPAddress[4]
	{
		GoogleDnsServers.First((IPAddress x) => x.IsV4()),
		GoogleDnsServers.First((IPAddress x) => x.IsV6()),
		KidsSafeCloudflareDnsServers.First((IPAddress x) => x.IsV4()),
		KidsSafeCloudflareDnsServers.First((IPAddress x) => x.IsV6())
	};

	public static IPAddress MaxIPv6Value { get; } = IPAddress.Parse("FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF");

	public static IPAddress MinIPv6Value { get; } = IPAddress.Parse("::");

	public static IPAddress MaxIPv4Value { get; } = IPAddress.Parse("255.255.255.255");

	public static IPAddress MinIPv4Value { get; } = IPAddress.Parse("0.0.0.0");

	public static IPAddress GenerateUlaAddress(ushort lastValue)
	{
		byte[] array = new byte[5];
		using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
		randomNumberGenerator.GetBytes(array);
		return new IPAddress(new byte[16]
		{
			253,
			array[0],
			array[1],
			array[2],
			array[3],
			array[4],
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			(byte)(lastValue >> 8),
			(byte)(lastValue & 0xFF)
		});
	}

	public static async Task<IPAddress[]> GetPrivateIpAddresses()
	{
		List<IPAddress> ret = new List<IPAddress>();
		Task<IPAddress?> ipV4Task = GetPrivateIpAddress(AddressFamily.InterNetwork);
		Task<IPAddress?> ipV6Task = GetPrivateIpAddress(AddressFamily.InterNetworkV6);
		InlineArray2<Task<IPAddress>> buffer = default(InlineArray2<Task<IPAddress>>);
		buffer[0] = ipV4Task;
		buffer[1] = ipV6Task;
		await Task.WhenAll<IPAddress>(buffer).Vhc();
		if (ipV4Task.Result != null)
		{
			ret.Add(ipV4Task.Result);
		}
		if (ipV6Task.Result != null)
		{
			ret.Add(ipV6Task.Result);
		}
		return ret.ToArray();
	}

	public static async Task<bool> IsIpv6Supported()
	{
		try
		{
			IPAddress[] source = ReliableDnsServers.Where((IPAddress x) => x.IsV6()).ToArray();
			using UdpClient udpClient = new UdpClient(AddressFamily.InterNetworkV6);
			udpClient.Connect(source.First(), 53);
			Ping ping = new Ping();
			IEnumerable<Task<PingReply>> enumerable = source.Select((IPAddress x) => ping.SendPingAsync(x));
			foreach (Task<PingReply> item in enumerable)
			{
				try
				{
					if ((await item.Vhc()).Status == IPStatus.Success)
					{
						return true;
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return false;
	}

	public static async Task<IPAddress[]> GetPublicIpAddresses(CancellationToken cancellationToken)
	{
		List<IPAddress> ret = new List<IPAddress>();
		IPAddress ipV4Task = await GetPublicIpAddress(AddressFamily.InterNetwork, cancellationToken).Vhc();
		IPAddress iPAddress = await GetPublicIpAddress(AddressFamily.InterNetworkV6, cancellationToken).Vhc();
		if (ipV4Task != null)
		{
			ret.Add(ipV4Task);
		}
		if (iPAddress != null)
		{
			ret.Add(iPAddress);
		}
		return ret.ToArray();
	}

	public static async Task<IPAddress?> GetPublicIpAddress(AddressFamily addressFamily, CancellationToken cancellationToken)
	{
		try
		{
			using CancellationTokenSource linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			linkedToken.CancelAfter(TimeSpan.FromSeconds(5L));
			return await GetPublicIpAddressByCloudflare(addressFamily, linkedToken.Token);
		}
		catch
		{
		}
		try
		{
			using CancellationTokenSource linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			linkedToken.CancelAfter(TimeSpan.FromSeconds(5L));
			return await GetPublicIpAddressByIpify(addressFamily, linkedToken.Token);
		}
		catch
		{
		}
		return null;
	}

	private static async Task<IPAddress?> GetPublicIpAddressByCloudflare(AddressFamily addressFamily, CancellationToken cancellationToken)
	{
		string requestUri = "https://www.cloudflare.com/cdn-cgi/trace";
		SocketsHttpHandler handler = new SocketsHttpHandler
		{
			ConnectCallback = async delegate(SocketsHttpConnectionContext context, CancellationToken token)
			{
				Socket socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
				try
				{
					await socket.ConnectAsync(context.DnsEndPoint, token).ConfigureAwait(continueOnCapturedContext: false);
					return new NetworkStream(socket, ownsSocket: true);
				}
				catch
				{
					socket.Dispose();
					throw;
				}
			}
		};
		using HttpClient httpClient = new HttpClient(handler);
		httpClient.DefaultRequestHeaders.Add("User-Agent", "VpnHood");
		string text = (await httpClient.GetStringAsync(requestUri, cancellationToken).Vhc()).Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).SingleOrDefault((string x) => x.StartsWith("ip=", StringComparison.OrdinalIgnoreCase));
		return (text != null) ? IPAddress.Parse(text.Split('=')[1]) : null;
	}

	private static async Task<IPAddress?> GetPublicIpAddressByIpify(AddressFamily addressFamily, CancellationToken cancellationToken)
	{
		string requestUri = ((addressFamily == AddressFamily.InterNetwork) ? "https://api4.my-ip.io/v2/ip.json" : "https://api6.my-ip.io/v2/ip.json");
		HttpClientHandler handler = new HttpClientHandler
		{
			AllowAutoRedirect = true
		};
		using HttpClient httpClient = new HttpClient(handler);
		httpClient.DefaultRequestHeaders.Add("User-Agent", "VpnHood");
		IPAddress iPAddress = IPAddress.Parse(JsonDocument.Parse(await httpClient.GetStringAsync(requestUri, cancellationToken).Vhc()).RootElement.GetProperty("ip").GetString() ?? throw new InvalidOperationException());
		return (iPAddress.AddressFamily == addressFamily) ? iPAddress : null;
	}

	public static Task<IPAddress?> GetPrivateIpAddress(AddressFamily addressFamily)
	{
		try
		{
			using UdpClient udpClient = new UdpClient(addressFamily);
			return GetPrivateIpAddress(udpClient);
		}
		catch
		{
			return Task.FromResult<IPAddress>(null);
		}
	}

	public static Task<IPAddress?> GetPrivateIpAddress(UdpClient udpClient)
	{
		try
		{
			AddressFamily addressFamily = udpClient.Client.AddressFamily;
			IPAddress addr = KidsSafeCloudflareDnsServers.First((IPAddress x) => x.AddressFamily == addressFamily);
			udpClient.Connect(addr, 53);
			IPAddress address = (((IPEndPoint)udpClient.Client.LocalEndPoint) ?? throw new InvalidOperationException("Could not get local endpoint from UdpClient!")).Address;
			return Task.FromResult((address.AddressFamily == addressFamily) ? address : null);
		}
		catch
		{
			return Task.FromResult<IPAddress>(null);
		}
	}

	public static IPAddress GetAnyIpAddress(AddressFamily addressFamily)
	{
		return addressFamily switch
		{
			AddressFamily.InterNetwork => IPAddress.Any, 
			AddressFamily.InterNetworkV6 => IPAddress.IPv6Any, 
			_ => throw new NotSupportedException($"{addressFamily} is not supported!"), 
		};
	}

	public static void Verify(AddressFamily addressFamily)
	{
		if ((addressFamily != AddressFamily.InterNetwork && addressFamily != AddressFamily.InterNetworkV6) || 1 == 0)
		{
			throw new NotSupportedException($"{addressFamily} is not supported!");
		}
	}

	public static void Verify(IPAddress ipAddress)
	{
		Verify(ipAddress.AddressFamily);
	}

	public static int Compare(IPAddress? ipAddress1, IPAddress? ipAddress2)
	{
		if (ipAddress1 == null && ipAddress2 == null)
		{
			return 0;
		}
		if (ipAddress1 == null)
		{
			return -1;
		}
		if (ipAddress2 == null)
		{
			return 1;
		}
		Verify(ipAddress1);
		Verify(ipAddress2);
		if (ipAddress1.IsIPv4MappedToIPv6)
		{
			ipAddress1 = ipAddress1.MapToIPv4();
		}
		if (ipAddress2.IsIPv4MappedToIPv6)
		{
			ipAddress2 = ipAddress2.MapToIPv4();
		}
		if (ipAddress1.IsV4() && ipAddress2.IsV6())
		{
			return -1;
		}
		if (ipAddress1.IsV6() && ipAddress2.IsV4())
		{
			return 1;
		}
		IPAddress ipAddress3 = ipAddress1;
		Span<byte> buffer = stackalloc byte[16];
		Span<byte> addressBytesFast = ipAddress3.GetAddressBytesFast(buffer);
		ipAddress3 = ipAddress2;
		buffer = stackalloc byte[16];
		Span<byte> addressBytesFast2 = ipAddress3.GetAddressBytesFast(buffer);
		return ((ReadOnlySpan<byte>)addressBytesFast).SequenceCompareTo((ReadOnlySpan<byte>)addressBytesFast2);
	}

	public static long ToLong(IPAddress ipAddress)
	{
		if (!ipAddress.IsV4())
		{
			throw new InvalidOperationException($"Only {AddressFamily.InterNetwork} family can be converted into long!");
		}
		Span<byte> buffer = stackalloc byte[16];
		Span<byte> addressBytesFast = ipAddress.GetAddressBytesFast(buffer);
		return (long)(((ulong)addressBytesFast[0] << 24) | ((ulong)addressBytesFast[1] << 16) | ((ulong)addressBytesFast[2] << 8) | addressBytesFast[3]);
	}

	public static IPAddress FromLong(long ipAddress)
	{
		return new IPAddress((uint)IPAddress.NetworkToHostOrder((int)ipAddress));
	}

	public static BigInteger ToBigInteger(IPAddress value)
	{
		Span<byte> buffer = stackalloc byte[16];
		return new BigInteger(value.GetAddressBytesFast(buffer), isUnsigned: true, isBigEndian: true);
	}

	public static IPAddress FromBigInteger(BigInteger value, AddressFamily addressFamily)
	{
		Verify(addressFamily);
		int bytesWritten = ((addressFamily == AddressFamily.InterNetworkV6) ? 16 : 4);
		Span<byte> span = stackalloc byte[bytesWritten];
		value.TryWriteBytes(span, out bytesWritten, isUnsigned: true);
		span.Reverse();
		return new IPAddress(span);
	}

	public static IPAddress Increment(IPAddress ipAddress)
	{
		Verify(ipAddress);
		Span<byte> buffer = stackalloc byte[16];
		Span<byte> addressBytesFast = ipAddress.GetAddressBytesFast(buffer);
		int num = addressBytesFast.Length - 1;
		while (num >= 0)
		{
			if (addressBytesFast[num] == byte.MaxValue)
			{
				addressBytesFast[num] = 0;
				num--;
				continue;
			}
			addressBytesFast[num]++;
			return new IPAddress(addressBytesFast);
		}
		return ipAddress;
	}

	public static IPAddress Decrement(IPAddress ipAddress)
	{
		Verify(ipAddress);
		Span<byte> buffer = stackalloc byte[16];
		Span<byte> addressBytesFast = ipAddress.GetAddressBytesFast(buffer);
		int num = addressBytesFast.Length - 1;
		while (num >= 0)
		{
			if (addressBytesFast[num] == 0)
			{
				addressBytesFast[num] = byte.MaxValue;
				num--;
				continue;
			}
			addressBytesFast[num]--;
			return new IPAddress(addressBytesFast);
		}
		return ipAddress;
	}

	public static bool IsMaxValue(IPAddress ipAddress)
	{
		Verify(ipAddress);
		return ipAddress.AddressFamily switch
		{
			AddressFamily.InterNetworkV6 => ipAddress.Equals(MaxIPv6Value), 
			AddressFamily.InterNetwork => ipAddress.Equals(MaxIPv4Value), 
			_ => throw new NotSupportedException($"{ipAddress.AddressFamily} is not supported!"), 
		};
	}

	public static bool IsMinValue(IPAddress ipAddress)
	{
		Verify(ipAddress);
		return ipAddress.AddressFamily switch
		{
			AddressFamily.InterNetwork => ipAddress.Equals(MinIPv4Value), 
			AddressFamily.InterNetworkV6 => ipAddress.Equals(MinIPv6Value), 
			_ => throw new NotSupportedException($"{ipAddress.AddressFamily} is not supported!"), 
		};
	}

	public static IPAddress Anonymize(IPAddress ipAddress)
	{
		IPAddress ipAddress2;
		Span<byte> buffer;
		if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
		{
			ipAddress2 = ipAddress;
			buffer = stackalloc byte[16];
			Span<byte> addressBytesFast = ipAddress2.GetAddressBytesFast(buffer);
			addressBytesFast[addressBytesFast.Length - 1] = 0;
			return new IPAddress(addressBytesFast);
		}
		ipAddress2 = ipAddress;
		buffer = stackalloc byte[16];
		Span<byte> addressBytesFast2 = ipAddress2.GetAddressBytesFast(buffer);
		for (int i = 6; i < addressBytesFast2.Length; i++)
		{
			addressBytesFast2[i] = 0;
		}
		return new IPAddress(addressBytesFast2);
	}

	public static IPAddress Min(IPAddress ipAddress1, IPAddress ipAddress2)
	{
		if (Compare(ipAddress1, ipAddress2) >= 0)
		{
			return ipAddress2;
		}
		return ipAddress1;
	}

	public static IPAddress Max(IPAddress ipAddress1, IPAddress ipAddress2)
	{
		if (Compare(ipAddress1, ipAddress2) <= 0)
		{
			return ipAddress2;
		}
		return ipAddress1;
	}

	public static string GetDosKey(IPAddress ip)
	{
		if (ip == null)
		{
			throw new ArgumentNullException("ip");
		}
		switch (ip.AddressFamily)
		{
		case AddressFamily.InterNetwork:
			return ip.ToString();
		case AddressFamily.InterNetworkV6:
		{
			byte[] addressBytes = ip.GetAddressBytes();
			for (int i = 8; i < 16; i++)
			{
				addressBytes[i] = 0;
			}
			IPAddress value = new IPAddress(addressBytes);
			return $"{value}/64";
		}
		default:
			throw new NotSupportedException("Unsupported IP address family.");
		}
	}
}
