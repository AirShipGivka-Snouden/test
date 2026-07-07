using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VpnHood.Core.Toolkit.Streams;

public static class StreamExtensions
{
	extension(Stream stream)
	{
		public async Task<string> ReadStringAtMostAsync(int maxByteCount, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			if (!stream.CanRead)
			{
				throw new InvalidOperationException("Stream must be readable.");
			}
			if (maxByteCount <= 0)
			{
				return string.Empty;
			}
			byte[] buffer = new byte[maxByteCount];
			int totalRead;
			int num;
			for (totalRead = 0; totalRead < maxByteCount; totalRead += num)
			{
				num = await stream.ReadAsync(buffer, totalRead, maxByteCount - totalRead, cancellationToken);
				if (num == 0)
				{
					break;
				}
			}
			return encoding.GetString(buffer, 0, totalRead);
		}
	}
}
