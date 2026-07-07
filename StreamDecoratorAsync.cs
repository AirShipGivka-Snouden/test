using System.IO;

namespace VpnHood.Core.Toolkit.Streams;

public class StreamDecoratorAsync : AsyncStreamDecorator<Stream>
{
	public StreamDecoratorAsync(Stream sourceStream, bool leaveOpen)
		: base(sourceStream, leaveOpen)
	{
	}
}
