using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;

namespace Ciphra.VPN.Common.Storage;

public static class EncryptedJsonStore
{
	private const int NonceSize = 12;

	private const int TagSize = 16;

	private const int HeaderSize = 28;

	private const int WriteRetries = 3;

	private const int RetryDelayMs = 50;

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true,
		WriteIndented = false
	};

	public static T? Read<T>(string filePath) where T : class
	{
		if (!File.Exists(filePath))
		{
			return null;
		}
		try
		{
			byte[] array = ReadBytesWithRetry(filePath);
			if (array.Length < 28)
			{
				TryDelete(filePath);
				return null;
			}
			Span<byte> span = array.AsSpan(0, 12);
			Span<byte> span2 = array.AsSpan(12, 16);
			Span<byte> span3 = array.AsSpan(28);
			byte[] array2 = new byte[span3.Length];
			using AesGcm aesGcm = new AesGcm(StorageKeys.GetKey(), 16);
			aesGcm.Decrypt(span, span3, span2, array2);
			return JsonSerializer.Deserialize<T>(array2, JsonOptions);
		}
		catch (Exception ex) when (((ex is OutOfMemoryException || ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			return null;
		}
		catch
		{
			TryDelete(filePath);
			return null;
		}
	}

	public static void Write<T>(string filePath, T data) where T : class
	{
		byte[] array = JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions);
		byte[] array2 = new byte[12];
		RandomNumberGenerator.Fill(array2);
		byte[] array3 = new byte[array.Length];
		byte[] array4 = new byte[16];
		using AesGcm aesGcm = new AesGcm(StorageKeys.GetKey(), 16);
		aesGcm.Encrypt(array2, array, array3, array4);
		byte[] array5 = new byte[28 + array3.Length];
		array2.CopyTo(array5, 0);
		array4.CopyTo(array5, 12);
		array3.CopyTo(array5, 28);
		AtomicWrite(filePath, array5);
	}

	private static void AtomicWrite(string path, byte[] data)
	{
		string directoryName = Path.GetDirectoryName(path);
		if (directoryName != null && !Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		string text = path + ".tmp";
		TryDeleteForce(text);
		int num = 0;
		while (true)
		{
			try
			{
				File.WriteAllBytes(text, data);
			}
			catch (IOException) when (num < 2)
			{
				Thread.Sleep(50);
				goto IL_006c;
			}
			break;
			IL_006c:
			num++;
		}
		try
		{
			for (int i = 0; i < 3; i++)
			{
				try
				{
					File.Move(text, path, overwrite: true);
					break;
				}
				catch (IOException) when (i < 2)
				{
					Thread.Sleep(50);
				}
			}
		}
		catch
		{
			TryDelete(text);
			throw;
		}
	}

	private static byte[] ReadBytesWithRetry(string path)
	{
		for (int i = 0; i < 3; i++)
		{
			try
			{
				return File.ReadAllBytes(path);
			}
			catch (IOException) when (i < 2)
			{
				Thread.Sleep(50);
			}
		}
		return File.ReadAllBytes(path);
	}

	private static void TryDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (IOException)
		{
		}
	}

	private static void TryDeleteForce(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.SetAttributes(path, FileAttributes.Normal);
				File.Delete(path);
			}
		}
		catch
		{
		}
	}
}
