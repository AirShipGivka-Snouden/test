using System;
using System.Net;
using VpnHood.Core.Common.Exceptions;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Common.Tokens;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Client.Exceptions;

public class RedirectHostException : SessionException
{
	public ServerToken[]? RedirectServerTokens => base.SessionResponse.RedirectServerTokens;

	[Obsolete("Deprecated on protocol 10. User RedirectServerTokens")]
	public IPEndPoint[] RedirectHostEndPoints
	{
		get
		{
			if (!VhUtils.IsNullOrEmpty(base.SessionResponse.RedirectHostEndPoints))
			{
				return base.SessionResponse.RedirectHostEndPoints;
			}
			if (base.SessionResponse.RedirectHostEndPoint == null)
			{
				return Array.Empty<IPEndPoint>();
			}
			return new IPEndPoint[1] { base.SessionResponse.RedirectHostEndPoint };
		}
	}

	public RedirectHostException(SessionResponse sessionResponse)
		: base(sessionResponse)
	{
	}
}
