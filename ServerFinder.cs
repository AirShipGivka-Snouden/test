using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Ga4.Trackers;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Client.Abstractions;
using VpnHood.Core.Client.Abstractions.Exceptions;
using VpnHood.Core.Client.ConnectorServices;
using VpnHood.Core.Common.Exceptions;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Common.Tokens;
using VpnHood.Core.Proxies.EndPointManagement;
using VpnHood.Core.Proxies.EndPointManagement.Abstractions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Monitoring;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Sockets;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling;
using VpnHood.Core.Tunneling.Messaging;
using VpnHood.Core.Tunneling.Utils;

namespace VpnHood.Core.Client;

public class ServerFinder(ISocketFactory socketFactory, string? serverLocation, TimeSpan serverQueryTimeout, EndPointStrategy endPointStrategy, IPEndPoint[] customServerEndpoints, ITracker? tracker, ProxyEndPointManager proxyEndPointManager, bool includeIpV6, int maxDegreeOfParallelism = 10)
{
	private class HostStatus
	{
		public required VpnEndPoint VpnEndPoint { get; init; }

		public bool? Available { get; set; }
	}

	private ProgressMonitor? _progressMonitor;

	private HostStatus[] _hostEndPointStatuses = Array.Empty<HostStatus>();

	public bool IncludeIpV6 { get; set; } = includeIpV6;

	public string? ServerLocation => serverLocation;

	public IPEndPoint[] CustomServerEndpoints => customServerEndpoints;

	public ProgressStatus? Progress => _progressMonitor?.Progress;

	public async Task<VpnEndPoint[]> ResolveVpnEndPoints(IEnumerable<ServerToken> serverTokens, bool includeIpV6, CancellationToken cancellationToken)
	{
		if (customServerEndpoints.Any())
		{
			VhLogger.Instance.LogWarning("There are forced endpoints in the configuration. EndPoints: {EndPoints}", string.Join(", ", customServerEndpoints.Select(VhLogger.Format)));
			return (from x in serverTokens.SelectMany((ServerToken serverToken) => customServerEndpoints.Select((IPEndPoint ep) => new VpnEndPoint(ep, serverToken.HostName, serverToken.CertificateHash, serverToken.PathBase)))
				where includeIpV6 || x.TcpEndPoint.IsV4() || x.TcpEndPoint.Address.IsLoopback()
				select x).ToArray();
		}
		List<(Exception exception, ServerToken serverToken)> itemExceptions = new List<(Exception, ServerToken)>();
		VpnEndPoint[] array = (from x in (await Task.WhenAll(serverTokens.Select(async delegate(ServerToken serverToken)
			{
				try
				{
					return (await EndPointResolver.ResolveHostEndPoints(serverToken, endPointStrategy, cancellationToken)).Select((IPEndPoint ep) => new VpnEndPoint(ep, serverToken.HostName, serverToken.CertificateHash, serverToken.PathBase));
				}
				catch (Exception item)
				{
					itemExceptions.Add((item, serverToken));
					return Array.Empty<VpnEndPoint>();
				}
			}))).SelectMany((IEnumerable<VpnEndPoint> x) => x)
			where includeIpV6 || x.TcpEndPoint.IsV4() || x.TcpEndPoint.Address.IsLoopback()
			select x).ToArray();
		if (!array.Any() && itemExceptions.Any())
		{
			throw itemExceptions.First().exception;
		}
		foreach (var item2 in itemExceptions)
		{
			VhLogger.Instance.LogWarning(item2.exception, "Failed to resolve endpoints for server token. HostName: {HostName}, HostPort: {HostPort}", item2.serverToken.HostName, item2.serverToken.HostPort);
		}
		return array;
	}

	public async Task<VpnEndPoint> FindReachableServerAsync(IEnumerable<ServerToken> serverTokens, CancellationToken cancellationToken)
	{
		VhLogger.Instance.LogInformation(GeneralEventId.Request, "Finding a reachable server... QueryTimeout: {QueryTimeout}, ServerLocation: {ServerLocation}", serverQueryTimeout, ServerLocation);
		HostStatus[] hostStatuses = (await ResolveVpnEndPoints(serverTokens, IncludeIpV6, cancellationToken)).Select((VpnEndPoint x) => new HostStatus
		{
			VpnEndPoint = x
		}).Shuffle().ToArray();
		_hostEndPointStatuses = await VerifyHostsStatus(hostStatuses, byOrder: false, cancellationToken);
		VpnEndPoint vpnEndPoint = _hostEndPointStatuses.FirstOrDefault((HostStatus x) => x.Available == true)?.VpnEndPoint;
		VhLogger.Instance.LogInformation(GeneralEventId.Request, "ServerFinder result. Reachable: {Reachable}, Unreachable: {Unreachable}, Unknown: {Unknown}", _hostEndPointStatuses.Count((HostStatus x) => x.Available == true), _hostEndPointStatuses.Count((HostStatus x) => x.Available == false), _hostEndPointStatuses.Count((HostStatus x) => !x.Available.HasValue));
		TryTrackEndPointsAvailability(Array.Empty<HostStatus>(), _hostEndPointStatuses).Vhc();
		if (vpnEndPoint != null)
		{
			return vpnEndPoint;
		}
		tracker?.TryTrack(ClientTrackerBuilder.BuildConnectionFailed(ServerLocation, IncludeIpV6, hasRedirected: false));
		ProxyEndPointManager proxyEndPointManager2 = proxyEndPointManager;
		if (proxyEndPointManager2 != null && proxyEndPointManager2.IsEnabled)
		{
			ProxyEndPointManagerStatus status = proxyEndPointManager2.Status;
			if (status != null && !status.IsAnySucceeded)
			{
				throw new UnreachableProxyServerException();
			}
		}
		throw new UnreachableServerException();
	}

	public async Task<VpnEndPoint> FindBestRedirectedServerAsync(ServerToken[] serverTokens, CancellationToken cancellationToken)
	{
		VhLogger.Instance.LogInformation(GeneralEventId.Request, "Finding best server from redirected endpoints... ServerLocation: {ServerLocation}", ServerLocation);
		if (!serverTokens.Any())
		{
			throw new Exception("There is no server endpoint. Please check server configuration.");
		}
		HostStatus[] array = (await ResolveVpnEndPoints(serverTokens, IncludeIpV6, cancellationToken)).Select((VpnEndPoint x) => new HostStatus
		{
			VpnEndPoint = x
		}).ToArray();
		HostStatus[] array2 = array;
		foreach (HostStatus hostStatus in array2)
		{
			hostStatus.Available = _hostEndPointStatuses.FirstOrDefault((HostStatus x) => x.VpnEndPoint.Equals(hostStatus.VpnEndPoint))?.Available;
		}
		HostStatus[] array3 = await VerifyHostsStatus(array, byOrder: true, cancellationToken);
		VpnEndPoint vpnEndPoint = array3.FirstOrDefault((HostStatus x) => x.Available == true)?.VpnEndPoint;
		VhLogger.Instance.LogInformation(GeneralEventId.Session, "ServerFinder result. Reachable:{Reachable}, Unreachable:{Unreachable}, Unknown: {Unknown}, Best: {Best}", array3.Count((HostStatus x) => x.Available == true), array3.Count((HostStatus x) => x.Available == false), array3.Count((HostStatus x) => !x.Available.HasValue), VhLogger.Format(vpnEndPoint?.TcpEndPoint));
		TryTrackEndPointsAvailability(_hostEndPointStatuses, array3).Vhc();
		if (vpnEndPoint != null)
		{
			return vpnEndPoint;
		}
		tracker?.TryTrack(ClientTrackerBuilder.BuildConnectionFailed(ServerLocation, IncludeIpV6, hasRedirected: true));
		ProxyEndPointManager proxyEndPointManager2 = proxyEndPointManager;
		if (proxyEndPointManager2 != null && proxyEndPointManager2.IsEnabled)
		{
			ProxyEndPointManagerStatus status = proxyEndPointManager2.Status;
			if (status != null && !status.IsAnySucceeded)
			{
				throw new UnreachableProxyServerException();
			}
		}
		throw UnreachableServerLocationException.Create(ServerLocation);
	}

	private Task TryTrackEndPointsAvailability(HostStatus[] oldStatuses, HostStatus[] newStatuses)
	{
		HostStatus[] source = newStatuses.Where((HostStatus x) => x.Available.HasValue && !oldStatuses.Any((HostStatus y) => y.Available == x.Available && y.VpnEndPoint.Equals(x.VpnEndPoint))).ToArray();
		TrackEvent[] trackEvents = (from x in source
			where x.Available.HasValue
			select ClientTrackerBuilder.BuildEndPointStatus(x.VpnEndPoint, x.Available.Value)).ToArray();
		string text = string.Join(", ", source.Select((HostStatus x) => $"{VhLogger.Format(x.VpnEndPoint.TcpEndPoint)} => {x.Available}"));
		VhLogger.Instance.LogInformation(GeneralEventId.Request, "HostEndPoints: {EndPoints}", text);
		return tracker?.TryTrack(trackEvents) ?? Task.CompletedTask;
	}

	private async Task<HostStatus[]> VerifyHostsStatus(HostStatus[] hostStatuses, bool byOrder, CancellationToken cancellationToken)
	{
		HostStatus firstHostStatus = hostStatuses.FirstOrDefault();
		if (firstHostStatus == null)
		{
			return Array.Empty<HostStatus>();
		}
		_progressMonitor = new ProgressMonitor(hostStatuses.Length + maxDegreeOfParallelism, serverQueryTimeout, maxDegreeOfParallelism);
		try
		{
			VhLogger.Instance.LogInformation(GeneralEventId.Request, "Starting endpoint check for the first endpoint... EndPoint: {EndPoint}", VhLogger.Format(firstHostStatus.VpnEndPoint.TcpEndPoint));
			await VerifyHostStatus(firstHostStatus, serverQueryTimeout, cancellationToken);
			if (firstHostStatus.Available == true)
			{
				return new HostStatus[1] { firstHostStatus };
			}
			for (int i = 0; i < maxDegreeOfParallelism; i++)
			{
				_progressMonitor.IncrementCompleted();
			}
			return await VerifyHostStatusParallel(hostStatuses, byOrder, _progressMonitor, cancellationToken);
		}
		finally
		{
			VhLogger.Instance.LogInformation(GeneralEventId.Request, "Endpoint reachability check completed. ElapsedTime: {ElapsedTime}, CompletedEndpoints: {CompletedEndpoints}/{TotalEndpoints}", FastDateTime.Now - _progressMonitor.Progress.StartedTime, hostStatuses.Count((HostStatus x) => x.Available.HasValue), hostStatuses.Length);
			_progressMonitor = null;
		}
	}

	private async Task<HostStatus[]> VerifyHostStatusParallel(HostStatus[] hostStatuses, bool byOrder, ProgressMonitor progressMonitor, CancellationToken cancellationToken)
	{
		VhLogger.Instance.LogInformation(GeneralEventId.Request, "Starting endpoints reachability. EndpointCount: {EndpointCount}, MaxParallelism: {MaxParallelism}", hostStatuses.Length, maxDegreeOfParallelism);
		CancellationTokenSource searchingCts = new CancellationTokenSource();
		try
		{
			CancellationTokenSource parallelCts = CancellationTokenSource.CreateLinkedTokenSource(searchingCts.Token, cancellationToken);
			try
			{
				bool oldUseRecentSucceeded = proxyEndPointManager.UseRecentSucceeded;
				try
				{
					proxyEndPointManager.UseRecentSucceeded = true;
					SemaphoreSlim semaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
					try
					{
						Task<HostStatus>[] tasks = hostStatuses.Select(async delegate(HostStatus hostStatus)
						{
							await semaphore.WaitAsync(parallelCts.Token);
							try
							{
								await VerifyHostStatus(hostStatus, serverQueryTimeout, parallelCts.Token).Vhc();
								if (hostStatus.Available == true && !byOrder)
								{
									await searchingCts.CancelAsync().Vhc();
								}
								if (byOrder)
								{
									HostStatus[] array = hostStatuses;
									foreach (HostStatus hostStatus2 in array)
									{
										if (!hostStatus2.Available.HasValue)
										{
											break;
										}
										if (hostStatus2.Available.Value)
										{
											await searchingCts.CancelAsync().Vhc();
											break;
										}
									}
								}
								return hostStatus;
							}
							catch (OperationCanceledException) when (searchingCts.IsCancellationRequested)
							{
								return hostStatus;
							}
							finally
							{
								progressMonitor.IncrementCompleted();
								semaphore.Release();
							}
						}).ToArray();
						try
						{
							await Task.WhenAll(tasks);
						}
						catch (OperationCanceledException) when (searchingCts.IsCancellationRequested)
						{
						}
					}
					finally
					{
						if (semaphore != null)
						{
							((IDisposable)semaphore).Dispose();
						}
					}
				}
				catch (OperationCanceledException) when (searchingCts.IsCancellationRequested)
				{
				}
				finally
				{
					proxyEndPointManager.UseRecentSucceeded = oldUseRecentSucceeded;
				}
				return hostStatuses;
			}
			finally
			{
				if (parallelCts != null)
				{
					((IDisposable)parallelCts).Dispose();
				}
			}
		}
		finally
		{
			if (searchingCts != null)
			{
				((IDisposable)searchingCts).Dispose();
			}
		}
	}

	private async Task VerifyHostStatus(HostStatus hostStatus, TimeSpan queryTimeout, CancellationToken cancellationToken)
	{
		if (hostStatus.Available.HasValue)
		{
			return;
		}
		using RequestSender requestSender = CreateRequestSender(hostStatus.VpnEndPoint);
		hostStatus.Available = await VerifyServerStatus(requestSender, queryTimeout, cancellationToken).Vhc();
	}

	private static async Task<bool> VerifyServerStatus(RequestSender requestSender, TimeSpan queryTimeout, CancellationToken cancellationToken)
	{
		try
		{
			VhLogger.Instance.LogInformation(GeneralEventId.Request, "Check an endpoint reachability. EndPoint: {EndPoint}", VhLogger.Format(requestSender.ConnectorService.VpnEndPoint.TcpEndPoint));
			using CancellationTokenSource queryTimeoutCts = new CancellationTokenSource(queryTimeout);
			using CancellationTokenSource requestCts = CancellationTokenSource.CreateLinkedTokenSource(queryTimeoutCts.Token, cancellationToken);
			ConnectorRequestResult<SessionResponse> connectorRequestResult = await requestSender.SendRequest<SessionResponse>(new ServerCheckRequest
			{
				RequestId = UniqueIdFactory.Create()
			}, requestCts.Token).Vhc();
			if (connectorRequestResult.Response.ErrorCode != SessionErrorCode.Ok)
			{
				throw new SessionException(connectorRequestResult.Response.ErrorCode);
			}
			return true;
		}
		catch (UnauthorizedAccessException)
		{
			return true;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogInformation(exception, "Could not get server status. EndPoint: {EndPoint}", VhLogger.Format(requestSender.ConnectorService.VpnEndPoint.TcpEndPoint));
			return false;
		}
	}

	private RequestSender CreateRequestSender(VpnEndPoint vpnEndPoint)
	{
		RequestSender requestSender = new RequestSender(new ConnectorService(new ConnectorServiceOptions
		{
			VpnEndPoint = vpnEndPoint,
			ProxyEndPointManager = proxyEndPointManager,
			SocketFactory = socketFactory,
			RequestTimeout = serverQueryTimeout,
			AllowChannelReuse = false
		}));
		requestSender.ConnectorService.Init(requestSender.ConnectorService.ProtocolVersion, null, TimeSpan.Zero, useWebSocket: false, serverQueryTimeout, useQuic: false, null);
		return requestSender;
	}
}
