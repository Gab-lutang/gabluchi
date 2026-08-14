using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class SteamFeaturedItem
{
	[JsonPropertyName("id")]
	public long Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("large_capsule_image")]
	public string? LargeCapsuleImage { get; set; }

	[JsonPropertyName("type")]
	public int Type { get; set; }
}
