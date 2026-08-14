using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class DenuvoGameListing
{
	[JsonPropertyName("appid")]
	public string AppId { get; set; } = "";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("header_image")]
	public string? HeaderImage { get; set; }

	[JsonPropertyName("fixCount")]
	public int FixCount { get; set; }

	[JsonPropertyName("tags")]
	public List<DenuvoTag> Tags { get; set; } = new List<DenuvoTag>();
}
