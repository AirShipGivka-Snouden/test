using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ciphra.VPN.Common.Models;

public class PurchasePricesResponse : ApiResponse
{
	[JsonProperty("purchases")]
	public List<PurchaseDto>? Purchases { get; set; }
}
