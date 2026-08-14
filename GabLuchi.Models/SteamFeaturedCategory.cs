using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class SteamFeaturedCategory
{
	[JsonPropertyName("items")]
	public List<SteamFeaturedItem> Items { get; set; } = new List<SteamFeaturedItem>();
}
