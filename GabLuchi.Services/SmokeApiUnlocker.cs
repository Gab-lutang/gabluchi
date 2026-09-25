using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class SmokeApiUnlocker : DlcUnlockerBase
{
	private static readonly string BundledDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dlc_unlockers", "smokeapi");

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions { WriteIndented = true };

	public override DlcUnlockerType Type => DlcUnlockerType.SmokeApi;

	public override string DisplayName => "SmokeAPI";

	public override string[] ConfigFileNames => new[] { "SmokeAPI.config.json" };

	public override DlcUnlockerType[] ConflictsWith => new[] { DlcUnlockerType.CreamApi };

	public override bool IsAvailable() => Directory.Exists(BundledDir);

	public override bool IsInstalled(string gameDir)
	{
		if (File.Exists(Path.Combine(gameDir, "SmokeAPI.config.json")))
		{
			return true;
		}
		foreach (string backup in Directory.GetFiles(gameDir, "steam_api*_o.dll", SearchOption.AllDirectories))
		{
			return true;
		}
		return false;
	}

	public override DlcUnlockerInstallResult Install(string gameDir, List<long> dlcIds, long appId)
	{
		if (!Directory.Exists(gameDir))
		{
			return new DlcUnlockerInstallResult(false, "Game directory not found.", 0);
		}
		if (!Directory.Exists(BundledDir))
		{
			return new DlcUnlockerInstallResult(false, "SmokeAPI DLLs not found in installation.", 0);
		}
		string? steamApi32 = FindSteamApi(gameDir, "steam_api.dll");
		string? steamApi64 = FindSteamApi(gameDir, "steam_api64.dll");
		if (steamApi32 == null && steamApi64 == null)
		{
			return new DlcUnlockerInstallResult(false, "No steam_api.dll or steam_api64.dll found.", 0);
		}
		try
		{
			int installed = 0;
			if (steamApi32 != null)
			{
				if (InstallToLocation(gameDir, steamApi32, "steam_api.dll", "smoke_api32.dll", dlcIds, appId))
				{
					installed++;
				}
			}
			if (steamApi64 != null)
			{
				if (InstallToLocation(gameDir, steamApi64, "steam_api64.dll", "smoke_api64.dll", dlcIds, appId))
				{
					installed++;
				}
			}
			if (installed == 0)
			{
				return new DlcUnlockerInstallResult(false, "Failed to install SmokeAPI to any location.", 0);
			}
			return new DlcUnlockerInstallResult(true, null, dlcIds.Count);
		}
		catch (Exception ex)
		{
			return new DlcUnlockerInstallResult(false, ex.Message, 0);
		}
	}

	private bool InstallToLocation(string gameDir, string originalDllPath, string dllName, string smokeApiDllName, List<long> dlcIds, long appId)
	{
		string targetDir = Path.GetDirectoryName(originalDllPath) ?? gameDir;
		string backupPath = Path.Combine(targetDir, dllName.Replace(".dll", "_o.dll"));
		if (!backupPath.Equals(originalDllPath, StringComparison.OrdinalIgnoreCase) && File.Exists(originalDllPath) && !File.Exists(backupPath))
		{
			File.Copy(originalDllPath, backupPath, overwrite: false);
		}
		string smokeApiDll = Path.Combine(BundledDir, smokeApiDllName);
		if (!File.Exists(smokeApiDll))
		{
			return false;
		}
		File.Copy(smokeApiDll, originalDllPath, overwrite: true);
		string creamConfig = Path.Combine(targetDir, "cream_api.ini");
		if (File.Exists(creamConfig))
		{
			File.Delete(creamConfig);
		}
		WriteConfig(targetDir, dlcIds, appId);
		return true;
	}

	private static void WriteConfig(string targetDir, List<long> dlcIds, long appId)
	{
		var extraDlcs = new Dictionary<string, object>();
		foreach (long dlc in dlcIds)
		{
			extraDlcs[dlc.ToString()] = new object();
		}
		var config = new Dictionary<string, object>
		{
			["$version"] = 4,
			["logging"] = false,
			["log_steam_http"] = false,
			["default_app_status"] = "unlocked",
			["override_app_status"] = new object(),
			["override_dlc_status"] = new object(),
			["auto_inject_inventory"] = true,
			["extra_inventory_items"] = new List<object>(),
			["extra_dlcs"] = extraDlcs
		};
		string configPath = Path.Combine(targetDir, "SmokeAPI.config.json");
		string json = JsonSerializer.Serialize(config, JsonOpts);
		File.WriteAllText(configPath, json);
	}

	public override bool Uninstall(string gameDir)
	{
		try
		{
			string[] backups = Directory.GetFiles(gameDir, "steam_api*_o.dll", SearchOption.AllDirectories);
			foreach (string backup in backups)
			{
				string dir = Path.GetDirectoryName(backup) ?? gameDir;
				string name = Path.GetFileName(backup);
				string originalName = name.Replace("_o.dll", ".dll");
				string originalPath = Path.Combine(dir, originalName);
				if (File.Exists(backup))
				{
					if (File.Exists(originalPath))
					{
						File.Delete(originalPath);
					}
					File.Move(backup, originalPath);
				}
			}
			string[] smokeDlls = Directory.GetFiles(gameDir, "smoke_api*.dll", SearchOption.AllDirectories);
			foreach (string dll in smokeDlls)
			{
				File.Delete(dll);
			}
			string[] configs = Directory.GetFiles(gameDir, "SmokeAPI.config.json", SearchOption.AllDirectories);
			foreach (string cfg in configs)
			{
				File.Delete(cfg);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string? FindSteamApi(string gameDir, string dllName)
	{
		string root = Path.Combine(gameDir, dllName);
		if (File.Exists(root))
		{
			return root;
		}
		foreach (string found in Directory.GetFiles(gameDir, dllName, SearchOption.AllDirectories))
		{
			if (!found.Contains("_o"))
			{
				return found;
			}
		}
		return null;
	}
}
