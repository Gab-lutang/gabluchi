namespace GabLuchi;

public static class AppConfig
{
	public const string HubcapBaseUrl = "https://hubcapmanifest.com";

	public const string SteamStoreSearchUrl = "https://store.steampowered.com/api/storesearch/";

	public const string SteamFeaturedUrl = "https://store.steampowered.com/api/featuredcategories";

	public const string HardwareAppIdListUrl = "https://raw.githubusercontent.com/jsnli/steamappidlist/master/data/hardware_appid.json";

	public const string SteamlessRepo = "atom0s/Steamless";

	public const string CloudRedirectRepo = "Selectively11/CloudRedirect";

	public static string ManifestBackendUrl => Config.ManifestBackendBase;

	public static string ManifestBackendUserAgent => Config.ManifestBackendUserAgent;

	public const string UmamiHost = "https://analytics.lua.tools";

	public const string UmamiWebsiteId = "820d782c-a434-424f-9f90-dee83dc6032e";

	public const string UmamiHostname = "desktop.lua.tools";

	public static readonly string[] GithubReleasesRepos = new string[1] { "https://github.com/Gab-lutang/gabluchi" };

	public const string PluginReleasesOwner = "Gab-lutang";

	public const string PluginReleasesRepo = "gabluchi-plugin";

	public static readonly string[] GithubApiMirrors = new string[1] { "https://lua.tools/api/gh/" };

	public static readonly string[] GithubDownloadMirrors = new string[3] { "https://ghproxy.net/", "https://ghfast.top/", "https://gh.ddlc.top/" };

	public static string GithubReleasesRepo => GithubReleasesRepos[0];
}
