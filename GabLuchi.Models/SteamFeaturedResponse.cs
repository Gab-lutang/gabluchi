using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class SteamFeaturedResponse
{
	[JsonPropertyName("top_sellers")]
	public SteamFeaturedCategory? TopSellers { get; set; }

	[JsonPropertyName("new_releases")]
	public SteamFeaturedCategory? NewReleases { get; set; }
}
