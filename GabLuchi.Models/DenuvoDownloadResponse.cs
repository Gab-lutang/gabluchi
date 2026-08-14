using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class DenuvoDownloadResponse
{
	[JsonPropertyName("url")]
	public string Url { get; set; } = "";
}
