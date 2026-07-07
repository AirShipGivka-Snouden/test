using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VpnHood.Core.Toolkit.ApiClients;

public abstract class ApiClientCommon
{
	public Uri? DefaultBaseAddress { get; set; }

	public AuthenticationHeaderValue? DefaultAuthorization { get; set; }

	public Dictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();

	protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, StringBuilder urlBuilder, CancellationToken cancellationToken)
	{
		return PrepareRequestAsync(client, request, urlBuilder.ToString(), cancellationToken);
	}

	protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, string url, CancellationToken cancellationToken)
	{
		request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
		if (DefaultBaseAddress != null && !request.RequestUri.IsAbsoluteUri)
		{
			request.RequestUri = new Uri(DefaultBaseAddress, request.RequestUri);
		}
		HttpRequestHeaders headers = request.Headers;
		if (headers.Authorization == null)
		{
			AuthenticationHeaderValue authenticationHeaderValue = (headers.Authorization = DefaultAuthorization);
		}
		foreach (KeyValuePair<string, string> defaultHeader in DefaultHeaders)
		{
			request.Headers.Add(defaultHeader.Key, defaultHeader.Value);
		}
		return Task.CompletedTask;
	}

	protected virtual Task ProcessResponseAsync(HttpClient client, HttpResponseMessage response, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}
