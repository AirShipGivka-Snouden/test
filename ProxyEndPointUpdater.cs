using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VpnHood.Core.Proxies.EndPointManagement.Abstractions;

public static class ProxyEndPointUpdater
{
	public const int DefaultMaxItemCount = 1000;

	public const int DefaultMaxPenalty = 50;

	public static ProxyEndPoint[] Merge(IEnumerable<ProxyEndPointInfo> currentEndPointInfos, ProxyEndPoint[] newEndPoints, int? maxItemCount, int? maxPenalty, bool removeDuplicateIps = false)
	{
		int valueOrDefault = maxItemCount.GetValueOrDefault();
		if (!maxItemCount.HasValue)
		{
			valueOrDefault = 1000;
			maxItemCount = valueOrDefault;
		}
		valueOrDefault = maxPenalty.GetValueOrDefault();
		if (!maxPenalty.HasValue)
		{
			valueOrDefault = 50;
			maxPenalty = valueOrDefault;
		}
		if (maxItemCount <= 0)
		{
			throw new ArgumentException("MaxItemCount must be greater than 0", "maxItemCount");
		}
		List<ProxyEndPoint> list = new List<ProxyEndPoint>();
		HashSet<ProxyEndPoint> existingSet = new HashSet<ProxyEndPoint>();
		ProxyEndPointInfo[] array = currentEndPointInfos.ToArray();
		if (removeDuplicateIps)
		{
			newEndPoints = RemoveDuplicateIps(array, newEndPoints);
		}
		IEnumerable<ProxyEndPoint> newEndPoints2 = from info in array
			where info.Status.HasUsed && info.Status.Penalty <= maxPenalty && info.EndPoint.IsEnabled
			orderby info.Status.Penalty
			select info.EndPoint;
		AddNoDuplicate(list, newEndPoints2, existingSet);
		AddNoDuplicate(list, newEndPoints, existingSet);
		IEnumerable<ProxyEndPoint> newEndPoints3 = from info in array
			where !info.Status.HasUsed && info.EndPoint.IsEnabled
			orderby info.Status.Penalty
			select info.EndPoint;
		AddNoDuplicate(list, newEndPoints3, existingSet);
		IEnumerable<ProxyEndPoint> newEndPoints4 = from info in array
			where info.Status.HasUsed && info.Status.Penalty > maxPenalty && info.EndPoint.IsEnabled
			orderby info.Status.Penalty
			select info.EndPoint;
		AddNoDuplicate(list, newEndPoints4, existingSet);
		IEnumerable<ProxyEndPoint> newEndPoints5 = from info in array
			where !info.EndPoint.IsEnabled
			select info.EndPoint;
		AddNoDuplicate(list, newEndPoints5, existingSet);
		if (list.Count > maxItemCount)
		{
			list = list.Take(maxItemCount.Value).ToList();
		}
		return list.ToArray();
	}

	private static ProxyEndPoint[] RemoveDuplicateIps(ProxyEndPointInfo[] currentEndPointInfos, ProxyEndPoint[] newEndPoints)
	{
		IEnumerable<ProxyEndPoint> second = newEndPoints.Where((ProxyEndPoint x) => currentEndPointInfos.Any((ProxyEndPointInfo y) => y.EndPoint.Host.Equals(x.Host, StringComparison.OrdinalIgnoreCase) && y.EndPoint.Protocol == x.Protocol && y.Status.IsLastUsedSucceeded));
		return newEndPoints.Except(second).ToArray();
	}

	public static async Task<ProxyEndPoint[]> LoadFromUrlAsync(HttpClient httpClient, Uri url, CancellationToken cancellationToken = default(CancellationToken))
	{
		return ProxyEndPointParser.ExtractFromContent(await httpClient.GetStringAsync(url, cancellationToken)).Select(ProxyEndPointParser.FromUrl).ToArray();
	}

	private static void AddNoDuplicate(List<ProxyEndPoint> endPoints, IEnumerable<ProxyEndPoint> newEndPoints, HashSet<ProxyEndPoint> existingSet)
	{
		foreach (ProxyEndPoint newEndPoint in newEndPoints)
		{
			if (!existingSet.Contains(newEndPoint))
			{
				endPoints.Add(newEndPoint);
				existingSet.Add(newEndPoint);
			}
		}
	}
}
