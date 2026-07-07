using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Tunneling.Utils;

public static class HttpUtils
{
	public const string HttpRequestKey = "HTTP_REQUEST";

	public const int MaxHeaderSize = 131072;

	public static async Task<MemoryStream> ReadHeadersAsync(Stream stream, CancellationToken cancellationToken, int maxLength = 131072)
	{
		MemoryStream memStream = new MemoryStream(1024);
		try
		{
			byte[] readBuffer = new byte[1];
			int lfCounter = 0;
			while (lfCounter < 4)
			{
				if (await stream.ReadAsync(readBuffer, 0, 1, cancellationToken).Vhc() == 0)
				{
					if (memStream.Length != 0L)
					{
						throw new Exception("HttpStream has been closed unexpectedly.");
					}
					return memStream;
				}
				lfCounter = ((readBuffer[0] == 13 || readBuffer[0] == 10) ? (lfCounter + 1) : 0);
				await memStream.WriteAsync(readBuffer, 0, 1, cancellationToken).Vhc();
				if (memStream.Length > maxLength)
				{
					throw new Exception("HTTP header is too big.");
				}
			}
			memStream.Position = 0L;
			return memStream;
		}
		catch
		{
			await memStream.DisposeAsync().Vhc();
			throw;
		}
	}

	public static async Task<HttpResponseMessage> ReadResponse(Stream stream, CancellationToken cancellationToken, int maxLength = 131072)
	{
		using MemoryStream headerStream = await ReadHeadersAsync(stream, cancellationToken, maxLength);
		using StreamReader reader = new StreamReader(headerStream, Encoding.UTF8);
		string obj = (await reader.ReadLineAsync(cancellationToken)) ?? string.Empty;
		if (!obj.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
		{
			throw new Exception("Invalid HTTP response format. Make sure you have the latest version.");
		}
		string[] array = obj.Split(' ');
		if (array.Length < 2)
		{
			throw new Exception("Invalid HTTP response. Make sure you have the latest version.");
		}
		return new HttpResponseMessage((HttpStatusCode)int.Parse(array[1]));
	}

	public static async Task<Dictionary<string, string>?> ParseHeadersAsync(Stream stream, CancellationToken cancellationToken, int maxLength = 131072)
	{
		using MemoryStream memStream = await ReadHeadersAsync(stream, cancellationToken, maxLength);
		if (memStream.Length == 0L)
		{
			return null;
		}
		StreamReader reader = new StreamReader(memStream, Encoding.UTF8);
		Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		while (true)
		{
			string text = await reader.ReadLineAsync(cancellationToken);
			if (string.IsNullOrEmpty(text))
			{
				break;
			}
			if (headers.Count == 0)
			{
				headers["HTTP_REQUEST"] = text;
			}
			int num = text.IndexOf(':');
			if (num > 0)
			{
				string key = text.Substring(0, num).Trim();
				string value = text.Substring(num + 1).Trim();
				headers[key] = value;
			}
		}
		if (int.TryParse(headers.GetValueOrDefault("X-StartPadding"), out var result) && result > 0)
		{
			await stream.CopyToAsync(Stream.Null, result, cancellationToken);
		}
		return headers;
	}

	public static string GetApiKey(byte[] key, string passCheck)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(passCheck);
		if (bytes.Length != 16)
		{
			throw new InvalidOperationException("Password must be 16 bytes.");
		}
		using Aes aes = Aes.Create();
		aes.Mode = CipherMode.ECB;
		aes.Padding = PaddingMode.PKCS7;
		aes.Key = key;
		using ICryptoTransform cryptoTransform = aes.CreateEncryptor(aes.Key, aes.IV);
		byte[] array = new byte[16];
		cryptoTransform.TransformBlock(bytes, 0, bytes.Length, array, 0);
		return Convert.ToBase64String(array);
	}
}
