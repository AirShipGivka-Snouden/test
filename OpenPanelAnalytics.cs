using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services;
using Ciphra.VPN.Common.Services.Interfaces;
using Serilog;
using Windows.ApplicationModel;

namespace Ciphra.VPN.WinUI.Services;

public class OpenPanelAnalytics : IAppAnalytics
{
	private const string ApiUrl = "https://openpanel.ciphravpn.com/api/track";

	private const string ClientId = "f6c1df5e-0c71-4216-b263-9f9e80caadd1";

	private const string ClientSecret = "sec_1d9357e8141df3a4c414";

	private const string Platform = "Windows Store";

	private readonly HttpClient _httpClient;

	private readonly IDeviceIdService _deviceIdService;

	private readonly Settings _settings;

	private readonly string _appVersion;

	private readonly string _osVersion;

	private readonly string _sessionId;

	private readonly string _uid;

	private volatile string? _deviceId;

	public OpenPanelAnalytics(HttpClient httpClient, IDeviceIdService deviceIdService, Settings settings)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException("httpClient");
		_deviceIdService = deviceIdService ?? throw new ArgumentNullException("deviceIdService");
		_settings = settings ?? throw new ArgumentNullException("settings");
		_uid = settings.UserId;
		_appVersion = GetAppVersion();
		_osVersion = Environment.OSVersion.ToString();
		_sessionId = Guid.NewGuid().ToString();
		_httpClient.DefaultRequestHeaders.Add("openpanel-client-id", "f6c1df5e-0c71-4216-b263-9f9e80caadd1");
		_httpClient.DefaultRequestHeaders.Add("openpanel-client-secret", "sec_1d9357e8141df3a4c414");
	}

	public async Task IdentifyAsync()
	{
		try
		{
			DateTime createdAt = _settings.UserCreatedAt;
			_deviceId = await _deviceIdService.GetDeviceId();
			var payload = new
			{
				type = "identify",
				payload = new
				{
					profileId = _uid,
					properties = EnrichProperties(new Dictionary<string, object> { ["created_at"] = createdAt.ToString("o") })
				}
			};
			await SendToOpenPanelAsync(payload);
			Log.Information("User identified: {UserId}, DeviceId: {DeviceId}, CreatedAt: {CreatedAt}, AppVersion: {AppVersion}", new object[4] { _uid, _deviceId, createdAt, _appVersion });
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Debug<string>(ex2, "Failed to identify user: {Message}", ex2.Message);
		}
	}

	public void SendView(string viewName)
	{
		try
		{
			var payload = new
			{
				type = "track",
				payload = new
				{
					name = "Page View",
					profileId = _uid,
					properties = EnrichProperties(new Dictionary<string, object> { ["Page Name"] = viewName })
				}
			};
			SendToOpenPanelAsync(payload);
		}
		catch (Exception ex)
		{
			Log.Debug<string>(ex, "Failed to send view: {Message}", ex.Message);
		}
	}

	public void SendEvent(string eventName, params (string Key, string Value)[] properties)
	{
		try
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			for (int i = 0; i < properties.Length; i++)
			{
				(string, string) tuple = properties[i];
				if (!string.IsNullOrEmpty(tuple.Item1) && !string.IsNullOrEmpty(tuple.Item2))
				{
					dictionary[tuple.Item1] = tuple.Item2;
				}
			}
			EnrichProperties(dictionary);
			var payload = new
			{
				type = "track",
				payload = new
				{
					name = eventName,
					profileId = _uid,
					properties = dictionary
				}
			};
			SendToOpenPanelAsync(payload);
		}
		catch (Exception ex)
		{
			Log.Debug<string>(ex, "Failed to send event: {Message}", ex.Message);
		}
	}

	public void SendException(Exception exception, string? source = null, bool fatal = false)
	{
		try
		{
			ExceptionReport exceptionReport = ExceptionReportBuilder.TryBuild(exception, source, fatal);
			if (exceptionReport == null)
			{
				Log.Debug<string, string>("Suppressed duplicate exception report: {Type} from {Source}", exception.GetType().Name, source);
				return;
			}
			var payload = new
			{
				type = "track",
				payload = new
				{
					name = "Exception",
					profileId = _uid,
					properties = EnrichProperties(new Dictionary<string, object>
					{
						["Exception"] = exceptionReport.Message,
						["StackTrace"] = exceptionReport.StackTrace,
						["Type"] = exceptionReport.TypeName,
						["Source"] = exceptionReport.Source,
						["TargetSite"] = exceptionReport.TargetSite,
						["MethodName"] = exceptionReport.MethodName,
						["Fatal"] = exceptionReport.Fatal.ToString(),
						["InnerExceptionMessages"] = exceptionReport.InnerExceptionMessages,
						["OuterType"] = exceptionReport.OuterType,
						["Occurrence"] = exceptionReport.Occurrence.ToString()
					})
				}
			};
			SendToOpenPanelAsync(payload);
		}
		catch (Exception ex)
		{
			Log.Debug<string>(ex, "Failed to send exception: {Message}", ex.Message);
		}
	}

	public void TrackSubscriptionPurchaseIntent(SubscriptionType subType, string transactionId)
	{
		try
		{
			Log.Debug<SubscriptionType, string>("Tracking subscription purchase intent: {SubType}, TransactionId: {TransactionId}", subType, transactionId);
			string subscriptionLevel = GetSubscriptionLevel(subType);
			var payload = new
			{
				type = "track",
				payload = new
				{
					name = "Subscription Purchase Intent",
					profileId = _uid,
					properties = EnrichProperties(new Dictionary<string, object>
					{
						["Subscription Level"] = subscriptionLevel,
						["Transaction ID"] = transactionId
					})
				}
			};
			SendToOpenPanelAsync(payload);
		}
		catch (Exception ex)
		{
			Log.Debug<string>(ex, "Failed to track subscription purchase intent: {Message}", ex.Message);
		}
	}

	public void TrackSubscriptionPurchaseResult(SubscriptionType subType, string transactionId, bool success)
	{
		try
		{
			Log.Debug<SubscriptionType, string, bool>("Tracking subscription purchase result: {SubType}, TransactionId: {TransactionId}, Success: {Success}", subType, transactionId, success);
			string subscriptionLevel = GetSubscriptionLevel(subType);
			var payload = new
			{
				type = "track",
				payload = new
				{
					name = "Subscription Purchase Result",
					profileId = _uid,
					properties = EnrichProperties(new Dictionary<string, object>
					{
						["Subscription Level"] = subscriptionLevel,
						["Transaction ID"] = transactionId,
						["Success"] = success.ToString()
					})
				}
			};
			SendToOpenPanelAsync(payload);
		}
		catch (Exception ex)
		{
			Log.Debug<string>(ex, "Failed to track subscription purchase result: {Message}", ex.Message);
		}
	}

	public void TrackPaymentMethodSelected(SubscriptionType subType, string paymentMethod)
	{
		try
		{
			Log.Debug<SubscriptionType, string>("Tracking payment method selected: {SubType}, PaymentMethod: {PaymentMethod}", subType, paymentMethod);
			string subscriptionLevel = GetSubscriptionLevel(subType);
			var payload = new
			{
				type = "track",
				payload = new
				{
					name = "Payment Method Selected",
					profileId = _uid,
					properties = EnrichProperties(new Dictionary<string, object>
					{
						["Subscription Level"] = subscriptionLevel,
						["Payment Method"] = paymentMethod
					})
				}
			};
			SendToOpenPanelAsync(payload);
		}
		catch (Exception ex)
		{
			Log.Debug<string>(ex, "Failed to track payment method selected: {Message}", ex.Message);
		}
	}

	public void SendRelayFailureEvent(RelayFailureEntry entry)
	{
		try
		{
			Log.Debug<string, string>("Tracking relay domain failure: {Domain}, Reason: {Reason}", entry.Domain, entry.FailureReason);
			SendEvent("relay_domain_failed", ("domain", entry.Domain), ("failed_at", entry.FailedAt.ToString("o")), ("failure_reason", entry.FailureReason), ("requests_served", entry.RequestsServed.ToString()), ("time_in_use_minutes", ((int)entry.TimeInUse.TotalMinutes).ToString()), ("resolution_outcome", entry.ResolutionOutcome));
		}
		catch (Exception ex)
		{
			Log.Debug<string>(ex, "Failed to send relay failure event: {Message}", ex.Message);
		}
	}

	public Task FlushAsync()
	{
		return Task.CompletedTask;
	}

	private async Task SendToOpenPanelAsync(object payload)
	{
		try
		{
			string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			});
			StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
			CancellationTokenSource cts = new CancellationTokenSource();
			cts.CancelAfter(TimeSpan.FromSeconds(4L));
			(await _httpClient.PostAsync("https://openpanel.ciphravpn.com/api/track", content, cts.Token)).EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Debug<string>(ex2, "Failed to send data to OpenPanel: {Message}", ex2.Message);
		}
	}

	private Dictionary<string, object> EnrichProperties(Dictionary<string, object> properties)
	{
		properties["platform"] = "Windows Store";
		properties["app_version"] = _appVersion;
		properties["os_version"] = _osVersion;
		properties["session_id"] = _sessionId;
		properties["locale"] = CultureInfo.CurrentCulture.Name;
		if (_deviceId != null)
		{
			properties["device_id"] = _deviceId;
		}
		return properties;
	}

	private static string GetAppVersion()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			PackageId id = Package.Current.Id;
			return $"{id.Version.Major}.{id.Version.Minor}.{id.Version.Build}.{id.Version.Revision}";
		}
		catch
		{
			return "1.0.0.0";
		}
	}

	private string GetSubscriptionLevel(SubscriptionType subType)
	{
		if (1 == 0)
		{
		}
		string result = subType switch
		{
			SubscriptionType.M1 => "Monthly", 
			SubscriptionType.M3 => "Quarterly", 
			SubscriptionType.M12 => "Yearly", 
			_ => "Unknown", 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
