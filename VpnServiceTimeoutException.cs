using System;

namespace VpnHood.Core.Client.VpnServices.Manager.Exceptions;

public class VpnServiceTimeoutException : TimeoutException
{
	public required TimeSpan TimeoutDuration
	{
		get
		{
			object obj = Data["TimeoutDuration"];
			if (obj != null)
			{
				return TimeSpan.FromSeconds((int)obj);
			}
			return TimeSpan.Zero;
		}
		init
		{
			Data["TimeoutDuration"] = (int)value.TotalSeconds;
		}
	}

	public VpnServiceTimeoutException(string message)
		: base(message)
	{
	}

	public VpnServiceTimeoutException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
