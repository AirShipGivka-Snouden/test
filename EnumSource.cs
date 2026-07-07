using System;

namespace Ciphra.VPN.Common.App;

public class EnumSource<T> where T : struct, Enum
{
	public required T Value { get; init; }

	public required string DisplayName { get; init; }
}
