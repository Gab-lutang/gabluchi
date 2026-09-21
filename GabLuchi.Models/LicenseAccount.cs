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

	[JsonPropertyName("tier")]
	public string? Tier { get; set; }

	[JsonPropertyName("expiresAt")]
	public string? ExpiresAt { get; set; }

	[JsonPropertyName("keys")]
	public LicenseAccountKey[]? Keys { get; set; }

	[JsonPropertyName("error")]
	public string? Error { get; set; }
}

public class LicenseAccountKey
{
	[JsonPropertyName("key")]
	public string? Key { get; set; }

	[JsonPropertyName("status")]
	public string? Status { get; set; }

	[JsonPropertyName("tier")]
	public string? Tier { get; set; }

	[JsonPropertyName("expiresAt")]
	public string? ExpiresAt { get; set; }

	[JsonPropertyName("machineId")]
	public string? MachineId { get; set; }
}
