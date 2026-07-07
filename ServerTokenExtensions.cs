using System;
using System.Text.Json;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Common.Tokens;

public static class ServerTokenExtensions
{
	public static bool IsTokenUpdated(this ServerToken serverToken, ServerToken newServerToken)
	{
		ServerToken serverToken2 = JsonUtils.JsonClone(serverToken);
		serverToken2.CreatedTime = DateTime.MinValue;
		ServerToken serverToken3 = JsonUtils.JsonClone(newServerToken);
		serverToken3.CreatedTime = DateTime.MinValue;
		if (JsonSerializer.Serialize(serverToken2) == JsonSerializer.Serialize(serverToken3))
		{
			return false;
		}
		return newServerToken.CreatedTime >= serverToken.CreatedTime;
	}
}
