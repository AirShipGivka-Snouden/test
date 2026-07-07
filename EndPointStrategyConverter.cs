using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VpnHood.Core.Common.Tokens;

public sealed class EndPointStrategyConverter : JsonConverter<EndPointStrategy>
{
	public override EndPointStrategy Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
		case JsonTokenType.Number:
			return (EndPointStrategy)reader.GetInt32();
		case JsonTokenType.String:
		{
			string text = reader.GetString();
			if (Enum.TryParse<EndPointStrategy>(text, ignoreCase: true, out var result))
			{
				return result;
			}
			throw new JsonException("Invalid EndPointStrategy value: " + text);
		}
		default:
			throw new JsonException($"Unexpected token {reader.TokenType}");
		}
	}

	public override void Write(Utf8JsonWriter writer, EndPointStrategy value, JsonSerializerOptions options)
	{
		writer.WriteNumberValue((int)value);
	}
}
