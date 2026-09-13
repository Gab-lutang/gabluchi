using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class SmartDlcService(
	DlcUnlockerManager dlcManager,
	SteamLibraryService library,
	SteamService steam,
	SteamDepotInfo depots,
	SteamAppListCache appList
)
{
	public record SmartDlcResult(
		long AppId,
		string GameName,
		string InstallDir,
		string? DetectedPlatform,
		DlcUnlockerBase? RecommendedUnlocker,
		IReadOnlyList<long> DlcIds,
		DlcUnlockerInstallResult? InstallResult,
		string Message
	);

	public async Task<IReadOnlyList<SmartDlcResult>> ScanAllAsync(Func<long, string, int, int, Task>? progress = null)
	{
		List<long> appIds = GetInstalledAppIds();
		List<SmartDlcResult> results = new List<SmartDlcResult>();

		for (int i = 0; i < appIds.Count; i++)
		{
			long appId = appIds[i];
			string name = GetGameName(appId);
			if (progress != null)
				await progress(appId, name, i + 1, appIds.Count);
			results.Add(await AnalyzeGameAsync(appId));
		}

		return results;
	}

	public async Task<SmartDlcResult> AnalyzeGameAsync(long appId)
	{
		string name = GetGameName(appId);
		string installDir = library.GetInstallDir(appId) ?? string.Empty;

		if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
		{
			return new SmartDlcResult(appId, name, installDir, null, null, Array.Empty<long>(), null, "Install directory not found.");
		}

		string? platform = dlcManager.DetectPlatform(installDir);
		DlcUnlockerBase? installed = dlcManager.GetInstalledUnlocker(installDir);

		if (platform == null)
		{
			return new SmartDlcResult(appId, name, installDir, null, null, Array.Empty<long>(), null, "No recognizable platform detected (no steam_api or uplay DLLs found).");
		}

		List<DlcUnlockerBase> compatible = dlcManager.GetCompatibleUnlockers(platform);
		DlcUnlockerBase? recommended = compatible.FirstOrDefault() ?? null;

		List<long> dlcIds = new List<long>();
		if (platform == "steam")
		{
			dlcIds = await dlcManager.FetchDlcIdsAsync(appId);
		}

		string status;
		if (installed != null)
		{
			status = $"Already has {installed.DisplayName} installed.";
		}
		else if (dlcIds.Count == 0)
		{
			status = $"Platform: {platform}. No DLCs found via Steam store API (game may have no DLC).";
		}
		else
		{
			status = $"Platform: {platform}. {dlcIds.Count} DLC(s) found. Ready to unlock.";
		}

		return new SmartDlcResult(appId, name, installDir, platform, recommended, dlcIds, null, status);
	}

	public SmartDlcResult InstallForGame(SmartDlcResult analysis)
	{
		if (analysis.RecommendedUnlocker == null)
		{
			return analysis with { InstallResult = new DlcUnlockerInstallResult(false, "No compatible unlocker found.", 0), Message = "No compatible unlocker for this platform." };
		}

		DlcUnlockerInstallResult result = dlcManager.Install(
			analysis.RecommendedUnlocker.Type,
			analysis.InstallDir,
			analysis.DlcIds.ToList(),
			analysis.AppId
		);

		string msg = result.Success
			? $"Installed {analysis.RecommendedUnlocker.DisplayName} successfully."
			: $"Install failed: {result.Error}";

		return analysis with { InstallResult = result, Message = msg };
	}

	public bool UninstallForGame(long appId)
	{
		string installDir = library.GetInstallDir(appId) ?? string.Empty;
		if (string.IsNullOrEmpty(installDir))
			return false;
		return dlcManager.Uninstall(installDir);
	}

	public IReadOnlyList<DlcUnlockerBase> GetAllUnlockers() => dlcManager.GetAll();

	private List<long> GetInstalledAppIds()
	{
		List<long> ids = new List<long>();
		try
		{
			string effectivePath = steam.EffectivePath;
			if (effectivePath == null)
				return ids;

			foreach (string root in GetLibraryRoots(effectivePath))
			{
				string steamappsDir = Path.Combine(root, "steamapps");
				if (!Directory.Exists(steamappsDir))
					continue;

				foreach (string acf in Directory.GetFiles(steamappsDir, "appmanifest_*.acf"))
				{
					string content = File.ReadAllText(acf);
					string? appIdStr = ExtractAcfValue(content, "appid");
					if (appIdStr != null && long.TryParse(appIdStr, out long appId))
					{
						ids.Add(appId);
					}
				}
			}
		}
		catch
		{
		}
		return ids;
	}

	private static IEnumerable<string> GetLibraryRoots(string steamRoot)
	{
		yield return steamRoot;
		string path = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
		if (!File.Exists(path))
			yield break;

		foreach (string line in File.ReadLines(path))
		{
			if (!line.TrimStart().StartsWith("\"path\""))
				continue;

			int firstQuote = line.IndexOf('"', 6);
			int secondQuote = line.IndexOf('"', firstQuote + 1);
			if (firstQuote >= 0 && secondQuote > firstQuote)
			{
				string libPath = line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
				if (Directory.Exists(libPath))
					yield return libPath;
			}
		}
	}

	private static string? ExtractAcfValue(string content, string key)
	{
		foreach (string line in content.Split('\n'))
		{
			string trimmed = line.Trim();
			if (trimmed.StartsWith($"\"{key}\""))
			{
				int firstQuote = trimmed.IndexOf('"', key.Length + 2);
				int secondQuote = trimmed.IndexOf('"', firstQuote + 1);
				if (firstQuote >= 0 && secondQuote > firstQuote)
					return trimmed.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
			}
		}
		return null;
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
