using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class HubcapStats
{
	[JsonPropertyName("user_id")]
	public string UserId { get; set; } = "";

	[JsonPropertyName("daily_usage")]
	public int DailyUsage { get; set; }

	[JsonPropertyName("daily_limit")]
	public int DailyLimit { get; set; }

	[JsonPropertyName("can_make_requests")]
	public bool CanMakeRequests { get; set; }

	[JsonPropertyName("api_key_expires_at")]
	public string? ApiKeyExpiresAt { get; set; }
}
