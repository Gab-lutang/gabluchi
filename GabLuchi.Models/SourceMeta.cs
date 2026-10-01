using System.Collections.Generic;

namespace GabLuchi.Models;

public static class SourceMeta
{
	public record Meta(string? DisplayName = null, bool RequiresUserKey = false);

	// DiscordUrl was removed deliberately. Every value that existed here pointed at a public invite
	// for the upstream project's own mirrors, so a customer clicking through landed on a free
	// alternative to the product they had just paid for. The names are kept because they are
	// genuinely useful when diagnosing which upstream answered, but the links are gone and the
	// property no longer exists, so nothing can rebind it by accident.
	//
	// Sadie (Hubcap) is the exception that never had a DiscordUrl: it needs the user's own API key
	// from hubcapmanifest.com, which SettingsViewModel.OpenHubcap opens directly.
	public static readonly Dictionary<string, Meta> All = new Dictionary<string, Meta>
	{
		["Ryuu"] = new Meta(null),
		["Sushi"] = new Meta(null),
		["Sadie (Morrenus)"] = new Meta("Sadie (Hubcap)", RequiresUserKey: true),
		["Luie"] = new Meta("Luie (Mirror)")
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
