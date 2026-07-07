namespace Ciphra.VPN.Common.Storage;

internal static class StorageKeys
{
	private static readonly byte[] _a = new byte[32]
	{
		79, 163, 23, 226, 139, 93, 201, 54, 113, 14,
		244, 152, 42, 213, 99, 188, 31, 132, 78, 167,
		57, 192, 91, 246, 34, 157, 104, 177, 12, 227,
		117, 170
	};

	private static readonly byte[] _b = new byte[32]
	{
		124, 209, 37, 144, 184, 111, 251, 4, 67, 60,
		198, 170, 24, 231, 81, 142, 45, 182, 124, 149,
		11, 242, 105, 196, 16, 175, 90, 131, 62, 209,
		71, 152
	};

	private static readonly byte[] _key = ComputeKey();

	private static byte[] ComputeKey()
	{
		byte[] array = new byte[32];
		for (int i = 0; i < 32; i++)
		{
			array[i] = (byte)(_a[i] ^ _b[i]);
		}
		return array;
	}

	internal static byte[] GetKey()
	{
		return _key;
	}
}
