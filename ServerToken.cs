using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using VpnHood.Core.Toolkit.Converters;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Common.Tokens;

public class ServerToken
{
	[JsonPropertyName("ct")]
	public required DateTime CreatedTime { get; set; }

	[JsonPropertyName("hname")]
	[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
	public required string HostName { get; set; }

	[JsonPropertyName("hport")]
	[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
	public required int HostPort { get; set; }

	[JsonPropertyName("isv")]
	[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
	public required bool IsValidHostName { get; set; }

	[JsonPropertyName("sec")]
	public required byte[]? Secret { get; set; }

	[JsonPropertyName("ch")]
	public byte[]? CertificateHash { get; set; }

	[JsonPropertyName("url")]
	[Obsolete("Use Urls. Version 558 or upper")]
	public string? Url
	{
		get
		{
			return Urls?.FirstOrDefault();
		}
		set
		{
			if (VhUtils.IsNullOrEmpty(Urls))
			{
				Urls = ((value == null) ? null : new string[1] { value });
			}
		}
	}

	[JsonPropertyName("urls")]
	public string[]? Urls { get; set; }

	[JsonPropertyName("ep")]
	[JsonConverter(typeof(ArrayConverter<IPEndPoint, IPEndPointConverter>))]
	public IPEndPoint[]? HostEndPoints { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[JsonPropertyName("path")]
	public string? PathBase { get; set; }

	[JsonPropertyName("ep_st")]
	[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
	public EndPointStrategy EndPointsStrategy { get; set; }

	[JsonPropertyName("loc")]
	[Obsolete]
	public string[]? ServerLocationsLegacy
	{
		get
		{
			return ServerLocations?.Select((string x) => x.Split("[").First()).ToArray();
		}
		set
		{
			int? num = ServerLocations?.Length;
			if ((!num.HasValue || num.GetValueOrDefault() == 0) ? true : false)
			{
				ServerLocations = value;
			}
		}
	}

	[JsonPropertyName("loc2")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string[]? ServerLocations { get; set; }

	public string Encrypt(byte[]? iv = null)
	{
		string value = JsonSerializer.Serialize(this, new JsonSerializerOptions
		{
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
		});
		if (Secret == null)
		{
			throw new Exception("There is no Secret in ServerToken.");
		}
		if (iv == null)
		{
			using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
			iv = new byte[Secret.Length];
			randomNumberGenerator.GetBytes(iv);
		}
		using Aes aes = Aes.Create();
		aes.Mode = CipherMode.CBC;
		aes.Key = Secret;
		aes.IV = iv;
		aes.Padding = PaddingMode.PKCS7;
		using MemoryStream memoryStream = new MemoryStream();
		using (ICryptoTransform transform = aes.CreateEncryptor())
		{
			using CryptoStream stream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
			using StreamWriter streamWriter = new StreamWriter(stream);
			streamWriter.Write(value);
			streamWriter.Flush();
		}
		return Convert.ToBase64String(iv) + "." + Convert.ToBase64String(memoryStream.ToArray());
	}

	public static ServerToken Decrypt(byte[] serverSecret, string base64)
	{
		string[] array = base64.Trim().Split('.');
		if (array.Length != 2)
		{
			throw new FormatException("Could not parse server token data.");
		}
		using Aes aes = Aes.Create();
		aes.Mode = CipherMode.CBC;
		aes.Key = serverSecret;
		aes.IV = Convert.FromBase64String(array[0]);
		aes.Padding = PaddingMode.PKCS7;
		using ICryptoTransform transform = aes.CreateDecryptor();
		using MemoryStream stream = new MemoryStream(Convert.FromBase64String(array[1]));
		using CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Read);
		using StreamReader streamReader = new StreamReader(stream2);
		return JsonUtils.Deserialize<ServerToken>(streamReader.ReadToEnd());
	}
}
