using System;
using System.IO;

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

	public static readonly string[] GithubReleasesRepos = new string[1] { "https://github.com/Gab-lutang/gabluchi" };

	private static string? _cachedToken;

	public static string? GithubToken
	{
		get
		{
			if (_cachedToken != null) return _cachedToken;
			try
			{
				string tokenPath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
					"GabLuchi", "github_token.txt");
				if (File.Exists(tokenPath))
				{
					_cachedToken = File.ReadAllText(tokenPath).Trim();
					return _cachedToken;
				}
			}
			catch { }
			return null;
		}
	}

	public const string PluginReleasesOwner = "Gab-lutang";

	public const string PluginReleasesRepo = "gabluchi-plugin";

	public static readonly string[] GithubApiMirrors = new string[0];

	public static readonly string[] GithubDownloadMirrors = new string[3] { "https://ghproxy.net/", "https://ghfast.top/", "https://gh.ddlc.top/" };

	public static string GithubReleasesRepo => GithubReleasesRepos[0];

	public const string GitHubMirrorBaseUrl = "https://raw.githubusercontent.com/qwe213312/k25FCdfEOoEJ42S6/main";

	public const string SteamCmdInfoUrl = "https://api.steamcmd.net/v1/info";
}
