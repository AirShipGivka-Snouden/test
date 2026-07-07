using System;
using System.Security.Cryptography;
using System.Text;

namespace Ciphra.VPN.Common.Services;

public static class OAuthPkce
{
	public sealed record PendingAuth(string Verifier, string Challenge, string State, DateTimeOffset CreatedAt);

	public static PendingAuth Create(TimeProvider? timeProvider = null)
	{
		string verifier = GenerateRandomBase64Url(32);
		string challenge = ComputeS256Challenge(verifier);
		string state = GenerateRandomBase64Url(32);
		return new PendingAuth(verifier, challenge, state, (timeProvider ?? TimeProvider.System).GetUtcNow());
	}

	private static string GenerateRandomBase64Url(int byteCount)
	{
		byte[] array = new byte[byteCount];
		RandomNumberGenerator.Fill(array);
		return Base64Url(array);
	}

	private static string ComputeS256Challenge(string verifier)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(verifier);
		byte[] data = SHA256.HashData(bytes);
		return Base64Url(data);
	}

	private static string Base64Url(byte[] data)
	{
		return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-')
			.Replace('/', '_');
	}
}
