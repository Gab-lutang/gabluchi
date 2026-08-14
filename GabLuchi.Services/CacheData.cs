using System.Collections.Generic;

namespace GabLuchi.Services;

public class CacheData
{
	public string? GabLuchiInstalledVersion { get; set; }

	public string? GabLuchiInstalledZipDigest { get; set; }

	public List<long> SteamApiRequestTimes { get; set; } = new List<long>();

	public List<string> DonatedAppIds { get; set; } = new List<string>();

	public List<long> HardwareAppIds { get; set; } = new List<long>();

	public long HardwareAppIdsFetchedAtMs { get; set; }

	public List<long> LoadedAppIds { get; set; } = new List<long>();

	public bool OnboardingComplete { get; set; }
}
