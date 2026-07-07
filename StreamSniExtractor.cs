using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Filtering.DomainFiltering.SniExtractors.TlsStream;

public static class StreamSniExtractor
{
	public static async Task<StreamSniResult> ExtractSni(Stream tcpStream, EventId eventId, int streamHeaderBufferSize, CancellationToken cancellationToken)
	{
		Memory<byte> initBuffer = new Memory<byte>(new byte[streamHeaderBufferSize]);
		Memory<byte> readData = initBuffer.Slice(0, await tcpStream.ReadAsync(initBuffer, cancellationToken).Vhc());
		return new StreamSniResult
		{
			DomainName = TryExtractSni(readData.Span, eventId),
			ReadData = readData
		};
	}

	private static string? TryExtractSni(ReadOnlySpan<byte> payloadData, EventId eventId)
	{
		try
		{
			return TlsClientHelloParser.ExtractSni(payloadData);
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogDebug(eventId, exception, "Could not extract sni.");
			return null;
		}
	}
}
