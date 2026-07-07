using System;
using VpnHood.Core.Common.Messaging;

namespace VpnHood.Core.Tunneling.Messaging;

public abstract class RequestBase : ClientRequest
{
	public required ulong SessionId { get; set; }

	public required ReadOnlyMemory<byte> SessionKey { get; set; }

	protected RequestBase(RequestCode requestCode)
		: base((byte)requestCode)
	{
	}
}
