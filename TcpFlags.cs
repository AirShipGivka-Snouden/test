using System;

namespace VpnHood.Core.TcpStack.Primitives;

[Flags]
internal enum TcpFlags : byte
{
	None = 0,
	Fin = 1,
	Syn = 2,
	Rst = 4,
	Psh = 8,
	Ack = 0x10
}
