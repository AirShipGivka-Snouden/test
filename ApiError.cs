using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using VpnHood.Core.Toolkit.Exceptions;

namespace VpnHood.Core.Toolkit.ApiClients;

public class ApiError : ICloneable, IEquatable<ApiError>
{
	public const string Flag = "IsApiError";

	public required string TypeName { get; init; }

	public string? TypeFullName { get; init; }

	public required string Message { get; init; }

	public Dictionary<string, string?> Data { get; init; } = new Dictionary<string, string>();

	public string? InnerMessage { get; init; }

	public object Clone()
	{
		return new ApiError
		{
			TypeName = TypeName,
			TypeFullName = TypeFullName,
			Message = Message,
			Data = new Dictionary<string, string>(Data),
			InnerMessage = InnerMessage
		};
	}

	public static bool TryParse(string value, [NotNullWhen(true)] out ApiError? apiError)
	{
		apiError = null;
		try
		{
			ApiError apiError2 = JsonSerializer.Deserialize<ApiError>(value);
			if (apiError2 == null || apiError2.TypeName == null)
			{
				return false;
			}
			apiError = apiError2;
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static ApiError Parse(string value)
	{
		if (TryParse(value, out ApiError apiError))
		{
			return apiError;
		}
		throw new FormatException("Invalid ApiError format.");
	}

	public string ToJson(bool writeIndented = false)
	{
		return JsonSerializer.Serialize(this, new JsonSerializerOptions
		{
			WriteIndented = writeIndented
		});
	}

	public bool Is<T>()
	{
		return TypeName == typeof(T).Name;
	}

	public Exception ToException()
	{
		Exception ex = new Exception(InnerMessage ?? "");
		Exception ex2 = (Is<OperationCanceledException>() ? new OperationCanceledException(Message, ex) : (Is<TaskCanceledException>() ? new TaskCanceledException(Message, ex) : (Is<AlreadyExistsException>() ? new AlreadyExistsException(Message, ex) : (Is<NotExistsException>() ? new NotExistsException(Message, ex) : (Is<UnauthorizedAccessException>() ? new UnauthorizedAccessException(Message, ex) : (Is<TimeoutException>() ? new TimeoutException(Message, ex) : (Is<InvalidOperationException>() ? ((Exception)new InvalidOperationException(Message, ex)) : ((Exception)new ApiException(this)))))))));
		ExportData(ex2.Data);
		return ex2;
	}

	public void ImportData(IDictionary data)
	{
		foreach (DictionaryEntry datum in data)
		{
			string text = datum.Key.ToString();
			if (text != null)
			{
				Data.TryAdd(text, datum.Value?.ToString());
			}
		}
		Data.TryAdd("IsApiError", "true");
	}

	public void ExportData(IDictionary data)
	{
		foreach (KeyValuePair<string, string> item in Data.Where<KeyValuePair<string, string>>((KeyValuePair<string, string> kvp) => !data.Contains(kvp.Key)))
		{
			data.Add(item.Key, item.Value);
		}
		if (!data.Contains("TypeFullName"))
		{
			data.Add("TypeFullName", TypeFullName);
		}
		if (!data.Contains("TypeName"))
		{
			data.Add("TypeName", TypeName);
		}
		if (!data.Contains("IsApiError"))
		{
			data.Add("IsApiError", "true");
		}
	}

	public bool Equals(ApiError? other)
	{
		if (other == null)
		{
			return false;
		}
		if (this == other)
		{
			return true;
		}
		if (TypeName == other.TypeName && TypeFullName == other.TypeFullName && Message == other.Message && InnerMessage == other.InnerMessage)
		{
			return DictionariesEqual(Data, other.Data);
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((ApiError)obj);
	}

	private static bool DictionariesEqual(Dictionary<string, string?> dict1, Dictionary<string, string?> dict2)
	{
		if (dict1.Count != dict2.Count)
		{
			return false;
		}
		foreach (KeyValuePair<string, string> item in dict1)
		{
			if (!dict2.TryGetValue(item.Key, out string value) || !string.Equals(item.Value, value))
			{
				return false;
			}
		}
		return true;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(TypeName, TypeFullName, Message, InnerMessage);
	}

	public override string ToString()
	{
		return $"ApiError: TypeName={TypeName}, TypeFullName={TypeFullName}, Message={Message}, InnerMessage={InnerMessage}, Data=[{string.Join(", ", Data.Select<KeyValuePair<string, string>, string>((KeyValuePair<string, string> kvp) => kvp.Key + "=" + kvp.Value))}]";
	}
}
