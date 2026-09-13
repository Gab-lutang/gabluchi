using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class GameHealthService(
	SteamLibraryService library,
	SteamAppListCache appList,
	LuaInstaller lua,
	DlcUnlockerManager dlcManager,
	GoldbergService goldberg,
	SteamApiCheckBypassService bypass,
	SteamlessService steamless
)
{
	private static readonly string[] DllDetectionPatterns = new[]
	{
		"steam_api.dll", "steam_api64.dll",
		"steam_api_o.dll", "steam_api64_o.dll",
		"steamclient.so", "libsteam_api.so"
	};

	private static readonly string[] BypassDllNames = new[]
	{
		"winmm.dll", "version.dll", "winhttp.dll"
	};

	private static readonly string SteamSettingsDir = "steam_settings";

	public async Task<IReadOnlyList<GameHealthReport>> ScanAllAsync(Func<long, string, int, int, Task>? progress = null)
	{
		List<long> appIds = GetInstalledAppIds();
		List<GameHealthReport> reports = new List<GameHealthReport>();

		for (int i = 0; i < appIds.Count; i++)
		{
			long appId = appIds[i];
			string name = GetGameName(appId);

			if (progress != null)
			{
				await progress(appId, name, i + 1, appIds.Count);
			}

			reports.Add(await ScanSingleAsync(appId));
		}

		return reports;
	}

	public async Task<GameHealthReport> ScanSingleAsync(long appId)
	{
		string name = GetGameName(appId);
		string installDir = library.GetInstallDir(appId) ?? string.Empty;
		List<HealthIssue> issues = new List<HealthIssue>();

		if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
		{
			return new GameHealthReport(appId, name, installDir, 0, new[] { new HealthIssue(HealthSeverity.Critical, "Missing", "Install directory not found", "The game folder no longer exists at the expected location.") }, DateTime.Now);
		}

		int score = 100;

		// Check: Lua unlocker installed
		string luaFile = lua.ReadInstalledLua(appId) ?? string.Empty;
		if (string.IsNullOrEmpty(luaFile))
		{
			issues.Add(new HealthIssue(HealthSeverity.Warning, "Unlocker", "No Lua unlocker installed", "SmartSteamEmu Lua not present — game may not have DLC unlocked.", async () =>
			{
				// Lua install requires a source path — we can't auto-fix without it, so this is informational
				return false;
			}));
			score -= 10;
		}

		// Check: DLC unlocker present
		string? platform = dlcManager.DetectPlatform(installDir);
		DlcUnlockerBase? installed = dlcManager.GetInstalledUnlocker(installDir);
		if (platform != null && installed == null)
		{
			issues.Add(new HealthIssue(HealthSeverity.Warning, "DLC Unlocker", "No DLC unlocker installed", $"Platform detected ({platform}) but no unlocker DLL found.", null));
			score -= 10;
		}
		else if (platform != null && installed != null)
		{
			// Check if smoke_api/creamapi has required config files
			if (platform == "steam")
			{
				string smokeApiConfig = Path.Combine(installDir, "steam_appid.txt");
				if (!File.Exists(smokeApiConfig))
				{
					issues.Add(new HealthIssue(HealthSeverity.Warning, "DLC Unlocker", "Missing steam_appid.txt", "SmokeAPI/CreamAPI may not function without steam_appid.txt in the game folder.", null));
					score -= 5;
				}
			}
		}

		// Check: steam_api DLL presence (original should be preserved for Goldberg)
		List<string> apiDlls = FindSteamApiDlls(installDir);
		if (apiDlls.Count == 0)
		{
			issues.Add(new HealthIssue(HealthSeverity.Critical, "Core", "No steam_api DLL found", "The game folder is missing steam_api.dll or steam_api64.dll — game may not launch.", null));
			score -= 30;
		}

		// Check: Goldberg steam_settings folder
		bool hasGoldberg = Directory.Exists(Path.Combine(installDir, SteamSettingsDir));
		if (!hasGoldberg)
		{
			issues.Add(new HealthIssue(HealthSeverity.Info, "Goldberg", "No steam_settings folder", "Goldberg emulator files not detected. Apply Goldberg if the game requires offline play.", async () =>
			{
				GoldbergApplyResult result = await goldberg.ApplyAsync(appId);
				return result.Replaced > 0;
			}));
			score -= 5;
		}

		// Check: Bypass DLLs present
		bool hasBypass = BypassDllNames.Any(d => File.Exists(Path.Combine(installDir, d)));
		if (!hasBypass)
		{
			issues.Add(new HealthIssue(HealthSeverity.Info, "Bypass", "No SteamAPI check bypass DLL", "SteamAPICheckBypass (winmm.dll, version.dll, or winhttp.dll) not found. May be needed for Steamless-patched games.", async () =>
			{
				bypass.Apply(installDir);
				return true;
			}));
			score -= 5;
		}

		// Check: Manifest pin files in Lua (stale pins cause lock to old depots)
		if (!string.IsNullOrEmpty(luaFile))
		{
			try
			{
				string luaContent = File.ReadAllText(luaFile);
				if (luaContent.Contains("set_manifest"))
				{
					issues.Add(new HealthIssue(HealthSeverity.Warning, "Lua", "Stale manifest pins detected", "The Lua file contains set_manifest calls that pin to old depot IDs — can cause download/update failures.", async () =>
					{
						// We can't auto-uncomment, but we can flag it
						return false;
					}));
					score -= 8;
				}
			}
			catch
			{
			}
		}

		// Check: Game executable exists and is valid size
		string? mainExe = FindMainExe(installDir);
		if (mainExe == null)
		{
			issues.Add(new HealthIssue(HealthSeverity.Critical, "Core", "No main executable found", "No .exe file found in the root of the game folder.", null));
			score -= 20;
		}
		else
		{
			FileInfo fi = new FileInfo(mainExe);
			if (fi.Length < 1024)
			{
				issues.Add(new HealthIssue(HealthSeverity.Critical, "Core", "Executable is suspiciously small", $"The main .exe is only {fi.Length} bytes — it may be corrupt or a stub.", null));
				score -= 15;
			}
		}

		// Check: Disk space
		try
		{
			DriveInfo di = new DriveInfo(Path.GetPathRoot(installDir));
			long freeGB = di.AvailableFreeSpace / (1024 * 1024 * 1024);
			if (freeGB < 2)
			{
				issues.Add(new HealthIssue(HealthSeverity.Warning, "Disk", "Low disk space", $"Only {freeGB} GB free on {Path.GetPathRoot(installDir)} — updates may fail.", null));
				score -= 5;
			}
		}
		catch
		{
		}

		score = Math.Max(0, score);
		return new GameHealthReport(appId, name, installDir, score, issues, DateTime.Now);
	}

	public async Task<(int fixedCount, int failedCount)> FixAllIssuesAsync(IReadOnlyList<GameHealthReport> reports)
	{
		int fixedCount = 0;
		int failedCount = 0;

		foreach (GameHealthReport report in reports)
		{
			foreach (HealthIssue issue in report.Issues.Where(i => i.HasFix))
			{
				try
				{
					bool ok = await issue.FixAction!();
					if (ok)
						fixedCount++;
					else
						failedCount++;
				}
				catch
				{
					failedCount++;
				}
			}
		}

		return (fixedCount, failedCount);
	}

	private List<long> GetInstalledAppIds()
	{
		return library.GetInstalledAppIds();
	}

	private List<string> FindSteamApiDlls(string installDir)
	{
		List<string> found = new List<string>();
		foreach (string pattern in DllDetectionPatterns)
		{
			string fullPath = Path.Combine(installDir, pattern);
			if (File.Exists(fullPath))
				found.Add(fullPath);
		}
		return found;
	}

	private static string? FindMainExe(string installDir)
	{
		try
		{
			FileInfo[] exes = new DirectoryInfo(installDir).GetFiles("*.exe");
			if (exes.Length == 0)
				return null;

			// Prefer the largest .exe (likely the main game)
			return exes.OrderByDescending(f => f.Length).First().FullName;
		}
		catch
		{
			return null;
		}
	}

	private string GetGameName(long appId)
	{
		try
		{
			return appList.GetName(appId) ?? $"App {appId}";
		}
		catch
		{
			return $"App {appId}";
		}
	}
}
