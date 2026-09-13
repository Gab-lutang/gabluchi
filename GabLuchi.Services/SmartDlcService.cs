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
	)
	{
		public bool HasRecommendedUnlocker => RecommendedUnlocker != null;
	}

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
		return library.GetInstalledAppIds();
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
