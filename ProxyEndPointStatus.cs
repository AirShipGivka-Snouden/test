using System;
using System.Text.Json.Serialization;

namespace VpnHood.Core.Proxies.EndPointManagement.Abstractions;

public class ProxyEndPointStatus
{
	public int Penalty { get; set; }

	public int SucceededCount { get; set; }

	public int FailedCount { get; set; }

	public TimeSpan? Latency { get; set; }

	public DateTime? LastSucceeded { get; set; }

	public DateTime? LastFailed { get; set; }

	public string? ErrorMessage { get; set; }

	public long QueuePosition { get; set; }

	[JsonIgnore]
	public bool IsLastUsedSucceeded
	{
		get
		{
			if (LastSucceeded.HasValue)
			{
				if (LastFailed.HasValue)
				{
					return LastSucceeded > LastFailed;
				}
				return true;
			}
			return false;
		}
	}

	[JsonIgnore]
	public bool IsLastUsedFailed
	{
		get
		{
			if (LastFailed.HasValue)
			{
				if (LastSucceeded.HasValue)
				{
					return LastFailed > LastSucceeded;
				}
				return true;
			}
			return false;
		}
	}

	[JsonIgnore]
	public DateTime? LastUsed
	{
		get
		{
			if (!(LastSucceeded > LastFailed))
			{
				return LastFailed;
			}
			return LastSucceeded;
		}
	}

	[JsonIgnore]
	public bool HasUsed
	{
		get
		{
			if (SucceededCount <= 0)
			{
				return FailedCount > 0;
			}
			return true;
		}
	}

	public StatusQuality Quality
	{
		get
		{
			int penalty = Penalty;
			if (penalty <= 100)
			{
				if (penalty <= 10)
				{
					if (penalty <= 0)
					{
						if (SucceededCount == 0 && FailedCount == 0)
						{
							return StatusQuality.Unknown;
						}
						if (SucceededCount > 0)
						{
							return StatusQuality.Excellent;
						}
					}
					if (SucceededCount > 0)
					{
						return StatusQuality.Good;
					}
				}
				else if (penalty > 20)
				{
					goto IL_0063;
				}
				if (SucceededCount > 0)
				{
					return StatusQuality.Fair;
				}
				goto IL_0063;
			}
			if (penalty <= 10000)
			{
				goto IL_0070;
			}
			goto IL_007d;
			IL_0070:
			if (SucceededCount > 0)
			{
				return StatusQuality.VeryPoor;
			}
			goto IL_007d;
			IL_0063:
			if (SucceededCount > 0)
			{
				return StatusQuality.Poor;
			}
			goto IL_0070;
			IL_007d:
			return StatusQuality.Failed;
		}
	}
}
