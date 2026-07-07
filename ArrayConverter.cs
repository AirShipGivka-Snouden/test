using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VpnHood.Core.Toolkit.Converters;

public class ArrayConverter<T, TConverter> : JsonConverter<T[]> where TConverter : JsonConverter<T>, new()
{
	private readonly TConverter _typeConverter = new TConverter();

	public override T[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartArray)
		{
			throw new JsonException();
		}
		reader.Read();
		List<T> list = new List<T>();
		while (reader.TokenType != JsonTokenType.EndArray)
		{
			list.Add(_typeConverter.Read(ref reader, typeof(T), options));
			reader.Read();
		}
		return list.ToArray();
	}

	public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();
		foreach (T value2 in value)
		{
			_typeConverter.Write(writer, value2, options);
		}
		writer.WriteEndArray();
	}
}
