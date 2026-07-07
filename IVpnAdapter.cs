using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.PacketTransports;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.VpnAdapters.Abstractions;

public interface IVpnAdapter : IPacketTransport, IDisposable
{
	bool IsStarted { get; }

	bool IsNatSupported { get; }

	bool CanProtectSocket { get; }

	event EventHandler? Disposed;

	event EventHandler? PrimaryAdapterIpChanged;

	bool ProtectSocket(Socket socket);

	bool ProtectSocket(Socket socket, IPAddress ipAddress);

	Task Start(VpnAdapterOptions options, CancellationToken cancellationToken);

	void Stop();

	IPAddress? GetPrimaryAdapterAddress(IpVersion ipVersion);

	bool IsIpVersionSupported(IpVersion ipVersion);
}
