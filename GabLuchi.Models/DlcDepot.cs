using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class DlcDepot
{
	[JsonPropertyName("depotId")]
	public string DepotId { get; set; } = "";

	[JsonPropertyName("language")]
	public string? Language { get; set; }

	[JsonPropertyName("oslist")]
	public string? OsList { get; set; }

	[JsonPropertyName("included")]
	public bool Included { get; set; }

	[JsonIgnore]
	public string Meta
	{
		get
		{
			List<string> list = new List<string> { Language ?? "default" };
			if (!string.IsNullOrEmpty(OsList))
			{
				list.Add(OsList);
			}
			return string.Join(" · ", list);
		}
	}
}
