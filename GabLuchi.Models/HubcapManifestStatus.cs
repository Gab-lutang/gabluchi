using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class HubcapManifestStatus
{
	[JsonPropertyName("status")]
	public string Status { get; set; } = "";

	[JsonPropertyName("manifest_file_exists")]
	public bool ManifestFileExists { get; set; }
}
