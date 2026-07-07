using System.Text.Json.Serialization;

namespace Ciphra.VPN.Common.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SplitAppMode
{
	All,
	Exclude,
	Include
}
