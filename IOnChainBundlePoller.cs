using System.Threading;
using System.Threading.Tasks;

namespace Ciphra.VPN.Common.Services.OnChain;

public interface IOnChainBundlePoller
{
	Task<OnChainRpcResult<bool>> TryIsRevokedAsync(byte[] encryptedDeviceId, CancellationToken ct);

	Task<OnChainRpcResult<byte[]>> TryGetDevicePubKeyAsync(byte[] encryptedDeviceId, CancellationToken ct);

	Task<OnChainRpcResult<string>> TryGetEncryptedIpnsNameAsync(byte[] encryptedDeviceId, CancellationToken ct);

	Task<byte[]?> FetchBundleAsync(string ipnsName, CancellationToken ct);

	Task<(byte[] EncryptedBundle, string IpnsName)> WaitForBundleAsync(byte[] encryptedDeviceId, byte[] x25519PrivateKey, CancellationToken ct);

	Task<bool> IsRevokedAsync(byte[] encryptedDeviceId, CancellationToken ct);

	Task<byte[]?> GetDevicePubKeyAsync(byte[] encryptedDeviceId, CancellationToken ct);

	Task<string?> GetIpnsNameAsync(byte[] encryptedDeviceId, CancellationToken ct);

	Task<string?> GetDecryptedIpnsNameAsync(byte[] encryptedDeviceId, byte[] x25519PrivateKey, CancellationToken ct);
}
