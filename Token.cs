using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.Core.Common.Tokens;

public class Token
{
	[JsonPropertyName("v")]
	[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
	public int Version => 4;

	[JsonPropertyName("name")]
	[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
	public required string? Name { get; set; }

	[JsonPropertyName("sid")]
	[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
	public required string? SupportId { get; set; }

	[JsonPropertyName("tid")]
	[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
	public required string TokenId { get; set; }

	[JsonPropertyName("iat")]
	[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
	public DateTime IssuedAt { get; set; } = DateTime.MinValue;

	[JsonPropertyName("sec")]
	[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
	public required byte[] Secret { get; set; }

	[JsonPropertyName("ser")]
	public required ServerToken ServerToken { get; set; }

	[JsonPropertyName("tags")]
	public string[] Tags { get; set; } = Array.Empty<string>();

	[JsonPropertyName("ispub")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool IsPublic { get; set; }

	[JsonPropertyName("cpols")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public ClientPolicy[]? ClientPolicies { get; set; }

	public string ToAccessKey()
	{
		string s = JsonSerializer.Serialize(this, new JsonSerializerOptions
		{
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
		});
		return "vh://" + Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
	}

	public static Token FromAccessKey(string base64)
	{
		base64 = base64.Trim().Trim('"');
		string[] array = new string[4] { "vh://", "vhkey://", "vh:", "vhkey:" };
		foreach (string text in array)
		{
			if (base64.StartsWith(text))
			{
				base64 = base64.Substring(text.Length);
			}
		}
		string json = Encoding.UTF8.GetString(VhUtils.ConvertFromBase64AndFixPadding(base64));
		TokenVersion tokenVersion = JsonUtils.Deserialize<TokenVersion>(json);
		if (tokenVersion.Version == 4)
		{
			return JsonUtils.Deserialize<Token>(json);
		}
		throw new NotSupportedException($"Token version {tokenVersion.Version} is not supported!");
	}
}
