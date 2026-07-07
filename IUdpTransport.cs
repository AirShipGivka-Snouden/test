using System;
using System.Threading.Tasks;

namespace VpnHood.Core.Tunneling.Channels;

public interface IUdpTransport : IDisposable
{
	Action<Memory<byte>>? DataReceived { get; set; }

	int OverheadLength { get; }

	bool Connected { get; }

	Task SendAsync(ReadOnlyMemory<byte> buffer);
}
