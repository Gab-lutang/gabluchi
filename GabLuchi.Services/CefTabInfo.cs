using System.Text.Json.Serialization;

namespace GabLuchi.Services;

internal class CefTabInfo
{
	[JsonPropertyName("title")]
	public string? Title { get; set; }

	[JsonPropertyName("url")]
	public string? Url { get; set; }

	[JsonPropertyName("webSocketDebuggerUrl")]
	public string? WebSocketDebuggerUrl { get; set; }

	[JsonPropertyName("id")]
	public string? Id { get; set; }
}
