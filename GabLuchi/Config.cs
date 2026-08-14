using System;
using System.IO;
using System.Text.Json;

namespace GabLuchi;

public static class Config
{
	private static readonly Lazy<JsonElement?> _root = new Lazy<JsonElement?>(Load);

	private static readonly Lazy<string> _baseDir = new Lazy<string>(() => AppContext.BaseDirectory);

	public static string BaseDirectory => _baseDir.Value;

	public static string ConfigPath => Path.Combine(BaseDirectory, "GabLuchi.config.json");

	private static JsonElement? Root => _root.Value;

	private static string Get(string key, string fallback)
	{
		if (_root.Value is JsonElement el && el.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.String)
		{
			string? s = value.GetString();
			if (!string.IsNullOrWhiteSpace(s))
			{
				return s.Trim();
			}
		}
		return fallback;
	}

	private static JsonElement? Load()
	{
		try
		{
			if (File.Exists(ConfigPath))
			{
				using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(ConfigPath));
				return doc.RootElement.Clone();
			}
		}
		catch
		{
		}
		return null;
	}

	public static string ManifestBackendBase => Get("ManifestBackendBase", "http://167.235.229.108");

	public static string ManifestBackendUserAgent => Get("ManifestBackendUserAgent", "secretgoonpoon");

	public static string ApiBaseUrl => Get("ApiBaseUrl", "https://lua.tools");

	public static string AuthBackendBase => Get("AuthBackendBase", "http://localhost:4567");

	public static string KeyCheckerBase => Get("KeyCheckerBase", "http://localhost:7890");

	public static string DiscordInviteUrl => Get("DiscordInviteUrl", "");

	public static string DiscordClientId => Get("DiscordClientId", "1535479561158918216");

	public static string FixRepositoryPath => Get("FixRepositoryPath", "");

	public static string BackendDir => Get("BackendDir", "");
}
