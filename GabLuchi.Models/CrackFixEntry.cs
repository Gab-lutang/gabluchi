using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class CrackFixEntry
{
	[JsonPropertyName("buildid")]
	public string BuildId { get; set; } = "";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("source_crack")]
	public List<string> SourceCrack { get; set; } = new List<string>();

	[JsonPropertyName("original_download")]
	public List<string> OriginalDownload { get; set; } = new List<string>();

	[JsonPropertyName("fixes")]
	public List<CrackFixItem> Fixes { get; set; } = new List<CrackFixItem>();
}

public class CrackFixItem
{
	[JsonPropertyName("href")]
	public string Href { get; set; } = "";

	[JsonPropertyName("filename")]
	public string Filename { get; set; } = "";

	[JsonPropertyName("size")]
	public string Size { get; set; } = "";

	[JsonPropertyName("badges")]
	public List<string> Badges { get; set; } = new List<string>();
}
