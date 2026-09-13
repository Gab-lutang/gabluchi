using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class QuickFixService(
	SteamLibraryService library,
	DlcUnlockerManager dlcManager,
	GoldbergService goldberg,
	SteamApiCheckBypassService bypass,
	SteamlessService steamless
)
{
	public record QuickFixStatus(int HealthScore, string HealthLabel, string HealthColor, bool HasDlcUnlocker, string Platform, int FixableIssues);

	public Task<QuickFixStatus> GetStatusAsync(long appId)
	{
		return Task.Run(() =>
		{
			string installDir = library.GetInstallDir(appId) ?? string.Empty;

			if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
			{
				return new QuickFixStatus(0, "Missing", "#ef4444", false, "", 0);
			}

			int score = 100;
			int fixable = 0;

			string? platform = dlcManager.DetectPlatform(installDir);
			DlcUnlockerBase? installed = dlcManager.GetInstalledUnlocker(installDir);
			bool hasDlc = installed != null;
			if (platform != null && !hasDlc)
			{
				score -= 10;
				fixable++;
			}

			bool hasSteamApi = File.Exists(Path.Combine(installDir, "steam_api.dll"))
				|| File.Exists(Path.Combine(installDir, "steam_api64.dll"));
			if (!hasSteamApi)
			{
				score -= 25;
			}

			bool hasGoldberg = Directory.Exists(Path.Combine(installDir, "steam_settings"));
			if (!hasGoldberg)
			{
				score -= 5;
				fixable++;
			}

			bool hasBypass = new[] { "winmm.dll", "version.dll", "winhttp.dll" }
				.Any(d => File.Exists(Path.Combine(installDir, d)));
			if (!hasBypass)
			{
				fixable++;
			}

			score = Math.Max(0, score);

			string label, color;
			if (score >= 90) { label = "Healthy"; color = "#22c55e"; }
			else if (score >= 60) { label = "Fair"; color = "#eab308"; }
			else { label = "Critical"; color = "#ef4444"; }

			return new QuickFixStatus(score, label, color, hasDlc, platform ?? "", fixable);
		});
	}

	public async Task<QuickFixStatus> ApplyQuickFixesAsync(long appId)
	{
		string installDir = library.GetInstallDir(appId) ?? string.Empty;
		if (!string.IsNullOrEmpty(installDir) && Directory.Exists(installDir))
		{
			// Apply Goldberg
			await goldberg.ApplyAsync(appId);

			// Apply bypass
			bypass.Apply(installDir);
		}

		return await GetStatusAsync(appId);
	}
}
