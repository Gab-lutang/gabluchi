using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class DenuvoFix
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = "";

	[JsonPropertyName("title")]
	public string Title { get; set; } = "";

	[JsonPropertyName("description")]
	public string? Description { get; set; }

	[JsonPropertyName("tags")]
	public List<DenuvoTag> Tags { get; set; } = new List<DenuvoTag>();

	[JsonPropertyName("hasManifest")]
	public bool HasManifest { get; set; }

	[JsonPropertyName("hasFix")]
	public bool HasFix { get; set; }

	[JsonPropertyName("manifestFilename")]
	public string? ManifestFilename { get; set; }

	[JsonPropertyName("fixFilename")]
	public string? FixFilename { get; set; }

	[JsonPropertyName("createdAt")]
	public string? CreatedAt { get; set; }
}
