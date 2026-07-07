using System;

namespace VpnHood.Core.Tunneling.Utils;

public static class UniqueIdFactory
{
	public static int DebugInitId { get; set; } = 1000;

	public static string Create()
	{
		return Guid.NewGuid().ToString();
	}
}
