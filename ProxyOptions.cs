using System;

namespace VpnHood.Core.Proxies.EndPointManagement.Abstractions.Options;

public class ProxyOptions
{
	public ProxyEndPoint[] ProxyEndPoints { get; init; } = Array.Empty<ProxyEndPoint>();

	public bool ResetStates { get; init; }

	public ProxyAutoUpdateOptions AutoUpdateOptions { get; init; } = new ProxyAutoUpdateOptions();

	public bool VerifyTls { get; init; } = true;
}
