using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VpnHood.Core.Toolkit.Converters;

public class TimeSpanConverter : JsonConverter<TimeSpan>
{
	public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.Number)
		{
			return TimeSpan.Parse(reader.GetString());
		}
		return TimeSpan.FromSeconds(reader.GetDouble());
	}

	public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}
}
