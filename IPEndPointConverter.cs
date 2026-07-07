using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VpnHood.Core.Toolkit.Converters;

public class IPEndPointConverter : JsonConverter<IPEndPoint>
{
	public override IPEndPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string text = reader.GetString();
		if (text == null || !IPEndPoint.TryParse(text, out IPEndPoint result))
		{
			throw new FormatException("An invalid IPEndPoint was specified. Value: " + text + ".");
		}
		return result;
	}

	public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}
}
