using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public sealed class GithubRelease
{
	[JsonPropertyName("tag_name")]
	public string TagName { get; set; } = "";

	[JsonPropertyName("published_at")]
	public DateTimeOffset? PublishedAt { get; set; }

	[JsonPropertyName("assets")]
	public List<GithubAsset> Assets { get; set; } = new List<GithubAsset>();
}
