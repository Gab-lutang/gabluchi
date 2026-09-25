using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class SteamSearchFeedItem
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("logo")]
	public string? Logo { get; set; }
}