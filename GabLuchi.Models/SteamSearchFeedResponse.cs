using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class SteamSearchFeedResponse
{
	[JsonPropertyName("items")]
	public List<SteamSearchFeedItem> Items { get; set; } = new List<SteamSearchFeedItem>();
}