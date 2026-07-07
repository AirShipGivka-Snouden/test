using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Ciphra.VPN.Common.Services;

public class RelayFallbackHandler : DelegatingHandler
{
	private readonly RelayDomainManager _relayManager;

	private static readonly HashSet<string> ApiHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "api.ciphravpn.com" };

	public RelayFallbackHandler(RelayDomainManager relayManager)
	{
		_relayManager = relayManager ?? throw new ArgumentNullException("relayManager");
	}

	private static bool IsApiRequest(Uri? uri)
	{
		if (uri == null)
		{
			return false;
		}
		return ApiHosts.Contains(uri.Host);
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		Uri originalUri = request.RequestUri;
		if (!IsApiRequest(originalUri) && !IsRelayHost(originalUri))
		{
			return await base.SendAsync(request, cancellationToken);
		}
		request.RequestUri = RewriteUri(originalUri, _relayManager.CurrentDomain);
		try
		{
			HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
			if (response.IsSuccessStatusCode || response.StatusCode < HttpStatusCode.InternalServerError)
			{
				_relayManager.ConfirmDomainWorking(_relayManager.CurrentDomain);
				return response;
			}
			HttpStatusCode statusCode = response.StatusCode;
			response.Dispose();
			throw new HttpRequestException($"Server error {statusCode} from {request.RequestUri?.Host}", null, statusCode);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex2) when (((ex2 is HttpRequestException || ex2 is OperationCanceledException) ? 1 : 0) != 0)
		{
			Log.Warning<string>(ex2, "Request to {Host} failed. Retrying on same domain before relay.", request.RequestUri?.Host);
			if (request.Content != null)
			{
				await request.Content.LoadIntoBufferAsync();
			}
			for (int retry = 0; retry < 3; retry++)
			{
				int delay = (retry + 1) * 500;
				await Task.Delay(delay, cancellationToken);
				HttpRequestMessage retryOnSame = await CloneRequestAsync(request, cancellationToken);
				retryOnSame.RequestUri = RewriteUri(originalUri, _relayManager.CurrentDomain);
				try
				{
					HttpResponseMessage sameResponse = await base.SendAsync(retryOnSame, cancellationToken);
					if (sameResponse.IsSuccessStatusCode || sameResponse.StatusCode < HttpStatusCode.InternalServerError)
					{
						_relayManager.ConfirmDomainWorking(_relayManager.CurrentDomain);
						return sameResponse;
					}
					sameResponse.Dispose();
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
				{
					throw;
				}
				catch
				{
				}
			}
			Log.Warning("Same-domain retry failed. Escalating to relay fallback.");
			string failureReason = ((ex2 is OperationCanceledException) ? "Timeout" : ClassifyFailure((HttpRequestException)ex2));
			string newDomain = await _relayManager.HandleDomainFailureAsync(failureReason, cancellationToken);
			if (newDomain == null)
			{
				Log.Warning("Domain fallback failed. Propagating original error.");
				throw;
			}
			Log.Information<string>("Retrying request with new domain: {Domain}", newDomain);
			try
			{
				HttpRequestMessage retryRequest = await CloneRequestAsync(request, cancellationToken);
				retryRequest.RequestUri = RewriteUri(originalUri, newDomain);
				HttpResponseMessage retryResponse = await base.SendAsync(retryRequest, cancellationToken);
				if (retryResponse.IsSuccessStatusCode)
				{
					_relayManager.ConfirmDomainWorking(newDomain);
				}
				return retryResponse;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex5)
			{
				Log.Warning<string>(ex5, "Relay retry to {Domain} also failed. Propagating original error.", newDomain);
				throw;
			}
		}
	}

	private bool IsRelayHost(Uri? uri)
	{
		if (uri == null)
		{
			return false;
		}
		string currentDomain = _relayManager.CurrentDomain;
		if (currentDomain.StartsWith("http://") || currentDomain.StartsWith("https://"))
		{
			try
			{
				return new Uri(currentDomain).Host.Equals(uri.Host, StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}
		return currentDomain.Equals(uri.Host, StringComparison.OrdinalIgnoreCase);
	}

	internal static Uri RewriteUri(Uri originalUri, string domain)
	{
		string uriString = ((!domain.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) ? ("https://" + domain.TrimEnd('/')) : domain.TrimEnd('/'));
		Uri uri = new Uri(uriString);
		UriBuilder uriBuilder = new UriBuilder(originalUri)
		{
			Scheme = uri.Scheme,
			Host = uri.Host,
			Port = (uri.IsDefaultPort ? (-1) : uri.Port)
		};
		return uriBuilder.Uri;
	}

	internal static string ClassifyFailure(HttpRequestException ex)
	{
		if (ex.StatusCode.HasValue)
		{
			return $"Http{(int)ex.StatusCode.Value}";
		}
		Exception innerException = ex.InnerException;
		if (innerException is SocketException { SocketErrorCode: var socketErrorCode })
		{
			if (1 == 0)
			{
			}
			string result;
			switch (socketErrorCode)
			{
			case SocketError.HostNotFound:
			case SocketError.TryAgain:
			case SocketError.NoData:
				result = "DNS";
				break;
			case SocketError.TimedOut:
				result = "Timeout";
				break;
			default:
				result = "NetworkError";
				break;
			}
			if (1 == 0)
			{
			}
			return result;
		}
		string message = ex.Message;
		if (message.Contains("timed out", StringComparison.OrdinalIgnoreCase) || message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
		{
			return "Timeout";
		}
		if (message.Contains("dns", StringComparison.OrdinalIgnoreCase) || message.Contains("name resolution", StringComparison.OrdinalIgnoreCase) || message.Contains("could not resolve", StringComparison.OrdinalIgnoreCase))
		{
			return "DNS";
		}
		return "NetworkError";
	}

	private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original, CancellationToken ct)
	{
		HttpRequestMessage clone = new HttpRequestMessage(original.Method, original.RequestUri);
		foreach (KeyValuePair<string, IEnumerable<string>> header in original.Headers)
		{
			clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}
		if (original.Content != null)
		{
			clone.Content = new ByteArrayContent(await original.Content.ReadAsByteArrayAsync(ct));
			foreach (KeyValuePair<string, IEnumerable<string>> header2 in original.Content.Headers)
			{
				clone.Content.Headers.TryAddWithoutValidation(header2.Key, header2.Value);
			}
		}
		clone.Version = original.Version;
		foreach (KeyValuePair<string, object> prop in (IEnumerable<KeyValuePair<string, object>>)original.Options)
		{
			clone.Options.TryAdd(prop.Key, prop.Value);
		}
		return clone;
	}
}
