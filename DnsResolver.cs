using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VpnHood.Core.Toolkit.Utils;

public static class DnsResolver
{
	public static async Task<IPHostEntry> GetHostEntry(string host, IPEndPoint dnsEndPoint, TimeSpan timeout, CancellationToken cancellationToken)
	{
		using UdpClient udpClientTemp = new UdpClient();
		return await GetHostEntry(host, dnsEndPoint, udpClientTemp, timeout, cancellationToken);
	}

	public static async Task<IPHostEntry> GetHostEntry(string host, IPEndPoint dnsEndPoint, UdpClient udpClient, TimeSpan timeout, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(host))
		{
			throw new ArgumentException("Host cannot be null or empty", "host");
		}
		if (dnsEndPoint == null)
		{
			throw new ArgumentNullException("dnsEndPoint");
		}
		udpClient.Connect(dnsEndPoint);
		ushort queryId = (ushort)new Random().Next(65535);
		byte[] array = BuildDnsQuery(queryId, host);
		using CancellationTokenSource timeoutCts = new CancellationTokenSource(timeout);
		using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
		await udpClient.SendAsync(array.AsMemory(), linkedCts.Token).Vhc();
		IPHostEntry iPHostEntry = ParseDnsResponse((await udpClient.ReceiveAsync(linkedCts.Token).Vhc()).Buffer, queryId);
		iPHostEntry.HostName = host;
		return iPHostEntry;
	}

	public static byte[] BuildDnsQuery(ushort queryId, string host)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write((ushort)IPAddress.HostToNetworkOrder((short)queryId));
		binaryWriter.Write((ushort)IPAddress.HostToNetworkOrder((short)256));
		binaryWriter.Write((ushort)IPAddress.HostToNetworkOrder((short)1));
		binaryWriter.Write((ushort)IPAddress.HostToNetworkOrder((short)0));
		binaryWriter.Write((ushort)IPAddress.HostToNetworkOrder((short)0));
		binaryWriter.Write((ushort)IPAddress.HostToNetworkOrder((short)0));
		string[] array = host.Split('.');
		foreach (string text in array)
		{
			binaryWriter.Write((byte)text.Length);
			binaryWriter.Write(Encoding.ASCII.GetBytes(text));
		}
		binaryWriter.Write((byte)0);
		binaryWriter.Write((ushort)IPAddress.HostToNetworkOrder((short)1));
		binaryWriter.Write((ushort)IPAddress.HostToNetworkOrder((short)1));
		return memoryStream.ToArray();
	}

	public static IPHostEntry ParseDnsResponse(byte[] response, ushort queryId)
	{
		using MemoryStream input = new MemoryStream(response);
		using BinaryReader binaryReader = new BinaryReader(input);
		if ((ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16()) != queryId)
		{
			throw new InvalidOperationException("Response ID does not match query ID.");
		}
		IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
		ushort num = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
		ushort num2 = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
		IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
		IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
		for (int i = 0; i < num; i++)
		{
			SkipDomainName(binaryReader);
			binaryReader.ReadUInt16();
			binaryReader.ReadUInt16();
		}
		List<IPAddress> list = new List<IPAddress>();
		for (int j = 0; j < num2; j++)
		{
			SkipDomainName(binaryReader);
			ushort num3 = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
			ushort num4 = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
			IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());
			ushort count = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
			if (num3 == 1 && num4 == 1)
			{
				byte[] address = binaryReader.ReadBytes(count);
				list.Add(new IPAddress(address));
			}
			else
			{
				binaryReader.ReadBytes(count);
			}
		}
		if (list.Count == 0)
		{
			throw new Exception("No valid A records found in DNS response.");
		}
		return new IPHostEntry
		{
			HostName = "",
			AddressList = list.ToArray()
		};
	}

	private static void SkipDomainName(BinaryReader reader)
	{
		while (true)
		{
			byte b = reader.ReadByte();
			if (b != 0)
			{
				if ((b & 0xC0) == 192)
				{
					reader.ReadByte();
					break;
				}
				reader.ReadBytes(b);
				continue;
			}
			break;
		}
	}
}
