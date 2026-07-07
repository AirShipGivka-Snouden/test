using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace VpnHood.Core.Toolkit.Utils;

public static class JsonUtils
{
	public static bool JsonEquals(object? obj1, object? obj2)
	{
		if (obj1 == null && obj2 == null)
		{
			return true;
		}
		if (obj1 == null || obj2 == null)
		{
			return false;
		}
		return JsonSerializer.Serialize(obj1) == JsonSerializer.Serialize(obj2);
	}

	public static T JsonClone<T>(T obj, JsonSerializerOptions? options = null)
	{
		return Deserialize<T>(JsonSerializer.Serialize(obj, options), options);
	}

	public static T Deserialize<T>(string json, JsonSerializerOptions? options = null)
	{
		T val = JsonSerializer.Deserialize<T>(json, options);
		if (val == null)
		{
			throw new InvalidDataException($"{typeof(T)} could not be deserialized!");
		}
		return val;
	}

	public static T DeserializeFile<T>(string filePath, JsonSerializerOptions? options = null)
	{
		return Deserialize<T>(File.ReadAllText(filePath), options);
	}

	public static T? TryDeserializeFile<T>(string filePath, JsonSerializerOptions? options = null, ILogger? logger = null)
	{
		try
		{
			return DeserializeFile<T>(filePath, options);
		}
		catch (Exception exception)
		{
			logger?.LogError(exception, "Could not read json file. FilePath: {FilePath}", filePath);
			return default(T);
		}
	}

	public static string RedactValue(string json, string[] keys)
	{
		foreach (string text in keys)
		{
			int length = json.Length;
			string pattern = "\"key\"\\s*:\\s*\\[[^\\]]*\\]".Replace("key", text);
			json = Regex.Replace(json, pattern, "\"" + text + "\": [\"***\"]");
			if (length == json.Length)
			{
				pattern = "(?<=\"key\":)[^,|}|\r]+(?=,|}|\r)".Replace("key", text);
				json = Regex.Replace(json, pattern, " \"***\"");
			}
		}
		return json;
	}
}
