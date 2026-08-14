using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class DenuvoFixesResponse
{
	[JsonPropertyName("appid")]
	public string AppId { get; set; } = "";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("header_image")]
	public string? HeaderImage { get; set; }

	[JsonPropertyName("fixes")]
	public List<DenuvoFix> Fixes { get; set; } = new List<DenuvoFix>();
}
