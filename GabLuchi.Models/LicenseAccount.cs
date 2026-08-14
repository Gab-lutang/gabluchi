using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class LicenseAccount
{
	[JsonPropertyName("ok")]
	public bool Ok { get; set; }

	[JsonPropertyName("buyer")]
	public bool Buyer { get; set; }

	[JsonPropertyName("key")]
	public string? Key { get; set; }

	[JsonPropertyName("status")]
	public string? Status { get; set; }

	[JsonPropertyName("machineId")]
	public string? MachineId { get; set; }

	[JsonPropertyName("activatedAt")]
	public string? ActivatedAt { get; set; }

	[JsonPropertyName("issuedAt")]
	public string? IssuedAt { get; set; }

	[JsonPropertyName("error")]
	public string? Error { get; set; }
}
