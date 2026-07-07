using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Toolkit.Logging;

public static class VhLogger
{
	private class VhLoggerDecorator : ILogger
	{
		private readonly AotPreserveHelper _aotPreserveHelper = new AotPreserveHelper();

		public ILogger Logger { get; set; } = CreateConsoleLogger();

		public VhLoggerDecorator()
		{
			Logger.LogInformation("A new LoggerDecorator has been created. InstanceId: {InstanceId}", _aotPreserveHelper.PreserveTypes());
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (MinLogLevel <= logLevel)
			{
				VhLogger.Logged?.Invoke(null, new LoggedEventArgs(logLevel, eventId, formatter(state, exception), exception));
				Logger.Log(logLevel, eventId, state, exception, formatter);
			}
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return Logger.IsEnabled(logLevel);
		}

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			return Logger.BeginScope(state);
		}
	}

	private static readonly VhLoggerDecorator InstanceDecorator = new VhLoggerDecorator();

	public static ILogger Instance
	{
		get
		{
			return InstanceDecorator;
		}
		set
		{
			InstanceDecorator.Logger = ((value is VhLoggerDecorator vhLoggerDecorator) ? vhLoggerDecorator.Logger : value);
		}
	}

	public static EventId TcpCloseEventId { get; set; }

	public static bool IsAnonymousMode { get; set; } = true;

	public static LogLevel MinLogLevel { get; set; } = LogLevel.Information;

	public static event EventHandler<LoggedEventArgs>? Logged;

	public static ILogger CreateConsoleLogger(LogLevel logLevel = LogLevel.Trace, bool singleLine = false)
	{
		VhConsoleLoggerProvider provider = new VhConsoleLoggerProvider(includeScopes: true, singleLine);
		try
		{
			using ILoggerFactory loggerFactory = LoggerFactory.Create(delegate(ILoggingBuilder builder)
			{
				builder.AddProvider(provider);
				builder.SetMinimumLevel(logLevel);
			});
			return loggerFactory.CreateLogger("");
		}
		finally
		{
			if (provider != null)
			{
				((IDisposable)provider).Dispose();
			}
		}
	}

	public static string Format(EndPoint? endPoint)
	{
		if (endPoint == null)
		{
			return "<null>";
		}
		string text;
		if (!(endPoint is IPEndPoint endPoint2))
		{
			text = endPoint.ToString();
			if (text == null)
			{
				return "<null>";
			}
		}
		else
		{
			text = Format(endPoint2);
		}
		return text;
	}

	public static string Format(IpEndPointValue? endPoint)
	{
		return Format(endPoint?.ToIPEndPoint());
	}

	public static string Format(IPEndPoint? endPoint)
	{
		if (endPoint == null)
		{
			return "<null>";
		}
		AddressFamily addressFamily = endPoint.AddressFamily;
		if ((addressFamily == AddressFamily.InterNetwork || addressFamily == AddressFamily.InterNetworkV6) ? true : false)
		{
			return $"{Format(endPoint.Address)}:{endPoint.Port}";
		}
		return endPoint.ToString();
	}

	public static string Format(IEnumerable<IPAddress> ipAddresses)
	{
		return string.Join(", ", ipAddresses.Select(Format));
	}

	public static string Format(IPAddress? ipAddress)
	{
		if (ipAddress == null)
		{
			return "<null>";
		}
		if (!IsAnonymousMode)
		{
			return ipAddress.ToString();
		}
		return VhUtils.RedactIpAddress(ipAddress);
	}

	public static string Format(IEnumerable<IpNetwork> ipNetworks)
	{
		return string.Join(", ", ipNetworks.Select(Format));
	}

	public static string Format(IpNetwork? ipNetwork)
	{
		if (ipNetwork == null)
		{
			return "<null>";
		}
		return $"{Format(ipNetwork.Prefix)}/{ipNetwork.PrefixLength}";
	}

	public static string FormatType(object? obj)
	{
		return obj?.GetType().Name ?? "<null>";
	}

	public static string FormatType<T>()
	{
		return typeof(T).Name;
	}

	public static string FormatId(object? id)
	{
		if (id == null)
		{
			return "<null>";
		}
		string text = id.ToString() ?? "";
		if (!IsAnonymousMode)
		{
			return text;
		}
		return "**" + text.Substring(text.Length / 2);
	}

	public static string FormatSessionId(object? id)
	{
		return id?.ToString() ?? "<null>";
	}

	public static string FormatHostName(string? dnsName)
	{
		if (dnsName == null)
		{
			return "<null>";
		}
		if (IPAddress.TryParse(dnsName, out IPAddress address))
		{
			return Format(address);
		}
		if (IPEndPoint.TryParse(dnsName, out IPEndPoint result))
		{
			return Format(result);
		}
		if (!IsAnonymousMode)
		{
			return dnsName;
		}
		return VhUtils.RedactHostName(dnsName);
	}

	public static string FormatIpPacket(string ipPacketText)
	{
		if (!IsAnonymousMode)
		{
			return ipPacketText;
		}
		ipPacketText = RedactIpAddress(ipPacketText, "SourceAddress");
		ipPacketText = RedactIpAddress(ipPacketText, "DestinationAddress");
		ipPacketText = RedactIpAddress(ipPacketText, "Src");
		ipPacketText = RedactIpAddress(ipPacketText, "Dst");
		return ipPacketText;
	}

	private static string RedactIpAddress(string text, string keyText)
	{
		try
		{
			int num = text.IndexOf(keyText + "=", StringComparison.Ordinal) + 1;
			if (num == -1)
			{
				return text;
			}
			num += keyText.Length;
			int num2 = text.IndexOf(",", num, StringComparison.Ordinal);
			string text2 = text;
			int num3 = num;
			IPAddress ipAddress = IPAddress.Parse(text2.Substring(num3, num2 - num3));
			text = text.Substring(0, num) + Format(ipAddress) + text.Substring(num2);
			return text;
		}
		catch
		{
			return "*";
		}
	}

	public static bool IsSocketCloseException(Exception ex)
	{
		bool flag = ex.InnerException != null && IsSocketCloseException(ex.InnerException);
		if (!flag)
		{
			bool flag2 = ((ex is ObjectDisposedException || ex is OperationCanceledException || (ex is SocketException { SocketErrorCode: var socketErrorCode } && (socketErrorCode == SocketError.OperationAborted || (uint)(socketErrorCode - 10052) <= 2u || socketErrorCode == SocketError.ConnectionRefused))) ? true : false);
			flag = flag2;
		}
		return flag;
	}

	public static void LogError(EventId eventId, Exception ex, string message, params object?[] args)
	{
		if (IsSocketCloseException(ex))
		{
			Instance.LogDebug(TcpCloseEventId, message + " Message: " + ex.Message, args);
		}
		else
		{
			Instance.LogError(eventId, ex, message, args);
		}
	}
}
