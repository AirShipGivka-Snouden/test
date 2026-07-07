using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Client.Abstractions;
using VpnHood.Core.Client.VpnServices.Abstractions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Client.VpnServices.Host;

internal class VpnServiceContext(string configFolder)
{
	private readonly AsyncLock _connectionInfoLock = new AsyncLock();

	public string ConfigFilePath => Path.Combine(configFolder, "vpn.config");

	public string StatusFilePath => Path.Combine(configFolder, "vpn.status");

	public string LogFilePath => Path.Combine(configFolder, "vpn.log");

	public string ConfigFolder => configFolder;

	public static ConnectionInfo DefaultConnectionInfo { get; } = new ConnectionInfo
	{
		ApiEndPoint = null,
		ApiKey = null,
		ProxyManagerStatus = null,
		ClientState = ClientState.Initializing,
		ClientStateProgress = null,
		ClientStateChangedTime = null,
		CreatedTime = FastDateTime.Now,
		Error = null,
		SessionInfo = null,
		SessionName = null,
		SessionStatus = null
	};

	public ConnectionInfo ConnectionInfo { get; private set; } = DefaultConnectionInfo;

	public ClientOptions? TryReadClientOptions()
	{
		try
		{
			return ReadClientOptions();
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogError(exception, "Could not read client options from file.");
			return null;
		}
	}

	public ClientOptions ReadClientOptions()
	{
		return JsonUtils.Deserialize<ClientOptions>(File.ReadAllText(ConfigFilePath));
	}

	public async Task<bool> TryWriteConnectionInfo(ConnectionInfo connectionInfo, CancellationToken cancellationToken)
	{
		_ = 1;
		try
		{
			using (await _connectionInfoLock.LockAsync(cancellationToken))
			{
				ConnectionInfo = connectionInfo;
				string content = JsonSerializer.Serialize(connectionInfo);
				await FileUtils.WriteAllTextRetryAsync(StatusFilePath, content, TimeSpan.FromSeconds(2L), cancellationToken);
				return true;
			}
		}
		catch (OperationCanceledException)
		{
			return false;
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogError(exception, "Could not save connection info to file. FilePath: {FilePath}", StatusFilePath);
			return false;
		}
	}
}
