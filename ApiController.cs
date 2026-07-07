using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Client.VpnServices.Abstractions;
using VpnHood.Core.Client.VpnServices.Abstractions.Requests;
using VpnHood.Core.Toolkit.ApiClients;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Streams;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling;

namespace VpnHood.Core.Client.VpnServices.Host;

internal class ApiController : IDisposable
{
	private int _isDisposed;

	private readonly VpnServiceHost _vpnHoodService;

	private readonly TcpListener _tcpListener;

	private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

	private VpnHoodClient VpnHoodClient => _vpnHoodService.RequiredClient;

	public IPEndPoint? ApiEndPoint { get; private set; }

	public byte[] ApiKey { get; } = VhUtils.GenerateKey(128);

	public VpnServiceHost? ServiceContext { get; set; }

	public ApiController(VpnServiceHost vpnHoodService)
	{
		_vpnHoodService = vpnHoodService;
		_tcpListener = new TcpListener(IPAddress.Loopback, 0);
		Start(_cancellationTokenSource.Token);
		VhLogger.Instance.LogDebug("VpnService ApiController has been started. EndPoint: {EndPoint}", ApiEndPoint);
	}

	private async Task Start(CancellationToken cancellationToken)
	{
		try
		{
			_tcpListener.Start();
			ApiEndPoint = (IPEndPoint)_tcpListener.LocalEndpoint;
			while (!cancellationToken.IsCancellationRequested)
			{
				ProcessClientAsync(await _tcpListener.AcceptTcpClientAsync(cancellationToken), cancellationToken);
			}
		}
		catch (Exception exception)
		{
			if (_isDisposed == 0)
			{
				VhLogger.Instance.LogError(exception, "VpnService host Listener has stopped.");
			}
		}
		finally
		{
			_tcpListener.Stop();
			VhLogger.Instance.LogDebug("VpnService host Listener has been stopped. EndPoint: {EndPoint}", ApiEndPoint);
			Dispose();
		}
	}

	private async Task ProcessClientAsync(TcpClient client, CancellationToken cancellationToken)
	{
		IPEndPoint clientEp = client.TryGetLocalEndPoint();
		try
		{
			await using NetworkStream stream = client.GetStream();
			byte[] array = await StreamUtils.ReadObjectAsync<byte[]>(stream, cancellationToken);
			if (!ApiKey.SequenceEqual(array))
			{
				throw new Exception("Invalid API key.");
			}
			while (!cancellationToken.IsCancellationRequested)
			{
				await ProcessRequest(stream, cancellationToken);
			}
		}
		catch (Exception exception)
		{
			if (_isDisposed == 0)
			{
				VhLogger.Instance.LogError(GeneralEventId.Test, exception, "Could not handle API request. ClientEp: {ClientEp}", clientEp);
			}
		}
		finally
		{
			client.Dispose();
		}
	}

	private async Task<ConnectionInfo> UpdateConnectionInfo(CancellationToken cancellationToken)
	{
		VpnHoodClient client = _vpnHoodService.Client;
		if (client != null)
		{
			await _vpnHoodService.UpdateConnectionInfo(client, cancellationToken);
		}
		return _vpnHoodService.Context.ConnectionInfo;
	}

	private async Task ProcessRequest(Stream stream, CancellationToken cancellationToken)
	{
		try
		{
			await ProcessRequestInternal(stream, cancellationToken);
		}
		catch (Exception ex) when (_isDisposed == 0)
		{
			ApiResponse<object> apiResponse = new ApiResponse<object>();
			ApiResponse<object> apiResponse2 = apiResponse;
			apiResponse2.ConnectionInfo = await UpdateConnectionInfo(cancellationToken);
			apiResponse.ApiError = ex.ToApiError();
			apiResponse.Result = null;
			await StreamUtils.WriteObjectAsync(stream, apiResponse, cancellationToken);
			if (ex is FormatException)
			{
				stream.Close();
				throw;
			}
		}
	}

	private async Task ProcessRequestInternal(Stream stream, CancellationToken cancellationToken)
	{
		string text = await StreamUtils.ReadObjectAsync<string>(stream, cancellationToken);
		VhLogger.Instance.LogTrace("ApiController is reading a request: {RequestType}", text);
		switch (text)
		{
		case "ApiGetConnectionInfoRequest":
			await GetConnectionInfo(await StreamUtils.ReadObjectAsync<ApiGetConnectionInfoRequest>(stream, cancellationToken), cancellationToken);
			await WriteResponseResult(stream, null, cancellationToken);
			break;
		case "ApiSetAdOkRequest":
			await SetAdOk(await StreamUtils.ReadObjectAsync<ApiSetAdOkRequest>(stream, cancellationToken), cancellationToken);
			await WriteResponseResult(stream, null, cancellationToken);
			break;
		case "ApiAdFailedRequest":
			await SetAdFailed(await StreamUtils.ReadObjectAsync<ApiAdFailedRequest>(stream, cancellationToken), cancellationToken);
			await WriteResponseResult(stream, null, cancellationToken);
			break;
		case "ApiSetWaitForAdRequest":
			await SetWaitForAd(await StreamUtils.ReadObjectAsync<ApiSetWaitForAdRequest>(stream, cancellationToken), cancellationToken);
			await WriteResponseResult(stream, null, cancellationToken);
			break;
		case "ApiReconfigureRequest":
			await Reconfigure(await StreamUtils.ReadObjectAsync<ApiReconfigureRequest>(stream, 16777215, cancellationToken), cancellationToken);
			await WriteResponseResult(stream, null, cancellationToken);
			break;
		case "ApiDisconnectRequest":
			await WriteResponseResult(stream, null, cancellationToken);
			await Disconnect(await StreamUtils.ReadObjectAsync<ApiDisconnectRequest>(stream, cancellationToken), cancellationToken);
			break;
		default:
			throw new InvalidOperationException("Unknown request type: " + text);
		}
	}

	private async Task WriteResponseResult(Stream stream, object? result, CancellationToken cancellationToken)
	{
		ApiResponse<object> apiResponse = new ApiResponse<object>();
		ApiResponse<object> apiResponse2 = apiResponse;
		apiResponse2.ConnectionInfo = await UpdateConnectionInfo(cancellationToken);
		apiResponse.ApiError = null;
		apiResponse.Result = result;
		await StreamUtils.WriteObjectAsync(stream, apiResponse, cancellationToken);
	}

	private Task GetConnectionInfo(ApiGetConnectionInfoRequest request, CancellationToken cancellationToken)
	{
		return UpdateConnectionInfo(cancellationToken);
	}

	private Task Disconnect(ApiDisconnectRequest request, CancellationToken cancellationToken)
	{
		return _vpnHoodService.TryDisconnect();
	}

	private Task Reconfigure(ApiReconfigureRequest request, CancellationToken cancellationToken)
	{
		VpnHoodClient.UseTcpProxy = request.Params.UseTcpProxy;
		VpnHoodClient.DropUdp = request.Params.DropUdp;
		VpnHoodClient.DropQuic = request.Params.DropQuic;
		VpnHoodClient.ChannelProtocol = request.Params.ChannelProtocol;
		VpnHoodClient.ProxyEndPointManager.UpdateOptions(request.Params.ProxyOptions);
		return Task.CompletedTask;
	}

	private Task SetWaitForAd(ApiSetWaitForAdRequest request, CancellationToken cancellationToken)
	{
		VpnHoodClient.RequiredSession.AdHandler.WaitForAd(CancellationToken.None);
		return Task.CompletedTask;
	}

	private async Task SetAdOk(ApiSetAdOkRequest request, CancellationToken cancellationToken)
	{
		ISessionAdHandler adHandler = VpnHoodClient.RequiredSession.AdHandler;
		if (request.IsRewarded && !string.IsNullOrEmpty(request.AdResult.AdData))
		{
			await adHandler.SendRewardedAdData(request.AdResult.AdData, cancellationToken);
		}
		else
		{
			adHandler.SetAdOk();
		}
	}

	private Task SetAdFailed(ApiAdFailedRequest request, CancellationToken cancellationToken)
	{
		VpnHoodClient.RequiredSession.AdHandler.SetAdFailed(request.ApiError?.ToException() ?? new Exception("Failed to load ad."));
		return Task.CompletedTask;
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, 1) != 1)
		{
			_cancellationTokenSource.Cancel();
			_tcpListener.Stop();
		}
	}
}
