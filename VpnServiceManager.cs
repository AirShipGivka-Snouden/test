using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Client.Abstractions;
using VpnHood.Core.Client.Device;
using VpnHood.Core.Client.Device.UiContexts;
using VpnHood.Core.Client.VpnServices.Abstractions;
using VpnHood.Core.Client.VpnServices.Abstractions.Requests;
using VpnHood.Core.Client.VpnServices.Manager.Exceptions;
using VpnHood.Core.Toolkit.ApiClients;
using VpnHood.Core.Toolkit.Jobs;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Streams;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Client.VpnServices.Manager;

public class VpnServiceManager : IDisposable
{
	private const int VpnServiceUnreachableThreshold = 1;

	private readonly TimeSpan _requestVpnServiceTimeout = TimeSpan.FromSeconds(120L).WhenNoDebugger();

	private readonly TimeSpan _startVpnServiceTimeout = TimeSpan.FromSeconds(20L).WhenNoDebugger();

	private bool _disposed;

	private readonly TimeSpan _connectionInfoTimeSpan = TimeSpan.FromSeconds(1L);

	private readonly IDevice _device;

	private readonly string _vpnConfigFilePath;

	private readonly string _vpnStatusFilePath;

	private ConnectionInfo _connectionInfo;

	private DateTime? _connectionInfoRefreshedTime;

	private TcpClient? _tcpClient;

	private bool _isInitializing;

	private int _vpnServiceUnreachableCount;

	private CancellationTokenSource _updateConnectionInfoCts = new CancellationTokenSource();

	private ConnectionInfo? _lastConnectionInfo;

	private readonly Job _updateConnectionInfoJob;

	private readonly AsyncLock _connectionInfoLock = new AsyncLock();

	private readonly AsyncLock _sendRequestLock = new AsyncLock();

	private readonly AsyncLock _stopLock = new AsyncLock();

	public string LogFilePath => Path.Combine(_device.VpnServiceConfigFolder, "vpn.log");

	public ConnectionInfo ConnectionInfo
	{
		get
		{
			TryRefreshConnectionInfo(force: false, CancellationToken.None);
			return _connectionInfo;
		}
	}

	public bool IsStarted
	{
		get
		{
			if (!_isInitializing)
			{
				return ConnectionInfo.IsStarted();
			}
			return true;
		}
	}

	public bool IsReconfiguring { get; private set; }

	public event EventHandler? StateChanged;

	public event EventHandler<ClientReconfigureParams>? Reconfigured;

	public VpnServiceManager(IDevice device, TimeSpan? eventWatcherInterval)
	{
		Directory.CreateDirectory(device.VpnServiceConfigFolder);
		_vpnConfigFilePath = Path.Combine(device.VpnServiceConfigFolder, "vpn.config");
		_vpnStatusFilePath = Path.Combine(device.VpnServiceConfigFolder, "vpn.status");
		_device = device;
		_connectionInfo = JsonUtils.TryDeserializeFile<ConnectionInfo>(_vpnStatusFilePath) ?? BuildConnectionInfo(ClientState.None);
		_updateConnectionInfoJob = new Job(UpdateConnectionInfoJob, eventWatcherInterval ?? TimeSpan.MaxValue, "UpdateConnectionInfoJob");
	}

	private static ConnectionInfo BuildConnectionInfo(ClientState clientState, Exception? ex = null)
	{
		return new ConnectionInfo
		{
			ProxyManagerStatus = null,
			CreatedTime = null,
			SessionInfo = null,
			SessionStatus = null,
			ApiEndPoint = null,
			ApiKey = null,
			ClientState = clientState,
			ClientStateChangedTime = null,
			ClientStateProgress = null,
			Error = ex?.ToApiError()
		};
	}

	private ConnectionInfo SetConnectionInfo(ClientState clientState, Exception? ex = null)
	{
		_connectionInfo = BuildConnectionInfo(clientState, ex);
		try
		{
			File.WriteAllText(_vpnStatusFilePath, JsonSerializer.Serialize(_connectionInfo));
		}
		catch
		{
		}
		return _connectionInfo;
	}

	public async Task Start(ClientOptions clientOptions, CancellationToken cancellationToken)
	{
		try
		{
			if (IsStarted)
			{
				await TryStop().Vhc();
			}
			_isInitializing = true;
			_vpnServiceUnreachableCount = 0;
			await _updateConnectionInfoCts.TryCancelAsync().Vhc();
			_updateConnectionInfoCts.Dispose();
			_updateConnectionInfoCts = new CancellationTokenSource();
			_connectionInfo = SetConnectionInfo(ClientState.Initializing);
			await File.WriteAllTextAsync(_vpnConfigFilePath, JsonSerializer.Serialize(clientOptions), cancellationToken).Vhc();
			VhLogger.Instance.LogInformation("Requesting VpnService...");
			if (!clientOptions.UseNullCapture)
			{
				await _device.RequestVpnService(AppUiContext.Context, _requestVpnServiceTimeout, cancellationToken).Vhc();
			}
			VhLogger.Instance.LogInformation("Starting VpnService...");
			VhUtils.TryDeleteFile(LogFilePath);
			await _device.StartVpnService(cancellationToken).Vhc();
			await WaitForVpnService(cancellationToken).Vhc();
		}
		catch (Exception ex)
		{
			ConnectionInfo? connectionInfo = JsonUtils.TryDeserializeFile<ConnectionInfo>(_vpnStatusFilePath);
			if (connectionInfo != null && connectionInfo.ClientState == ClientState.Initializing)
			{
				SetConnectionInfo(ClientState.Disposed, ex);
			}
			throw;
		}
		finally
		{
			_isInitializing = false;
		}
		await WaitForConnection(cancellationToken).Vhc();
	}

	private async Task WaitForVpnService(CancellationToken cancellationToken)
	{
		VhLogger.Instance.LogInformation("Waiting for VpnService to start...");
		using CancellationTokenSource timeoutCts = new CancellationTokenSource(_startVpnServiceTimeout);
		try
		{
			using CancellationTokenSource localCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
			ConnectionInfo connectionInfo = JsonUtils.TryDeserializeFile<ConnectionInfo>(_vpnStatusFilePath);
			while (true)
			{
				bool flag = connectionInfo == null;
				if (!flag)
				{
					ClientState clientState = connectionInfo.ClientState;
					bool flag2 = (uint)clientState <= 1u;
					flag = flag2;
				}
				if (!flag)
				{
					break;
				}
				await Task.Delay(1000, localCts.Token).Vhc();
				connectionInfo = JsonUtils.TryDeserializeFile<ConnectionInfo>(_vpnStatusFilePath);
			}
			_connectionInfo = connectionInfo;
			_tcpClient = null;
			VhLogger.Instance.LogInformation("VpnService has started. EndPoint: {EndPoint}, ConnectionState: {ConnectionState}", connectionInfo.ApiEndPoint, connectionInfo.ClientState);
		}
		catch (Exception innerException) when (timeoutCts.IsCancellationRequested)
		{
			throw new VpnServiceTimeoutException($"Could not start the VpnService in {_startVpnServiceTimeout.TotalSeconds} seconds.", innerException)
			{
				TimeoutDuration = _startVpnServiceTimeout
			};
		}
	}

	private async Task WaitForConnection(CancellationToken cancellationToken)
	{
		VhLogger.Instance.LogInformation("Waiting for VpnService to establish a connection ...");
		while (true)
		{
			ConnectionInfo connectionInfo = ConnectionInfo;
			if (connectionInfo.Error != null)
			{
				throw ClientExceptionConverter.ApiErrorToException(connectionInfo.Error) ?? connectionInfo.Error.ToException();
			}
			ClientState clientState = connectionInfo.ClientState;
			if ((uint)(clientState - 10) <= 1u)
			{
				throw new Exception("VpnService could not establish any connection.");
			}
			if (connectionInfo.ClientState == ClientState.Connected)
			{
				break;
			}
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				await Task.Delay(_connectionInfoTimeSpan, cancellationToken).Vhc();
			}
			catch (Exception) when (cancellationToken.IsCancellationRequested)
			{
			}
		}
		VhLogger.Instance.LogDebug("The VpnService has established a connection.");
	}

	public Task RefreshState(CancellationToken cancellationToken)
	{
		return TryRefreshConnectionInfo(force: true, cancellationToken);
	}

	private async Task<ConnectionInfo> TryRefreshConnectionInfo(bool force, CancellationToken cancellationToken)
	{
		try
		{
			return await RefreshConnectionInfo(force, cancellationToken).Vhc();
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogError(exception, "Could not update connection info.");
			return _connectionInfo;
		}
	}

	private async Task<ConnectionInfo> RefreshConnectionInfo(bool force, CancellationToken cancellationToken)
	{
		if (_disposed)
		{
			return _connectionInfo;
		}
		if (!force && _connectionInfoLock.IsLocked)
		{
			return _connectionInfo;
		}
		using CancellationTokenSource updateCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _updateConnectionInfoCts.Token);
		using (await _connectionInfoLock.LockAsync(updateCts.Token).Vhc())
		{
			if (!_isInitializing)
			{
				if (!force)
				{
					DateTime now = FastDateTime.Now;
					DateTime? connectionInfoRefreshedTime = _connectionInfoRefreshedTime;
					if (now - connectionInfoRefreshedTime < _connectionInfoTimeSpan)
					{
						goto IL_015f;
					}
				}
				_connectionInfo = JsonUtils.TryDeserializeFile<ConnectionInfo>(_vpnStatusFilePath) ?? _connectionInfo;
				_connectionInfoRefreshedTime = FastDateTime.Now;
				if (_isInitializing || _connectionInfo.Error != null || !_connectionInfo.IsStarted())
				{
					CheckForEvents();
					return _connectionInfo;
				}
				try
				{
					await SendRequest(new ApiGetConnectionInfoRequest(), updateCts.Token).Vhc();
					_vpnServiceUnreachableCount = 0;
				}
				catch (Exception exception) when (_updateConnectionInfoCts.IsCancellationRequested)
				{
					VhLogger.Instance.LogWarning(exception, "Previous UpdateConnection Info has been ignored due to the new service.");
				}
				catch (Exception) when (_isInitializing)
				{
					throw;
				}
				catch (VpnServiceNotReadyException exception2)
				{
					if (_connectionInfo.ClientState != ClientState.Disposed)
					{
						VhLogger.Instance.LogDebug(exception2, "Could not update connection info.");
						_connectionInfo = SetConnectionInfo(ClientState.Disposed, _connectionInfo.Error?.ToException());
					}
				}
				catch (VpnServiceUnreachableException) when (_stopLock.IsLocked)
				{
					_connectionInfo = SetConnectionInfo(ClientState.None);
				}
				catch (VpnServiceUnreachableException ex3)
				{
					VhLogger.Instance.LogError(ex3, "VpnService is unreachable. EndPoint: {EndPoint}", _connectionInfo.ApiEndPoint);
					_vpnServiceUnreachableCount++;
					if (_vpnServiceUnreachableCount == 1)
					{
						_connectionInfo = SetConnectionInfo(ClientState.Disposed, ex3);
					}
					if (_vpnServiceUnreachableCount == 1)
					{
						VhLogger.Instance.LogError(ex3, "Could not update connection info.");
					}
				}
				catch (Exception exception3)
				{
					_vpnServiceUnreachableCount = 0;
					VhLogger.Instance.LogError(exception3, "Could not update connection info.");
				}
				CheckForEvents();
				_connectionInfoRefreshedTime = FastDateTime.Now;
				return _connectionInfo;
			}
			goto IL_015f;
			IL_015f:
			return _connectionInfo;
		}
	}

	private Task SendRequest(IApiRequest request, CancellationToken cancellationToken)
	{
		return SendRequest<object>(request, cancellationToken);
	}

	private async Task<T?> SendRequest<T>(IApiRequest request, CancellationToken cancellationToken)
	{
		ApiResponse<T> apiResponse = await SendRequestCore<T>(request, cancellationToken).Vhc();
		if (apiResponse.ConnectionInfo.CreatedTime >= _connectionInfo.CreatedTime)
		{
			_connectionInfo = apiResponse.ConnectionInfo;
			_connectionInfoRefreshedTime = FastDateTime.Now;
		}
		if (apiResponse.ApiError != null)
		{
			throw ClientExceptionConverter.ApiErrorToException(apiResponse.ApiError) ?? apiResponse.ApiError.ToException();
		}
		return apiResponse.Result;
	}

	private async Task<ApiResponse<T>> SendRequestCore<T>(IApiRequest request, CancellationToken cancellationToken)
	{
		using (await _sendRequestLock.LockAsync(cancellationToken).Vhc())
		{
			TcpClient tcpClient = _tcpClient;
			if (tcpClient != null)
			{
				try
				{
					await StreamUtils.WriteObjectAsync(tcpClient.GetStream(), request.GetType().Name, cancellationToken).Vhc();
					await StreamUtils.WriteObjectAsync(tcpClient.GetStream(), request, cancellationToken).Vhc();
					return await StreamUtils.ReadObjectAsync<ApiResponse<T>>(tcpClient.GetStream(), 16777215, cancellationToken).Vhc();
				}
				catch (Exception exception)
				{
					VhLogger.Instance.LogDebug(exception, "Could not send request to VpnService Host. EndPoint: {EndPoint}", tcpClient.TryGetRemoteEndPoint());
					tcpClient.Dispose();
					_tcpClient = null;
				}
			}
			ConnectionInfo connectionInfo = _connectionInfo;
			if (connectionInfo.Error != null)
			{
				throw new VpnServiceNotReadyException("VpnService is not ready.");
			}
			if (connectionInfo.ApiEndPoint == null)
			{
				throw new VpnServiceNotReadyException("ApiEndPoint is not available.");
			}
			if (connectionInfo.ApiKey == null)
			{
				throw new VpnServiceNotReadyException("ApiKey is not available.");
			}
			_tcpClient = await ConnectToVpnService(connectionInfo.ApiEndPoint, connectionInfo.ApiKey, cancellationToken).Vhc();
			try
			{
				await StreamUtils.WriteObjectAsync(_tcpClient.GetStream(), request.GetType().Name, cancellationToken).Vhc();
				await StreamUtils.WriteObjectAsync(_tcpClient.GetStream(), request, cancellationToken).Vhc();
				return await StreamUtils.ReadObjectAsync<ApiResponse<T>>(_tcpClient.GetStream(), 16777215, cancellationToken).Vhc();
			}
			catch (Exception innerException)
			{
				_tcpClient.Dispose();
				_tcpClient = null;
				throw new VpnServiceUnreachableException($"VpnService is unreachable. EndPoint: {connectionInfo.ApiEndPoint}", innerException);
			}
		}
	}

	private static async Task<TcpClient> ConnectToVpnService(IPEndPoint apiEndPoint, byte[] apiKey, CancellationToken cancellationToken)
	{
		VhLogger.Instance.LogDebug("Connecting to VpnService Host... EndPoint: {EndPoint}", apiEndPoint);
		TcpClient tcpClient = new TcpClient();
		try
		{
			await tcpClient.ConnectAsync(apiEndPoint, cancellationToken).Vhc();
			await StreamUtils.WriteObjectAsync(tcpClient.GetStream(), apiKey, cancellationToken).Vhc();
			VhLogger.Instance.LogDebug("Connected to VpnService Host. LocalEp: {LocalEp}, RemoteEp: {RemoteEp}", tcpClient.TryGetLocalEndPoint(), tcpClient.TryGetRemoteEndPoint());
			return tcpClient;
		}
		catch (Exception innerException)
		{
			tcpClient.Dispose();
			throw new VpnServiceUnreachableException($"VpnService is unreachable. EndPoint: {apiEndPoint}", innerException);
		}
	}

	public async Task<bool> TryStop(TimeSpan? timeout = null)
	{
		using (await _stopLock.LockAsync().Vhc())
		{
			if (!ConnectionInfo.IsStarted())
			{
				return true;
			}
			using CancellationTokenSource stopTimeoutCts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5L));
			try
			{
				VhLogger.Instance.LogDebug("Sending disconnect request...");
				await SendRequest(new ApiDisconnectRequest(), stopTimeoutCts.Token).Vhc();
			}
			catch (Exception exception)
			{
				VhLogger.Instance.LogDebug(exception, "Could not send disconnect request. ClientState: {ClientState}", ConnectionInfo.ClientState);
			}
			VhLogger.Instance.LogDebug("Waiting for VpnService to stop.");
			stopTimeoutCts.CancelAfter(TimeSpan.FromSeconds(5L));
			try
			{
				while (ConnectionInfo.IsStarted())
				{
					await RefreshConnectionInfo(force: true, stopTimeoutCts.Token).Vhc();
					await Task.Delay(200, stopTimeoutCts.Token).Vhc();
				}
				VhLogger.Instance.LogDebug("VpnService has been stopped.");
				return true;
			}
			catch (Exception exception2)
			{
				VhLogger.Instance.LogError(exception2, "Could not stop the VpnService.");
				return false;
			}
		}
	}

	private void CheckForEvents()
	{
		ConnectionInfo connectionInfo = _connectionInfo;
		if (_lastConnectionInfo?.ClientState != connectionInfo.ClientState || _lastConnectionInfo.SessionStatus?.IsAdapterStarted != connectionInfo.SessionStatus?.IsAdapterStarted)
		{
			VhLogger.Instance.LogDebug("The VpnService state has been changed. {OldSate} => {NewState}", _lastConnectionInfo?.ClientState, connectionInfo.ClientState);
			Task.Run(delegate
			{
				this.StateChanged?.Invoke(this, EventArgs.Empty);
			}, CancellationToken.None);
		}
		_lastConnectionInfo = connectionInfo;
	}

	public async Task Reconfigure(ClientReconfigureParams reconfigureParams, CancellationToken cancellationToken)
	{
		IsReconfiguring = true;
		try
		{
			if (IsStarted)
			{
				await SendRequest(new ApiReconfigureRequest
				{
					Params = reconfigureParams
				}, cancellationToken);
				this.Reconfigured?.Invoke(this, reconfigureParams);
			}
		}
		finally
		{
			IsReconfiguring = false;
		}
	}

	public Task SetWaitForAd(CancellationToken cancellationToken)
	{
		return SendRequest(new ApiSetWaitForAdRequest(), cancellationToken);
	}

	public Task SetAdOk(AdResult adResult, bool isRewarded, CancellationToken cancellationToken)
	{
		return SendRequest(new ApiSetAdOkRequest
		{
			AdResult = adResult,
			IsRewarded = isRewarded
		}, cancellationToken);
	}

	public Task SetAdFailed(Exception ex, CancellationToken cancellationToken)
	{
		return SendRequest(new ApiAdFailedRequest
		{
			ApiError = ex.ToApiError()
		}, cancellationToken);
	}

	private async ValueTask UpdateConnectionInfoJob(CancellationToken cancellationToken)
	{
		await RefreshConnectionInfo(force: false, cancellationToken).Vhc();
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_updateConnectionInfoJob.Dispose();
			_updateConnectionInfoCts.TryCancel();
			_updateConnectionInfoCts.Dispose();
			_tcpClient?.Dispose();
		}
	}
}
