using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ciphra.VPN.Common.Services.Interfaces;
using Newtonsoft.Json;
using Serilog;
using Windows.Storage;

namespace Ciphra.VPN.WinUI.Services;

[SupportedOSPlatform("windows")]
public class WinUiOAuthSecureStorage : IOAuthSecureStorage
{
	private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ciphra.vpn.oauth.v1");

	private const string FileName = "oauth.dat";

	public Task<OAuthStoredSession?> LoadAsync(CancellationToken ct = default(CancellationToken))
	{
		try
		{
			string path = ResolvePath();
			if (!File.Exists(path))
			{
				return Task.FromResult<OAuthStoredSession>(null);
			}
			byte[] encryptedData = File.ReadAllBytes(path);
			byte[] bytes = ProtectedData.Unprotect(encryptedData, Entropy, DataProtectionScope.CurrentUser);
			string text = Encoding.UTF8.GetString(bytes);
			OAuthStoredSession result = JsonConvert.DeserializeObject<OAuthStoredSession>(text);
			return Task.FromResult(result);
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to load OAuth secure storage; treating as empty");
			return Task.FromResult<OAuthStoredSession>(null);
		}
	}

	public Task SaveAsync(OAuthStoredSession session, CancellationToken ct = default(CancellationToken))
	{
		string s = JsonConvert.SerializeObject((object)session);
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		byte[] bytes2 = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
		string path = ResolvePath();
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		File.WriteAllBytes(path, bytes2);
		return Task.CompletedTask;
	}

	public Task ClearAsync(CancellationToken ct = default(CancellationToken))
	{
		try
		{
			string path = ResolvePath();
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to delete OAuth secure storage file");
		}
		return Task.CompletedTask;
	}

	private static string ResolvePath()
	{
		string path = ApplicationData.Current.LocalFolder.Path;
		return Path.Combine(path, "data", "oauth.dat");
	}
}
