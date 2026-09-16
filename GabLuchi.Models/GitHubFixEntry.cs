using System;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class GitHubFixEntry
{
	[JsonPropertyName("appId")]
	public long AppId { get; set; }

	[JsonPropertyName("gameName")]
	public string GameName { get; set; } = "";

	[JsonPropertyName("password")]
	public string Password { get; set; } = "online-fix.me";

	[JsonPropertyName("fileName")]
	public string FileName { get; set; } = "";

	[JsonPropertyName("dateAdded")]
	public string DateAdded { get; set; } = "";

	public string DownloadUrl =>
		"https://raw.githubusercontent.com/Gab-lutang/gabluchi-fixes/main/fixes/" + Uri.EscapeDataString(FileName);

	public string Source => "gabluchi-fixes";
}
