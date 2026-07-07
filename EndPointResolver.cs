using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Client.Abstractions.Exceptions;
using VpnHood.Core.Common.Tokens;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Client.Abstractions;

public static class EndPointResolver
{
	private static async Task<IEnumerable<IPEndPoint>> TryGetEndPointFromDns(ServerToken serverToken, CancellationToken cancellationToken)
	{
		try
		{
			VhLogger.Instance.LogInformation("Resolving IP from host name: {HostName}...", VhLogger.FormatHostName(serverToken.HostName));
			return (await Dns.GetHostEntryAsync(serverToken.HostName, cancellationToken).Vhc()).AddressList.Select((IPAddress x) => new IPEndPoint(x, serverToken.HostPort));
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogError(exception, "Could not resolve IpAddress from hostname!");
			return Array.Empty<IPEndPoint>();
		}
	}

	public static async Task<IPEndPoint[]> ResolveHostEndPoints(ServerToken serverToken, EndPointStrategy strategy, CancellationToken cancellationToken)
	{
		if (strategy == EndPointStrategy.Auto)
		{
			strategy = serverToken.EndPointsStrategy;
		}
		if (!serverToken.IsValidHostName && VhUtils.IsNullOrEmpty(serverToken.HostEndPoints))
		{
			throw new InvalidOperationException("The token does not contain any server endpoints or a valid hostname. Please contact your provider’s support.");
		}
		if (strategy == EndPointStrategy.IpOnly)
		{
			if (!VhUtils.IsNullOrEmpty(serverToken.HostEndPoints))
			{
				return serverToken.HostEndPoints;
			}
			VhLogger.Instance.LogWarning("TokenOnly strategy is not supported by this token because there are no endpoints in the token. Fallback to DnsOnly.");
			strategy = EndPointStrategy.DnsOnly;
		}
		if (strategy == EndPointStrategy.DnsOnly && !serverToken.IsValidHostName)
		{
			VhLogger.Instance.LogWarning("DnsOnly strategy is not supported by this token because there are valid domain in the token. Fallback to auto.");
			strategy = EndPointStrategy.Auto;
		}
		IPEndPoint[] tokenEndPoints = serverToken.HostEndPoints ?? Array.Empty<IPEndPoint>();
		IEnumerable<IPEndPoint> enumerable = ((!serverToken.IsValidHostName) ? Array.Empty<IPEndPoint>() : (await TryGetEndPointFromDns(serverToken, cancellationToken).Vhc()));
		IEnumerable<IPEndPoint> enumerable2 = enumerable;
		IPEndPoint[] array = (strategy switch
		{
			EndPointStrategy.DnsFirst => enumerable2.Concat(tokenEndPoints), 
			EndPointStrategy.IpFirst => tokenEndPoints.Concat(enumerable2), 
			EndPointStrategy.DnsOnly => enumerable2, 
			_ => enumerable2.Concat(tokenEndPoints), 
		}).Distinct().ToArray();
		if (!array.Any())
		{
			throw new EndPointDiscoveryException("Could not resolve any host endpoint from AccessToken.");
		}
		return array;
	}
}
