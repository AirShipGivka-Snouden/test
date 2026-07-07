using System;

namespace VpnHood.Core.Toolkit.Utils;

public class Patch<T>(T value)
{
	public T Value { get; } = value;

	public override int GetHashCode()
	{
		T value = Value;
		if (value == null)
		{
			return "".GetHashCode();
		}
		return value.GetHashCode();
	}

	public override string ToString()
	{
		T value = Value;
		return ((value != null) ? value.ToString() : null) ?? "";
	}

	public override bool Equals(object? obj)
	{
		return object.Equals(Value, obj);
	}

	public static implicit operator Patch<T>(T value)
	{
		return new Patch<T>(value);
	}

	public static implicit operator T(Patch<T> value)
	{
		if (value == null)
		{
			throw new NullReferenceException("Value has not been set.");
		}
		return value.Value;
	}
}
