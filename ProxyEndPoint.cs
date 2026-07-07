using System;
using System.Security.Cryptography;
using System.Text;

namespace VpnHood.Core.Proxies.EndPointManagement.Abstractions;

public class ProxyEndPoint
{
	public string Id
	{
		get
		{
			string s = $"{Protocol}://{Host}:{Port}";
			MD5 mD = MD5.Create();
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			return Convert.ToHexString(mD.ComputeHash(bytes));
		}
	}

	public bool IsEnabled { get; set; } = true;

	public required ProxyProtocol Protocol { get; init; }

	public required string Host { get; init; }

	public required int Port { get; init; }

	public string? Username { get; set; }

	public string? Password { get; set; }

	public Uri Url => new UriBuilder
	{
		Scheme = Protocol.ToString().ToLower(),
		Host = Host,
		Port = Port,
		UserName = Username,
		Password = Password,
		Query = (IsEnabled ? "enabled=1" : null)
	}.Uri;

	public Uri BuildUrlWithoutPassword()
	{
		Uri url = Url;
		return new UriBuilder
		{
			Scheme = url.Scheme,
			Host = url.Host,
			Port = url.Port
		}.Uri;
	}
}
