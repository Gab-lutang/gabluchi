using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class SteamStoreItem
{
	[JsonPropertyName("id")]
	public long Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("tiny_image")]
	public string? TinyImage { get; set; }
}
