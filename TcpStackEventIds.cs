using Microsoft.Extensions.Logging;

namespace VpnHood.Core.TcpStack.Abstractions;

public static class TcpStackEventIds
{
	public static readonly EventId TcpStackDiag = new EventId(10001, "TcpStackVerbose");
}
