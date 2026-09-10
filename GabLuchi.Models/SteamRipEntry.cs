using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class SteamRipRoot
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("downloads")]
	public List<SteamRipEntry> Downloads { get; set; } = new List<SteamRipEntry>();
}

public class SteamRipEntry
{
	[JsonPropertyName("title")]
	public string Title { get; set; } = "";

	[JsonPropertyName("version")]
	public string Version { get; set; } = "";

	[JsonPropertyName("file_size")]
	public string FileSize { get; set; } = "";

	[JsonPropertyName("genre")]
	public string Genre { get; set; } = "";

	[JsonPropertyName("upload_date")]
	public string UploadDate { get; set; } = "";

	[JsonPropertyName("download_urls")]
	public List<string> DownloadUrls { get; set; } = new List<string>();
}
