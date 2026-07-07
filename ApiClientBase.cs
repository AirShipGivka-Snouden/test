using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Toolkit.ApiClients;

public class ApiClientBase : ApiClientCommon
{
	private class HttpNoResult
	{
	}

	protected readonly struct HttpResult<T>
	{
		public required HttpResponseMessage ResponseMessage { get; init; }

		public required T Object { get; init; }

		public required string Text { get; init; }
	}

	protected HttpClient? HttpClient;

	protected readonly Lazy<JsonSerializerOptions> Settings;

	protected JsonSerializerOptions JsonSerializerSettings => Settings.Value;

	public ILogger Logger { get; set; } = NullLogger.Instance;

	public EventId LoggerEventId { get; set; }

	public bool ReadResponseAsString { get; set; }

	public ApiClientBase(HttpClient httpClient)
	{
		HttpClient = httpClient;
		Settings = new Lazy<JsonSerializerOptions>(CreateSerializerSettings);
	}

	protected ApiClientBase()
	{
		Settings = new Lazy<JsonSerializerOptions>(CreateSerializerSettings);
	}

	protected virtual JsonSerializerOptions CreateSerializerSettings()
	{
		return new JsonSerializerOptions();
	}

	protected virtual async Task<HttpResult<T?>> ReadObjectResponseAsync<T>(HttpResponseMessage response, IReadOnlyDictionary<string, IEnumerable<string>> headers, CancellationToken cancellationToken)
	{
		if (ReadResponseAsString)
		{
			string text = await response.Content.ReadAsStringAsync(cancellationToken).Vhc();
			try
			{
				T val = JsonSerializer.Deserialize<T>(text, JsonSerializerSettings);
				return new HttpResult<T>
				{
					ResponseMessage = response,
					Object = val,
					Text = text
				};
			}
			catch (JsonException innerException)
			{
				throw new ApiException("Could not deserialize the response body string as " + typeof(T).FullName + ".", (int)response.StatusCode, text, headers, innerException);
			}
		}
		try
		{
			HttpResult<T?> result;
			await using (Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).Vhc())
			{
				T val2 = await JsonSerializer.DeserializeAsync<T>(responseStream, JsonSerializerSettings, cancellationToken).Vhc();
				result = new HttpResult<T>
				{
					ResponseMessage = response,
					Object = val2,
					Text = string.Empty
				};
			}
			return result;
		}
		catch (JsonException innerException2)
		{
			throw new ApiException("Could not deserialize the response body stream as " + typeof(T).FullName + ".", (int)response.StatusCode, string.Empty, headers, innerException2);
		}
	}

	protected string ConvertToString(object? value, CultureInfo cultureInfo)
	{
		if (value == null)
		{
			return "";
		}
		if (value is Enum)
		{
			string name = Enum.GetName(value.GetType(), value);
			if (name != null)
			{
				FieldInfo declaredField = value.GetType().GetTypeInfo().GetDeclaredField(name);
				if (declaredField != null && declaredField.GetCustomAttribute(typeof(EnumMemberAttribute)) is EnumMemberAttribute enumMemberAttribute)
				{
					return enumMemberAttribute.Value ?? name;
				}
				return Convert.ToString(Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()), cultureInfo)) ?? "";
			}
		}
		else
		{
			if (value is bool flag)
			{
				return Convert.ToString(flag, cultureInfo).ToLowerInvariant();
			}
			if (value is byte[] inArray)
			{
				return Convert.ToBase64String(inArray);
			}
			if (value.GetType().IsArray)
			{
				IEnumerable<object> source = ((Array)value).OfType<object>();
				return string.Join(",", source.Select((object o) => ConvertToString(o, cultureInfo)));
			}
		}
		return Convert.ToString(value, cultureInfo) ?? "";
	}

	protected async Task<string> HttpSendAsync(HttpMethod httpMethod, string urlPart, Dictionary<string, object?>? parameters = null, object? data = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		return (await HttpSendExAsync<HttpNoResult>(httpMethod, urlPart, parameters, data, cancellationToken).Vhc()).Text;
	}

	protected async Task<T> HttpSendAsync<T>(HttpMethod httpMethod, string urlPart, Dictionary<string, object?>? parameters = null, object? data = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		return (await HttpSendExAsync<T>(httpMethod, urlPart, parameters, data, cancellationToken).Vhc()).Object;
	}

	protected async Task<HttpResult<T>> HttpSendExAsync<T>(HttpMethod httpMethod, string urlPart, Dictionary<string, object?>? parameters = null, object? data = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		using HttpRequestMessage request = new HttpRequestMessage();
		request.Method = httpMethod;
		request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
		if (httpMethod != HttpMethod.Get)
		{
			StringContent stringContent = new StringContent(JsonSerializer.Serialize(data, JsonSerializerSettings));
			stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
			request.Content = stringContent;
		}
		return await HttpSendAsync<T>(urlPart, parameters, request, cancellationToken).Vhc();
	}

	protected async Task<string> HttpSendAsync(string urlPart, Dictionary<string, object?>? parameters, HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return (await HttpSendAsync<HttpNoResult>(urlPart, parameters, request, cancellationToken).Vhc()).Text;
	}

	protected virtual async Task<HttpResult<T>> HttpSendAsync<T>(string urlPart, Dictionary<string, object?>? parameters, HttpRequestMessage request, CancellationToken cancellationToken)
	{
		try
		{
			HttpResult<T> result = await HttpSendAsyncImpl<T>(urlPart, parameters, request, cancellationToken).Vhc();
			Logger.LogInformation(LoggerEventId, "API Called. Method: {Method}, Uri: {RequestUri} => StatusCode: {StatusCode}.", request.Method, request.RequestUri, result.ResponseMessage.StatusCode);
			return result;
		}
		catch (ApiException ex)
		{
			Logger.LogError(LoggerEventId, ex, "API Called. Method: {Method}, Uri: {RequestUri} => StatusCode: {StatusCode}.", request.Method, request.RequestUri, ex.StatusCode);
			throw;
		}
		catch (Exception exception)
		{
			Logger.LogError(LoggerEventId, exception, "API Called. Method: {Method}, Uri: {RequestUri}, Failed.", request.Method, request.RequestUri);
			throw;
		}
	}

	private async Task<HttpResult<T>> HttpSendAsyncImpl<T>(string urlPart, Dictionary<string, object?>? parameters, HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (parameters == null)
		{
			parameters = new Dictionary<string, object>();
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(urlPart);
		if (parameters.Any())
		{
			stringBuilder.Append("?");
			foreach (KeyValuePair<string, object> item in parameters.Where<KeyValuePair<string, object>>((KeyValuePair<string, object> x) => x.Value != null))
			{
				stringBuilder.Append(Uri.EscapeDataString(item.Key) + "=").Append(Uri.EscapeDataString(ConvertToString(item.Value, CultureInfo.InvariantCulture))).Append('&');
			}
			stringBuilder.Length--;
		}
		HttpClient client = HttpClient ?? throw new Exception("HttpClient has not been set.");
		await PrepareRequestAsync(client, request, stringBuilder, cancellationToken).Vhc();
		using HttpResponseMessage response = await HttpClientSendAsync(client, request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).Vhc();
		Dictionary<string, IEnumerable<string>> headers = response.Headers.ToDictionary<KeyValuePair<string, IEnumerable<string>>, string, IEnumerable<string>>((KeyValuePair<string, IEnumerable<string>> h) => h.Key, (KeyValuePair<string, IEnumerable<string>> h) => h.Value);
		if (response.Content?.Headers != null)
		{
			foreach (KeyValuePair<string, IEnumerable<string>> header in response.Content.Headers)
			{
				headers[header.Key] = header.Value;
			}
		}
		await ProcessResponseAsync(client, response, cancellationToken).Vhc();
		int status = (int)response.StatusCode;
		if (status >= 200 && status < 300)
		{
			if (typeof(T) == typeof(HttpNoResult))
			{
				return new HttpResult<T>
				{
					ResponseMessage = response,
					Object = default(T),
					Text = string.Empty
				};
			}
			HttpResult<T> result = await ReadObjectResponseAsync<T>(response, headers, cancellationToken).Vhc();
			if (result.Object == null)
			{
				throw new ApiException("Response was null which was not expected.", status, result.Text, headers, null);
			}
			return result;
		}
		string text = ((response.Content == null) ? null : (await response.Content.ReadAsStringAsync(cancellationToken).Vhc()));
		string response2 = text;
		throw new ApiException("The HTTP status code of the response was not expected (" + status + ").", status, response2, headers, null);
	}

	protected virtual Task<HttpResponseMessage> HttpClientSendAsync(HttpClient client, HttpRequestMessage request, HttpCompletionOption responseHeadersRead, CancellationToken cancellationToken)
	{
		return client.SendAsync(request, responseHeadersRead, cancellationToken);
	}

	protected Task<T> HttpGetAsync<T>(string urlPart, Dictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		return HttpSendAsync<T>(HttpMethod.Get, urlPart, parameters, null, cancellationToken);
	}

	protected Task<T> HttpPostAsync<T>(string urlPart, Dictionary<string, object?>? parameters, object? data, CancellationToken cancellationToken = default(CancellationToken))
	{
		return HttpSendAsync<T>(HttpMethod.Post, urlPart, parameters, data, cancellationToken);
	}

	protected Task HttpPostAsync(string urlPart, Dictionary<string, object?>? parameters, object? data, CancellationToken cancellationToken = default(CancellationToken))
	{
		return HttpSendAsync(HttpMethod.Post, urlPart, parameters, data, cancellationToken);
	}

	protected Task<T> HttpPutAsync<T>(string urlPart, Dictionary<string, object?>? parameters, object? data, CancellationToken cancellationToken = default(CancellationToken))
	{
		return HttpSendAsync<T>(HttpMethod.Put, urlPart, parameters, data, cancellationToken);
	}

	protected Task HttpPutAsync(string urlPart, Dictionary<string, object?>? parameters, object? data, CancellationToken cancellationToken = default(CancellationToken))
	{
		return HttpSendAsync(HttpMethod.Put, urlPart, parameters, data, cancellationToken);
	}

	protected Task<T> HttpPatchAsync<T>(string urlPart, Dictionary<string, object?>? parameters, object? data, CancellationToken cancellationToken = default(CancellationToken))
	{
		return HttpSendAsync<T>(HttpMethod.Put, urlPart, parameters, data, cancellationToken);
	}

	protected Task HttpPatchAsync(string urlPart, Dictionary<string, object?>? parameters, object? data, CancellationToken cancellationToken = default(CancellationToken))
	{
		return HttpSendAsync(HttpMethod.Patch, urlPart, parameters, data, cancellationToken);
	}

	protected Task HttpDeleteAsync(string urlPart, Dictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		return HttpSendAsync(HttpMethod.Delete, urlPart, parameters, null, cancellationToken);
	}
}
