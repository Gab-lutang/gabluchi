using System.Collections.Generic;

namespace GabLuchi.Models;

public static class SourceMeta
{
	public record Meta(string? DisplayName = null, string? DiscordUrl = null, bool RequiresUserKey = false);

	public static readonly Dictionary<string, Meta> All = new Dictionary<string, Meta>
	{
		["Ryuu"] = new Meta(null, "https://discord.gg/manifests"),
		["TwentyTwo Cloud"] = new Meta(null, "https://discord.gg/RrukXPyv5b"),
		["Sushi"] = new Meta(null, "https://discord.gg/hMdv5dQhcN"),
		["Skyflare"] = new Meta(null, "https://discord.gg/luatools"),
		["Sadie (Morrenus)"] = new Meta("Sadie (Hubcap)", "https://discord.gg/hubcapsmanifest", RequiresUserKey: true),
		["Luie"] = new Meta("Luie (Mirror)", null)
	};

	public static Meta Get(string name)
	{
		if (!All.TryGetValue(name, out Meta value))
		{
			return new Meta();
		}
		return value;
	}
}
