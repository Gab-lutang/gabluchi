using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class DenuvoListingsResponse
{
	[JsonPropertyName("games")]
	public List<DenuvoGameListing> Games { get; set; } = new List<DenuvoGameListing>();

	[JsonPropertyName("tags")]
	public List<DenuvoTag> Tags { get; set; } = new List<DenuvoTag>();
}
