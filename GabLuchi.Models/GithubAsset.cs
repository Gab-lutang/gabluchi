using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public sealed class GithubAsset
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("browser_download_url")]
	public string DownloadUrl { get; set; } = "";

	[JsonPropertyName("digest")]
	public string? Digest { get; set; }
}
