using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Toolkit.Utils;

public static class VhUtils
{
	public const long Megabytes = 1048576L;

	public const long Gigabytes = 1073741824L;

	public const long Terabytes = 1099511627776L;

	public const long Petabytes = 1125899906842624L;

	public static TimeSpan? DebuggerTimeout { get; set; } = TimeSpan.FromDays(1);

	public static bool IsConnectionRefusedException(Exception ex)
	{
		if (!(ex is SocketException { SocketErrorCode: SocketError.ConnectionRefused }))
		{
			if (ex.InnerException is SocketException ex3)
			{
				return ex3.SocketErrorCode == SocketError.ConnectionRefused;
			}
			return false;
		}
		return true;
	}

	public static bool IsSocketClosedException(Exception ex)
	{
		if (ex is ObjectDisposedException || ex is IOException || ex is SocketException)
		{
			return true;
		}
		return false;
	}

	public static IPEndPoint GetFreeTcpEndPoint(IPAddress ipAddress, int defaultPort = 0)
	{
		try
		{
			TcpListener tcpListener = new TcpListener(ipAddress, defaultPort);
			tcpListener.Start();
			int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
			tcpListener.Stop();
			return new IPEndPoint(ipAddress, port);
		}
		catch when (defaultPort != 0)
		{
			return GetFreeTcpEndPoint(ipAddress);
		}
	}

	public static IPEndPoint GetFreeUdpEndPoint(IPAddress ipAddress, int defaultPort = 0)
	{
		try
		{
			using UdpClient udpClient = new UdpClient(new IPEndPoint(ipAddress, defaultPort));
			IPEndPoint iPEndPoint = ((IPEndPoint)udpClient.Client.LocalEndPoint) ?? throw new InvalidOperationException("Failed to get local endpoint for UDP client.");
			return new IPEndPoint(ipAddress, iPEndPoint.Port);
		}
		catch when (defaultPort != 0)
		{
			return GetFreeUdpEndPoint(ipAddress);
		}
	}

	public static TaskCompletionSource CreateCompletedTcs()
	{
		TaskCompletionSource taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		taskCompletionSource.SetResult();
		return taskCompletionSource;
	}

	private static string FixBase64String(string base64)
	{
		base64 = base64.Trim();
		int num = base64.Length % 4;
		if (num > 0)
		{
			base64 = base64.PadRight(base64.Length + (4 - num), '=');
		}
		return base64;
	}

	public static byte[] ConvertFromBase64AndFixPadding(string base64)
	{
		try
		{
			return Convert.FromBase64String(base64);
		}
		catch (FormatException)
		{
			return Convert.FromBase64String(FixBase64String(base64));
		}
	}

	public static void DirectoryCopy(string sourcePath, string destinationPath, bool recursive)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(sourcePath);
		if (!directoryInfo.Exists)
		{
			throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourcePath);
		}
		DirectoryInfo[] directories = directoryInfo.GetDirectories();
		Directory.CreateDirectory(destinationPath);
		FileInfo[] files = directoryInfo.GetFiles();
		foreach (FileInfo fileInfo in files)
		{
			string destFileName = Path.Combine(destinationPath, fileInfo.Name);
			fileInfo.CopyTo(destFileName, overwrite: false);
		}
		if (recursive)
		{
			DirectoryInfo[] array = directories;
			foreach (DirectoryInfo directoryInfo2 in array)
			{
				string destinationPath2 = Path.Combine(destinationPath, directoryInfo2.Name);
				DirectoryCopy(directoryInfo2.FullName, destinationPath2, recursive);
			}
		}
	}

	public static T[] SafeToArray<T>(object lockObject, IEnumerable<T> collection)
	{
		lock (lockObject)
		{
			return collection.ToArray();
		}
	}

	public static Task<T> RunTask<T>(Task<T> task, CancellationToken cancellationToken = default(CancellationToken))
	{
		return RunTask(task, TimeSpan.Zero, cancellationToken);
	}

	public static async Task<T> RunTask<T>(Task<T> task, TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken))
	{
		await RunTask((Task)task, timeout, cancellationToken).Vhc();
		return await task.Vhc();
	}

	public static Task RunTask(Task task, CancellationToken cancellationToken = default(CancellationToken))
	{
		return RunTask(task, TimeSpan.Zero, cancellationToken);
	}

	public static async Task RunTask(Task task, TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (timeout == TimeSpan.Zero)
		{
			timeout = Timeout.InfiniteTimeSpan;
		}
		Task timeoutTask = Task.Delay(timeout, cancellationToken);
		if (await Task.WhenAny(task, timeoutTask).Vhc() == timeoutTask)
		{
			if (timeoutTask.IsCompletedSuccessfully)
			{
				throw new TimeoutException();
			}
			cancellationToken.ThrowIfCancellationRequested();
		}
		await task.Vhc();
	}

	public static bool IsNullOrEmpty<T>([NotNullWhen(false)] IEnumerable<T>? array)
	{
		if (array != null)
		{
			return !array.Any();
		}
		return true;
	}

	public static bool IsNullOrEmpty<T>([NotNullWhen(false)] T[]? array)
	{
		if (array != null)
		{
			return array.Length == 0;
		}
		return true;
	}

	public static bool SequenceEquals<T>(IEnumerable<T>? first, IEnumerable<T>? second)
	{
		if (first == null)
		{
			first = Array.Empty<T>();
		}
		if (second == null)
		{
			second = Array.Empty<T>();
		}
		return first.SequenceEqual(second);
	}

	public static IEnumerable<string> ParseArguments(string commandLine)
	{
		if (string.IsNullOrWhiteSpace(commandLine))
		{
			yield break;
		}
		StringBuilder sb = new StringBuilder();
		bool inQuote = false;
		foreach (char c in commandLine)
		{
			if (c == '"' && !inQuote)
			{
				inQuote = true;
			}
			else if (c != '"' && (!char.IsWhiteSpace(c) || inQuote))
			{
				sb.Append(c);
			}
			else if (sb.Length > 0)
			{
				string text = sb.ToString();
				sb.Clear();
				inQuote = false;
				yield return text;
			}
		}
		if (sb.Length > 0)
		{
			yield return sb.ToString();
		}
	}

	public static byte[] GenerateKey()
	{
		return GenerateKey(128);
	}

	public static byte[] GenerateKey(int keySizeInBit)
	{
		using Aes aes = Aes.Create();
		aes.KeySize = keySizeInBit;
		aes.GenerateKey();
		return aes.Key;
	}

	public static byte[] EncryptClientId(string clientId, byte[] key)
	{
		using Aes aes = Aes.Create();
		aes.Mode = CipherMode.CBC;
		aes.Key = key;
		aes.IV = new byte[key.Length];
		aes.Padding = PaddingMode.None;
		Guid result;
		byte[] array = (Guid.TryParse(clientId, out result) ? result.ToByteArray() : Encoding.UTF8.GetBytes(clientId));
		using ICryptoTransform cryptoTransform = aes.CreateEncryptor();
		return cryptoTransform.TransformFinalBlock(array, 0, array.Length);
	}

	public static string GetBufferMd5(byte[] value)
	{
		using MD5 mD = MD5.Create();
		return BitConverter.ToString(mD.ComputeHash(value)).Replace("-", "");
	}

	public static string GetStringMd5(string value)
	{
		using MD5 mD = MD5.Create();
		return BitConverter.ToString(mD.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
	}

	public static string RedactHostName(string hostName)
	{
		if (hostName.Length > 8)
		{
			return hostName.Substring(0, 2) + "***" + hostName.Substring(hostName.Length - 4);
		}
		return "***" + hostName.Substring(hostName.Length - 4);
	}

	public static string RedactEndPoint(IPEndPoint ipEndPoint)
	{
		return RedactIpAddress(ipEndPoint.Address) + ":" + ipEndPoint.Port;
	}

	public static string RedactIpAddress(IPAddress ipAddress)
	{
		Span<byte> buffer = stackalloc byte[16];
		Span<byte> addressBytesFast = ipAddress.GetAddressBytesFast(buffer);
		if (ipAddress.IsV4() && !ipAddress.Equals(IPAddress.Any) && !ipAddress.Equals(IPAddress.Loopback))
		{
			return $"{addressBytesFast[0]}.*.*.{addressBytesFast[3]}";
		}
		if (ipAddress.IsV6() && !ipAddress.Equals(IPAddress.IPv6Any) && !ipAddress.Equals(IPAddress.IPv6Loopback))
		{
			return $"{addressBytesFast[0]:x2}{addressBytesFast[1]:x2}:***:{addressBytesFast[14]:x2}{addressBytesFast[15]:x2}";
		}
		return ipAddress.ToString();
	}

	private static double Round(double value, bool round)
	{
		if (!round)
		{
			return value;
		}
		return Math.Round(value);
	}

	public static string FormatBytes(long size, bool use1024 = true, bool round = false)
	{
		double num = (use1024 ? 1024.0 : 1000.0);
		double num2 = num * num;
		double num3 = num2 * num;
		double num4 = num3 * num;
		bool num5 = size < 0;
		double num6 = Math.Abs((double)size);
		string text = (num5 ? "-" : "");
		if (num6 >= num4)
		{
			return text + Round(num6 / num4, round).ToString("0.## ") + "TB";
		}
		if (num6 >= num3)
		{
			return text + Round(num6 / num3, round).ToString("0.# ") + "GB";
		}
		if (num6 >= num2)
		{
			return text + Round(num6 / num2, round).ToString("0 ") + "MB";
		}
		if (num6 >= num)
		{
			return text + Round(num6 / num, round).ToString("0 ") + "KB";
		}
		if (num6 > 0.0)
		{
			return text + num6.ToString("0 ") + "B";
		}
		return "0";
	}

	public static string FormatMegaBytes(long sizeMb, bool use1024 = false, bool round = false)
	{
		long num = (use1024 ? 1024 : 1000);
		long num2 = num;
		long num3 = num2 * num;
		long num4 = num3 * num;
		bool num5 = sizeMb < 0;
		double num6 = Math.Abs((double)sizeMb);
		string text = (num5 ? "-" : "");
		if (num6 >= (double)num4)
		{
			return text + GetVal(num6 / (double)num4).ToString("0.## ") + "PB";
		}
		if (num6 >= (double)num3)
		{
			return text + GetVal(num6 / (double)num3).ToString("0.# ") + "TB";
		}
		if (num6 >= (double)num2)
		{
			return text + GetVal(num6 / (double)num2).ToString("0 ") + "GB";
		}
		if (num6 > 0.0)
		{
			return text + num6.ToString("0 ") + "MB";
		}
		return "0";
		double GetVal(double val)
		{
			if (!round)
			{
				return val;
			}
			return Math.Round(val);
		}
	}

	public static string FormatBits(long bytes)
	{
		bytes *= 8;
		if (bytes >= 1073741824)
		{
			return ((double)(bytes / 1073741824)).ToString("0.# ") + "Gbps";
		}
		if (bytes >= 1048576)
		{
			return ((double)(bytes / 1048576)).ToString("0 ") + "Mbps";
		}
		if (bytes >= 1024)
		{
			return ((double)(bytes / 1024)).ToString("0 ") + "Kbps";
		}
		if (bytes > 0)
		{
			return ((double)bytes).ToString("0 ") + "bps";
		}
		return bytes.ToString("0");
	}

	public static bool IsInfinite(TimeSpan timeSpan)
	{
		if (!(timeSpan == TimeSpan.MaxValue))
		{
			return timeSpan == Timeout.InfiniteTimeSpan;
		}
		return true;
	}

	public static void ConfigTcpClient(TcpClient tcpClient, int? sendBufferSize = null, int? receiveBufferSize = null, bool? reuseAddress = null, bool? keepAlive = null, bool noDelay = true)
	{
		tcpClient.NoDelay = noDelay;
		if (sendBufferSize > 0)
		{
			tcpClient.SendBufferSize = sendBufferSize.Value;
		}
		if (receiveBufferSize > 0)
		{
			tcpClient.ReceiveBufferSize = receiveBufferSize.Value;
		}
		if (reuseAddress.HasValue)
		{
			tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, reuseAddress.Value);
		}
		if (keepAlive.HasValue)
		{
			tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, keepAlive.Value);
		}
	}

	public static bool IsTcpClientHealthy(TcpClient tcpClient)
	{
		try
		{
			if (!tcpClient.Connected)
			{
				return false;
			}
			Socket client = tcpClient.Client;
			return client != null && client.Connected && !client.Poll(1, SelectMode.SelectError);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static T GetRequiredInstance<T>(T? obj)
	{
		if (obj == null)
		{
			throw new InvalidOperationException($"{typeof(T)} has not been initialized yet.");
		}
		return obj;
	}

	public static DateTime RemoveMilliseconds(DateTime dateTime)
	{
		return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Kind);
	}

	public static string GetAssemblyMetadata(Assembly assembly, string key, string defaultValue)
	{
		AssemblyMetadataAttribute assemblyMetadataAttribute = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault((AssemblyMetadataAttribute attr) => attr.Key == key);
		if (!string.IsNullOrEmpty(assemblyMetadataAttribute?.Value))
		{
			return assemblyMetadataAttribute.Value;
		}
		return defaultValue;
	}

	public static Exception? TryDeleteFile(string filePath)
	{
		try
		{
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
			return null;
		}
		catch (Exception result)
		{
			return result;
		}
	}

	public static void Shuffle<T>(T[] array)
	{
		Random random = new Random();
		for (int num = array.Length - 1; num > 0; num--)
		{
			int num2 = random.Next(num + 1);
			int num3 = num;
			int num4 = num2;
			T val = array[num2];
			T val2 = array[num];
			array[num3] = val;
			array[num4] = val2;
		}
	}

	public static string? TryGetCountryName(string? countryCode)
	{
		if (string.IsNullOrEmpty(countryCode))
		{
			return null;
		}
		try
		{
			return new RegionInfo(countryCode).EnglishName;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static string GetCurrencySymbol(string currencyCode)
	{
		CultureInfo cultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(delegate(CultureInfo c)
		{
			try
			{
				return new RegionInfo(c.LCID).ISOCurrencySymbol == currencyCode;
			}
			catch
			{
				return false;
			}
		});
		if (cultureInfo == null)
		{
			return currencyCode;
		}
		return new RegionInfo(cultureInfo.LCID).CurrencySymbol;
	}

	public static async Task TryInvokeAsync(string? actionName, Func<ValueTask> task)
	{
		try
		{
			await task().Vhc();
		}
		catch (Exception ex)
		{
			LogInvokeError(ex, actionName);
		}
	}

	public static async Task TryInvokeAsync(string? actionName, Func<Task> task)
	{
		try
		{
			await task().Vhc();
		}
		catch (Exception ex)
		{
			LogInvokeError(ex, actionName);
		}
	}

	public static async ValueTask<T?> TryInvokeAsync<T>(string? actionName, Func<ValueTask<T>> task, T? defaultValue = default(T?))
	{
		try
		{
			return await task().Vhc();
		}
		catch (Exception ex)
		{
			LogInvokeError(ex, actionName);
			return defaultValue;
		}
	}

	public static async Task<T?> TryInvokeAsync<T>(string? actionName, Func<Task<T>> task, T? defaultValue = default(T?))
	{
		try
		{
			return await task().Vhc();
		}
		catch (Exception ex)
		{
			LogInvokeError(ex, actionName);
			return defaultValue;
		}
	}

	public static void TryInvoke(Action action)
	{
		TryInvoke((string?)null, action);
	}

	public static void TryInvoke(string? actionName, Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			LogInvokeError(ex, actionName);
		}
	}

	public static T? TryInvoke<T>(Func<T> func, T? defaultValue = default(T?))
	{
		return TryInvoke(null, func, defaultValue);
	}

	public static T? TryInvoke<T>(string? actionName, Func<T> func, T? defaultValue = default(T?))
	{
		try
		{
			return func();
		}
		catch (Exception ex)
		{
			LogInvokeError(ex, actionName);
			return defaultValue;
		}
	}

	private static void LogInvokeError(Exception ex, string? actionName)
	{
		if (!string.IsNullOrEmpty(actionName))
		{
			VhLogger.Instance.LogDebug(ex, "Could not invoke {ActionDesc}", actionName);
		}
	}

	public static Uri? TryParseUrl(string? stringUrl, UriKind uriKind = UriKind.Absolute)
	{
		if (!Uri.TryCreate(stringUrl, uriKind, out Uri result))
		{
			return null;
		}
		return result;
	}
}
