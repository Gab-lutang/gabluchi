using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class SteamStoreSearchResponse
{
	[JsonPropertyName("items")]
	public List<SteamStoreItem> Items { get; set; } = new List<SteamStoreItem>();
}
