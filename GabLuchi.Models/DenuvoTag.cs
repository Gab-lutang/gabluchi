using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class DenuvoTag
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = "";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("slug")]
	public string Slug { get; set; } = "";

	[JsonPropertyName("color")]
	public string? Color { get; set; }
}
