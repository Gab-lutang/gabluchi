using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class ApiError
{
	[JsonPropertyName("error")]
	public string? Error { get; set; }
}
