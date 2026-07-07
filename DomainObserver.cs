using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Filtering.Abstractions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Filtering.DomainFiltering.Observation;

public class DomainObserver(EventId sniEventId)
{
	private readonly Dictionary<string, DomainObservation> _observations = new Dictionary<string, DomainObservation>(StringComparer.OrdinalIgnoreCase);

	private readonly Lock _lockObject = new Lock();

	public IReadOnlyList<DomainObservation> Observations
	{
		get
		{
			using (_lockObject.EnterScope())
			{
				return _observations.Values.ToList();
			}
		}
	}

	public void Track(string? domainName, FilterAction action, DomainObservationProtocol protocol, IpEndPointValue? destinationEndPoint = null)
	{
		if (string.IsNullOrEmpty(domainName))
		{
			return;
		}
		VhLogger.Instance.LogDebug(sniEventId, "Domain: {Domain}, DestEp: {DestEp}, Protocol: {protocol}", VhLogger.FormatHostName(domainName), VhLogger.Format(destinationEndPoint), protocol);
		using (_lockObject.EnterScope())
		{
			if (_observations.TryGetValue(domainName, out DomainObservation value))
			{
				value.Count++;
				value.LastObservedTime = FastDateTime.Now;
				value.Action = action;
				value.Protocol = protocol;
			}
			else
			{
				_observations[domainName] = new DomainObservation
				{
					DomainName = domainName,
					Action = action,
					Protocol = protocol,
					LastObservedTime = FastDateTime.Now
				};
			}
		}
	}

	public void Clear()
	{
		using (_lockObject.EnterScope())
		{
			_observations.Clear();
		}
	}
}
