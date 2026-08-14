using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class GameDetails
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("appid")]
	public long AppId { get; set; }

	[JsonPropertyName("type")]
	public string Type { get; set; } = "";

	[JsonPropertyName("baseAppId")]
	public string? BaseAppId { get; set; }

	[JsonPropertyName("genres")]
	public List<string> Genres { get; set; } = new List<string>();

	[JsonPropertyName("headerImage")]
	public string? HeaderImage { get; set; }

	[JsonPropertyName("releaseDate")]
	public string? ReleaseDate { get; set; }

	[JsonIgnore]
	public bool IsDlc => string.Equals(Type, "dlc", StringComparison.OrdinalIgnoreCase);
}
