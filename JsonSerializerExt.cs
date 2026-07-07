using System;
using System.Reflection;
using System.Text.Json;

namespace VpnHood.Core.Toolkit.Utils;

public static class JsonSerializerExt
{
	public static void PopulateObject<T>(T target, string jsonSource) where T : class
	{
		PopulateObject(target, jsonSource, typeof(T));
	}

	public static void PopulateObject(object target, string jsonSource, Type type)
	{
		foreach (JsonProperty item in JsonDocument.Parse(jsonSource).RootElement.EnumerateObject())
		{
			OverwriteProperty(target, item, type);
		}
	}

	private static void OverwriteProperty(object target, JsonProperty updatedProperty, Type type)
	{
		PropertyInfo property = type.GetProperty(updatedProperty.Name);
		if (property == null)
		{
			return;
		}
		Type propertyType = property.PropertyType;
		object obj;
		if (propertyType == typeof(Uri))
		{
			string text = updatedProperty.Value.GetString();
			obj = ((text != null) ? new Uri(text, UriKind.RelativeOrAbsolute) : null);
		}
		else if (propertyType.IsValueType || propertyType == typeof(string))
		{
			obj = JsonSerializer.Deserialize(updatedProperty.Value.GetRawText(), propertyType);
		}
		else
		{
			obj = property.GetValue(target);
			if (obj != null)
			{
				PopulateObject(obj, updatedProperty.Value.GetRawText(), propertyType);
			}
		}
		property.SetValue(target, obj);
	}
}
