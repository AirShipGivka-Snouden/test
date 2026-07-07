using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class ServerKeysResponse : ApiResponse
{
	[JsonProperty("servers")]
	public List<VpnServerDto>? Servers { get; set; }
}
