using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class DlcInfo
{
	[JsonPropertyName("appid")]
	public string AppId { get; set; } = "";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("type")]
	public string Type { get; set; } = "";

	[JsonPropertyName("depotCount")]
	public int DepotCount { get; set; }

	[JsonPropertyName("haveCount")]
	public int HaveCount { get; set; }

	[JsonPropertyName("missingCount")]
	public int MissingCount { get; set; }

	[JsonPropertyName("depots")]
	public List<DlcDepot> Depots { get; set; } = new List<DlcDepot>();
}
