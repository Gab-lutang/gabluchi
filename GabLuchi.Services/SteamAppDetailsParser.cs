using System.Text.Json;

namespace GabLuchi.Services;

internal static class SteamAppDetailsParser
{
	public static bool TryGetAppElement(JsonElement root, long appid, out JsonElement element)
	{
		element = default;
		if (root.ValueKind != JsonValueKind.Object)
		{
			return false;
		}
		string key = appid.ToString();
		JsonElement? exactKeyMatch = null;
		foreach (JsonProperty prop in root.EnumerateObject())
		{
			if (prop.Value.ValueKind != JsonValueKind.Object)
			{
				continue;
			}
			if (prop.Name == key)
			{
				exactKeyMatch = prop.Value;
			}
			if (prop.Value.TryGetProperty("data", out JsonElement data)
				&& data.ValueKind == JsonValueKind.Object
				&& data.TryGetProperty("steam_appid", out JsonElement steamAppId)
				&& steamAppId.TryGetInt64(out long dataAppId)
				&& dataAppId == appid)
			{
				element = prop.Value;
				return true;
			}
		}
		if (exactKeyMatch.HasValue)
		{
			element = exactKeyMatch.Value;
			return true;
		}
		return false;
	}
}