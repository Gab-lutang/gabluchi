using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class UplayR2Unlocker : DlcUnlockerBase
{
	private static readonly string BundledDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dlc_unlockers", "uplayr2");

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions { WriteIndented = true };

	public override DlcUnlockerType Type => DlcUnlockerType.UplayR2;

	public override string DisplayName => "UplayR2 (Newer Ubisoft)";

	public override string[] ConfigFileNames => new[] { "UplayR2Unlocker.jsonc" };

	public override DlcUnlockerType[] ConflictsWith => Array.Empty<DlcUnlockerType>();

	public override bool IsInstalled(string gameDir)
	{
		return File.Exists(Path.Combine(gameDir, "upc_r2_loader.dll"))
			|| File.Exists(Path.Combine(gameDir, "upc_r2_loader64.dll"));
	}

	public override DlcUnlockerInstallResult Install(string gameDir, List<long> dlcIds, long appId)
	{
		if (!Directory.Exists(gameDir))
		{
			return new DlcUnlockerInstallResult(false, "Game directory not found.", 0);
		}
		if (!Directory.Exists(BundledDir))
		{
			return new DlcUnlockerInstallResult(false, "UplayR2 DLLs not found in installation.", 0);
		}
		try
		{
			int installed = 0;
			string dll32 = Path.Combine(BundledDir, "upc_r2_loader.dll");
			string dll64 = Path.Combine(BundledDir, "upc_r2_loader64.dll");
			if (File.Exists(dll32))
			{
				File.Copy(dll32, Path.Combine(gameDir, "upc_r2_loader.dll"), overwrite: true);
				installed++;
			}
			if (File.Exists(dll64))
			{
				File.Copy(dll64, Path.Combine(gameDir, "upc_r2_loader64.dll"), overwrite: true);
				installed++;
			}
			if (installed == 0)
			{
				return new DlcUnlockerInstallResult(false, "No UplayR2 DLLs found to install.", 0);
			}
			WriteConfig(gameDir, dlcIds, appId);
			return new DlcUnlockerInstallResult(true, null, dlcIds.Count);
		}
		catch (Exception ex)
		{
			return new DlcUnlockerInstallResult(false, ex.Message, 0);
		}
	}

	private static void WriteConfig(string gameDir, List<long> dlcIds, long appId)
	{
		var config = new Dictionary<string, object>
		{
			["app_id"] = appId,
			["dlc_ids"] = dlcIds,
			["unlock_all"] = false
		};
		string configPath = Path.Combine(gameDir, "UplayR2Unlocker.jsonc");
		string json = JsonSerializer.Serialize(config, JsonOpts);
		File.WriteAllText(configPath, json);
	}

	public override bool Uninstall(string gameDir)
	{
		try
		{
			string f1 = Path.Combine(gameDir, "upc_r2_loader.dll");
			string f2 = Path.Combine(gameDir, "upc_r2_loader64.dll");
			string cfg = Path.Combine(gameDir, "UplayR2Unlocker.jsonc");
			if (File.Exists(f1)) File.Delete(f1);
			if (File.Exists(f2)) File.Delete(f2);
			if (File.Exists(cfg)) File.Delete(cfg);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
