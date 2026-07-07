using System;
using System.Collections.Generic;

namespace Ciphra.VPN.Common.Services.OnChain;

public static class OnChainConfig
{
	public const string CredentialsContract = "0x05a247D8345696cb194a9a4C4F3788b385B72E66";

	public const string EntryPointV07 = "0x0000000071727De22E5E9d8BAf0edAc6f37da032";

	public const string SimpleAccountFactory = "0x91E60e0613810449d098b0b5Ec8b51A0FE8c8985";

	public const string PaymasterAddress = "0xEc8e821e8337ca19CE289c976a04951Af532dA67";

	public const string SepoliaChainIdHex = "0xaa36a7";

	public const int SepoliaChainId = 11155111;

	public static readonly string[] Bundlers = new string[4] { "https://45.90.97.174/rpc", "https://45.90.97.178/rpc", "https://51.210.244.138/rpc", "https://api.pimlico.io/v2/sepolia/rpc?apikey=pim_5HJ8cUmdyAxSgtmYQQbUCK" };

	public static readonly string[] Paymasters = new string[4] { "https://45.90.97.174/paymaster", "https://45.90.97.178/paymaster", "https://51.210.244.138/paymaster", "https://api.pimlico.io/v2/sepolia/rpc?apikey=pim_5HJ8cUmdyAxSgtmYQQbUCK" };

	public static readonly HashSet<string> BundlerCertFingerprints = new HashSet<string> { "84E87EC1CEA0144E87C369FE0EC24122A48F65995DADE2033C7DAB3FC510D937", "B6A95828CD275718E51363B54064FD0CA29EB0F7C74858C730CE9B7A604ED6FE", "BD8E060173430584AF5BB4EF36C88E5EADB653E66E771DF2AEF07CA9B0B68864" };

	public static readonly string[] RpcEndpoints = new string[5] { "https://eth-sepolia.g.alchemy.com/v2/L3qfu6TF_gVPWJ5fXB9xC", "https://sepolia-rpc.ciphra.uk/public-rpc", "https://ethereum-sepolia-rpc.publicnode.com", "https://sepolia.drpc.org", "https://1rpc.io/sepolia" };

	public static readonly string[] IpfsGateways = new string[11]
	{
		"https://45.90.97.174", "https://45.90.97.178", "https://51.210.244.138", "https://cdn1.scrybee.com", "https://cdn2.scrybee.com", "https://ipfs.io", "https://dweb.link", "https://cloudflare-ipfs.com", "https://gateway.pinata.cloud", "https://w3s.link",
		"https://4everland.io"
	};

	public const string RegisterDeviceSelector = "0xd0675bfb";

	public const string UpdateDeviceKeySelector = "0x1a935c68";

	public const string GetIpnsNameSelector = "0x9d1606af";

	public const string GetDeviceSelector = "0xa33279c1";

	public const string IsRevokedSelector = "0xe28404b4";

	public const string GetAddressSelector = "0x8cb84e18";

	public const string CreateAccountSelector = "0x5fbfb9cf";

	public const string ExecuteSelector = "0xb61d27f6";

	public const string HkdfInfo = "ciphra-bundle-v1";

	public const string HkdfInfoIpns = "ciphra-ipns-v1";

	public static readonly byte[] DeviceIdEncryptionKey = Convert.FromHexString("a3f1b2c4d5e6f7081920a1b2c3d4e5f6");

	public static readonly TimeSpan BundlePollInterval = TimeSpan.FromSeconds(5L);

	public static readonly TimeSpan RpcTimeout = TimeSpan.FromSeconds(10L);

	public static readonly TimeSpan IpfsTimeout = TimeSpan.FromSeconds(15L);

	public const int MaxPollAttempts = 60;

	public const int MaxReceiptPollAttempts = 60;

	public static readonly TimeSpan ReceiptPollInterval = TimeSpan.FromSeconds(3L);
}
