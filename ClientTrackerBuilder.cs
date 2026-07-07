using System;
using System.Collections.Generic;
using System.Net;
using Ga4.Trackers;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Client;

public static class ClientTrackerBuilder
{
	public static TrackEvent BuildConnectionSucceeded(string? serverLocation, bool isIpV6Supported, bool hasRedirected, IPEndPoint endPoint, string? adNetworkName)
	{
		return BuildConnectionAttempt(connected: true, serverLocation, isIpV6Supported, hasRedirected, endPoint, adNetworkName);
	}

	public static TrackEvent BuildConnectionFailed(string? serverLocation, bool isIpV6Supported, bool hasRedirected)
	{
		return BuildConnectionAttempt(connected: false, serverLocation, isIpV6Supported, hasRedirected, null, null);
	}

	private static TrackEvent BuildConnectionAttempt(bool connected, string? serverLocation, bool isIpV6Supported, bool hasRedirected, IPEndPoint? endPoint, string? adNetwork)
	{
		return new TrackEvent
		{
			EventName = "vh_connect_attempt",
			Parameters = new Dictionary<string, object>
			{
				{
					"server_location",
					serverLocation ?? string.Empty
				},
				{
					"connected",
					connected.ToString()
				},
				{
					"ipv6_supported",
					isIpV6Supported.ToString()
				},
				{
					"redirected",
					hasRedirected.ToString()
				},
				{
					"ad_network",
					adNetwork ?? string.Empty
				},
				{
					"endpoint",
					endPoint?.ToString() ?? string.Empty
				}
			}
		};
	}

	public static TrackEvent BuildEndPointStatus(VpnEndPoint vpnEndPoint, bool available)
	{
		return new TrackEvent
		{
			EventName = "vh_endpoint_status",
			Parameters = new Dictionary<string, object>
			{
				{ "ep", vpnEndPoint.TcpEndPoint },
				{ "domain", vpnEndPoint.HostName },
				{
					"ip_v6",
					vpnEndPoint.TcpEndPoint.Address.IsV6()
				},
				{ "available", available }
			}
		};
	}

	public static TrackEvent BuildUsage(Traffic traffic, int requestCount, int connectionCount)
	{
		return new TrackEvent
		{
			EventName = "vh_usage",
			Parameters = new Dictionary<string, object>
			{
				{
					"traffic_total",
					Math.Round((double)traffic.Total / 1000000.0)
				},
				{
					"traffic_sent",
					Math.Round((double)traffic.Sent / 1000000.0)
				},
				{
					"traffic_received",
					Math.Round((double)traffic.Received / 1000000.0)
				},
				{ "requests", requestCount },
				{ "connections", connectionCount }
			}
		};
	}
}
