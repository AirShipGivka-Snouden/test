using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Ga4.Trackers;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Client.Abstractions;
using VpnHood.Core.Client.Abstractions.Exceptions;
using VpnHood.Core.Client.VpnServices.Abstractions;
using VpnHood.Core.Client.VpnServices.Abstractions.Tracking;
using VpnHood.Core.Filtering.Abstractions;
using VpnHood.Core.Toolkit.ApiClients;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Sockets;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling;
using VpnHood.Core.VpnAdapters.Abstractions;

namespace VpnHood.Core.Client.VpnServices.Host;

public class VpnServiceHost : IDisposable
{
	private readonly ApiController _apiController;

	private readonly IVpnServiceHandler _vpnServiceHandler;

	private readonly ISocketFactory _socketFactory;

	private readonly NetFilter? _netFilter;

	private readonly LogService? _logService;

	private CancellationTokenSource _connectCts = new CancellationTokenSource();

	private readonly TimeSpan _killServiceTimeout = TimeSpan.FromSeconds(3L);

	private int _isDisposed;

	private bool _disconnectRequested;

	internal VpnHoodClient? Client { get; private set; }

	internal VpnHoodClient RequiredClient => Client ?? throw new InvalidOperationException("Client is not initialized.");

	internal VpnServiceContext Context { get; }

	public static ConnectionInfo DefaultConnectionInfo => VpnServiceContext.DefaultConnectionInfo;

	public VpnServiceHost(string configFolder, IVpnServiceHandler vpnServiceHandler, ISocketFactory socketFactory, NetFilter? netFilter, bool withLogger = true)
	{
		Context = new VpnServiceContext(configFolder);
		_socketFactory = socketFactory;
		_netFilter = netFilter;
		_vpnServiceHandler = vpnServiceHandler;
		_logService = (withLogger ? new LogService(Context.LogFilePath) : null);
		VhLogger.TcpCloseEventId = GeneralEventId.Stream;
		ClientOptions clientOptions = Context.TryReadClientOptions();
		if (_logService != null && clientOptions != null)
		{
			_logService.Start(clientOptions.LogServiceOptions);
		}
		_apiController = new ApiController(this);
		VhLogger.Instance.LogInformation("VpnServiceHost has been initiated...ApiEndPoint: {_apiController}", _apiController.ApiEndPoint);
	}

	private void VpnHoodClient_StateChanged(object? sender, EventArgs e)
	{
		VpnHoodClient client = (VpnHoodClient)sender;
		if (client == null || Client != client)
		{
			return;
		}
		VhLogger.Instance.LogDebug("VpnService update the connection info file. State:{State}, LastError: {LastError}", client.State, client.Session?.Status.Error);
		UpdateConnectionInfo(client, CancellationToken.None);
		if (client.State != ClientState.Disposed)
		{
			_vpnServiceHandler.ShowNotification(Context.ConnectionInfo);
			return;
		}
		VhLogger.Instance.LogDebug("VpnServiceHost requests to stop the notification and service.");
		_vpnServiceHandler.StopNotification();
		Task.Delay(_killServiceTimeout).ContinueWith(delegate
		{
			if (client == Client)
			{
				VhLogger.Instance.LogDebug("VpnServiceHost requests to StopSelf.");
				_vpnServiceHandler.StopSelf();
			}
		});
	}

	public async Task<bool> TryConnect(bool forceReconnect = false, bool isAlwaysOn = false)
	{
		if (_isDisposed == 1)
		{
			return false;
		}
		try
		{
			_disconnectRequested = false;
			VpnHoodClient client = Client;
			bool flag;
			bool flag2;
			if (client != null)
			{
				flag = !forceReconnect;
				if (flag)
				{
					if (client != null)
					{
						ClientState state = client.State;
						if ((uint)(state - 10) <= 1u)
						{
							flag2 = true;
							goto IL_0073;
						}
					}
					flag2 = false;
					goto IL_0073;
				}
				goto IL_007a;
			}
			goto IL_019f;
			IL_0073:
			flag = !flag2;
			goto IL_007a;
			IL_019f:
			await _connectCts.TryCancelAsync();
			_connectCts.Dispose();
			_connectCts = new CancellationTokenSource();
			await Connect(isAlwaysOn, _connectCts.Token);
			return true;
			IL_007a:
			if (flag)
			{
				VhLogger.Instance.LogWarning("VpnService connection is already in progress.");
				await UpdateConnectionInfo(client, _connectCts.Token).Vhc();
				return false;
			}
			VhLogger.Instance.LogWarning("VpnService is killing the previous connection.");
			client.StateChanged -= VpnHoodClient_StateChanged;
			Client = null;
			client.DisposeAsync();
			goto IL_019f;
		}
		catch (Exception exception)
		{
			if (!_disconnectRequested)
			{
				VhLogger.Instance.LogError(exception, "VpnServiceHost could not establish the connection.");
			}
			return false;
		}
	}

	private async Task Connect(bool isAlwaysOn, CancellationToken cancellationToken)
	{
		if (Client != null)
		{
			throw new InvalidOperationException("Client is already initialized.");
		}
		try
		{
			ClientOptions clientOptions = Context.ReadClientOptions();
			_logService?.Start(clientOptions.LogServiceOptions, deleteOldReport: false);
			if (isAlwaysOn && !clientOptions.AllowAlwaysOn)
			{
				throw new AlwaysOnNotAllowedException("Auto start is only available for premium accounts.");
			}
			VhLogger.Instance.LogInformation("VpnService is connecting... ProcessId: {ProcessId}", Process.GetCurrentProcess().Id);
			await UpdateConnectionInfo(ClientState.Initializing, clientOptions.SessionName, null, cancellationToken).Vhc();
			_vpnServiceHandler.ShowNotification(Context.ConnectionInfo);
			clientOptions.ForceLogSni |= clientOptions.LogServiceOptions.LogEventNames.Contains<string>("Sni", StringComparer.OrdinalIgnoreCase);
			ITracker tracker = TryCreateTrackerFactory(clientOptions.TrackerFactoryAssemblyQualifiedName)?.TryCreateTracker(new TrackerCreateParams
			{
				ClientId = clientOptions.ClientId,
				ClientVersion = clientOptions.Version,
				Ga4MeasurementId = clientOptions.Ga4MeasurementId,
				UserAgent = clientOptions.UserAgent
			});
			VhLogger.Instance.LogDebug("VpnService is creating a new VpnHoodClient.");
			VpnAdapterSettings adapterSettings = new VpnAdapterSettings
			{
				AdapterName = clientOptions.AppName,
				Blocking = false,
				AutoDisposePackets = true
			};
			VpnServiceHost vpnServiceHost = this;
			IVpnAdapter vpnAdapter;
			if (!clientOptions.UseNullCapture)
			{
				vpnAdapter = _vpnServiceHandler.CreateAdapter(adapterSettings, clientOptions.DebugData1);
			}
			else
			{
				IVpnAdapter vpnAdapter2 = new NullVpnAdapter(autoDisposePackets: true, blocking: false);
				vpnAdapter = vpnAdapter2;
			}
			string configFolder = Context.ConfigFolder;
			NetFilter netFilter = _netFilter;
			ITracker tracker2 = tracker;
			vpnServiceHost.Client = new VpnHoodClient(vpnAdapter, _socketFactory, netFilter, configFolder, tracker2, clientOptions);
			Client.StateChanged += VpnHoodClient_StateChanged;
			await UpdateConnectionInfo(Client, cancellationToken).Vhc();
			_vpnServiceHandler.ShowNotification(Context.ConnectionInfo);
			await Client.Connect(cancellationToken).Vhc();
		}
		catch (Exception exception) when (Client == null)
		{
			await UpdateConnectionInfo(ClientState.Disposed, null, exception, _connectCts.Token).Vhc();
			_vpnServiceHandler.StopNotification();
			_vpnServiceHandler.StopSelf();
		}
		catch (Exception exception2)
		{
			VhLogger.Instance.LogError(exception2, "VpnServiceHost could not establish the connection.");
			Client?.DisposeAsync();
		}
	}

	public Task UpdateConnectionInfo(VpnHoodClient client, CancellationToken cancellationToken)
	{
		return UpdateConnectionInfo(client, null, cancellationToken);
	}

	public async Task UpdateConnectionInfo(VpnHoodClient client, Exception? ex, CancellationToken cancellationToken)
	{
		ConnectionInfo connectionInfo = new ConnectionInfo
		{
			CreatedTime = FastDateTime.Now,
			ProxyManagerStatus = client.ProxyEndPointManager.Status,
			SessionName = client.Config.SessionName,
			SessionInfo = client.Session?.Info,
			SessionStatus = client.Session?.Status.ToDto(),
			ClientState = client.State,
			ClientStateProgress = client.StateProgress,
			ClientStateChangedTime = client.StateChangedTime,
			Error = (ex?.ToApiError() ?? client.LastException?.ToApiError()),
			ApiEndPoint = _apiController.ApiEndPoint,
			ApiKey = _apiController.ApiKey
		};
		await Context.TryWriteConnectionInfo(connectionInfo, cancellationToken);
	}

	public async Task UpdateConnectionInfo(ClientState clientState, string? sessionName, Exception? exception, CancellationToken cancellationToken)
	{
		ConnectionInfo connectionInfo = new ConnectionInfo
		{
			CreatedTime = FastDateTime.Now,
			ApiEndPoint = _apiController.ApiEndPoint,
			ApiKey = _apiController.ApiKey,
			ProxyManagerStatus = null,
			SessionInfo = null,
			SessionStatus = null,
			ClientState = clientState,
			ClientStateProgress = null,
			ClientStateChangedTime = null,
			Error = exception?.ToApiError(),
			SessionName = sessionName
		};
		await Context.TryWriteConnectionInfo(connectionInfo, cancellationToken);
	}

	private static ITrackerFactory? TryCreateTrackerFactory(string? assemblyQualifiedName)
	{
		if (string.IsNullOrEmpty(assemblyQualifiedName))
		{
			return null;
		}
		try
		{
			Type type = Type.GetType(assemblyQualifiedName);
			if (type == null)
			{
				return null;
			}
			return Activator.CreateInstance(type) as ITrackerFactory;
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogError(exception, "Could not create tracker factory. ClassName: {className}", assemblyQualifiedName);
			return null;
		}
	}

	public async Task TryDisconnect(Exception? exception = null)
	{
		if (_isDisposed == 1)
		{
			return;
		}
		try
		{
			_disconnectRequested = true;
			if (exception != null)
			{
				VhLogger.Instance.LogError(exception, "VpnServiceHost is disconnecting due to an error...");
			}
			else
			{
				VhLogger.Instance.LogDebug(exception, "VpnServiceHost is disconnecting...");
			}
			VpnHoodClient client = Client;
			if (client != null)
			{
				await client.DisposeAsync();
			}
			if (exception != null)
			{
				await UpdateConnectionInfo(ClientState.Disposed, null, exception, CancellationToken.None);
			}
		}
		catch (Exception exception2)
		{
			VhLogger.Instance.LogError(exception2, "Could not disconnect the client.");
		}
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, 1) != 1)
		{
			VhLogger.Instance.LogDebug("VpnService Host is destroying...");
			_connectCts.TryCancel();
			_connectCts.Dispose();
			VpnHoodClient client = Client;
			if (client != null)
			{
				client.StateChanged -= VpnHoodClient_StateChanged;
				client.Dispose();
			}
			_apiController.Dispose();
			VhLogger.Instance.LogDebug("VpnService has been destroyed.");
			_logService?.Dispose();
		}
	}
}
