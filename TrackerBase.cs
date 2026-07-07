using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Ga4.Trackers;

public abstract class TrackerBase : ITracker
{
	private static readonly Lazy<HttpClient> HttpClientLazy = new Lazy<HttpClient>(() => new HttpClient());

	private static HttpClient HttpClient => HttpClientLazy.Value;

	public required string MeasurementId { get; init; }

	public required string SessionId { get; set; }

	public required string ClientId { get; init; }

	public string? UserId { get; init; }

	public string UserAgent { get; set; } = Environment.OSVersion.ToString().Replace(" ", "");

	public bool IsEnabled { get; set; } = true;

	public bool IsAdminDebugView { get; set; }

	public ILogger? Logger { get; set; }

	public EventId LoggerEventId { get; set; } = new EventId(0, "Ga4Tracker");

	public bool ThrowExceptionOnError { get; set; }

	public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10L);

	public Dictionary<string, object> UserProperties { get; set; } = new Dictionary<string, object>();

	public abstract Task Track(IEnumerable<TrackEvent> trackEvents, CancellationToken cancellationToken);

	public Task Track(TrackEvent trackEvent, CancellationToken cancellationToken)
	{
		return Track(new _003C_003Ez__ReadOnlySingleElementList<TrackEvent>(trackEvent), cancellationToken);
	}

	public Task TrackError(string action, Exception ex, CancellationToken cancellationToken)
	{
		TrackEvent item = new TrackEvent
		{
			EventName = "exception",
			Parameters = new Dictionary<string, object>
			{
				{
					"page_location",
					"ex/" + action
				},
				{ "page_title", ex.Message }
			}
		};
		return Track(new _003C_003Ez__ReadOnlySingleElementList<TrackEvent>(item), cancellationToken);
	}

	protected void PrepareHttpHeaders(HttpHeaders httpHeaders)
	{
		httpHeaders.Add("User-Agent", UserAgent);
	}

	protected async Task SendHttpRequest(HttpRequestMessage requestMessage, string name, object? jsonData, CancellationToken cancellationToken)
	{
		if (!IsEnabled)
		{
			return;
		}
		try
		{
			using CancellationTokenSource timeoutCts = new CancellationTokenSource(RequestTimeout);
			using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
			Logger?.LogInformation(LoggerEventId, "Sending Ga4Track: {name}, Url: {Url},  Headers: {Headers}", name, requestMessage.RequestUri, JsonSerializer.Serialize(requestMessage.Headers, new JsonSerializerOptions
			{
				WriteIndented = true
			}));
			if (jsonData != null)
			{
				requestMessage.Content = new StringContent(JsonSerializer.Serialize(jsonData));
				requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
				string text = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions
				{
					WriteIndented = true
				});
				Logger?.LogInformation(LoggerEventId, "Ga4Track Data: {Data}", text);
			}
			string text2 = await (await HttpClient.SendAsync(requestMessage, linkedCts.Token).ConfigureAwait(continueOnCapturedContext: false)).Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(continueOnCapturedContext: false);
			Logger?.LogInformation(LoggerEventId, "Ga4Track Result: {Result}", text2);
		}
		catch (Exception exception)
		{
			Logger?.LogError(LoggerEventId, exception, "Ga4Track could not send its track");
			if (ThrowExceptionOnError)
			{
				throw;
			}
		}
	}

	public void UseSimpleLogger(bool singleLine = false)
	{
		using ILoggerFactory loggerFactory = LoggerFactory.Create(delegate(ILoggingBuilder builder)
		{
			builder.AddSimpleConsole(delegate(SimpleConsoleFormatterOptions configure)
			{
				configure.TimestampFormat = "[HH:mm:ss.ffff] ";
				configure.IncludeScopes = false;
				configure.SingleLine = singleLine;
			});
		});
		Logger = loggerFactory.CreateLogger("");
	}
}
