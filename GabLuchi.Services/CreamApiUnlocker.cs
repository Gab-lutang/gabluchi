using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class CreamApiUnlocker : DlcUnlockerBase
{
	private static readonly string BundledDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dlc_unlockers", "creamapi");

	public override DlcUnlockerType Type => DlcUnlockerType.CreamApi;

	public override string DisplayName => "CreamAPI";

	public override string[] ConfigFileNames => new[] { "cream_api.ini" };

	public override DlcUnlockerType[] ConflictsWith => new[] { DlcUnlockerType.SmokeApi };

	public override bool IsAvailable() => Directory.Exists(BundledDir);

	public override bool IsInstalled(string gameDir)
	{
		if (File.Exists(Path.Combine(gameDir, "cream_api.ini")))
		{
			return true;
		}
		string backup32 = Path.Combine(gameDir, "steam_api_o.dll");
		string backup64 = Path.Combine(gameDir, "steam_api64_o.dll");
		if (File.Exists(backup32) || File.Exists(backup64))
		{
			return true;
		}
		foreach (string found in Directory.GetFiles(gameDir, "cream_api.ini", SearchOption.AllDirectories))
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
			return new DlcUnlockerInstallResult(false, "CreamAPI DLLs not found in installation.", 0);
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
				if (InstallToLocation(steamApi32, "steam_api.dll", "steam_api_o.dll"))
				{
					installed++;
				}
			}
			if (steamApi64 != null)
			{
				if (InstallToLocation(steamApi64, "steam_api64.dll", "steam_api64_o.dll"))
				{
					installed++;
				}
			}
			if (installed == 0)
			{
				return new DlcUnlockerInstallResult(false, "Failed to install CreamAPI to any location.", 0);
			}
			string configDir = Path.GetDirectoryName(steamApi32 ?? steamApi64!) ?? gameDir;
			WriteIniConfig(configDir, dlcIds, appId);
			return new DlcUnlockerInstallResult(true, null, dlcIds.Count);
		}
		catch (Exception ex)
		{
			return new DlcUnlockerInstallResult(false, ex.Message, 0);
		}
	}

	private bool InstallToLocation(string originalDllPath, string dllName, string backupName)
	{
		string targetDir = Path.GetDirectoryName(originalDllPath) ?? "";
		string backupPath = Path.Combine(targetDir, backupName);
		if (File.Exists(originalDllPath) && !File.Exists(backupPath))
		{
			File.Copy(originalDllPath, backupPath, overwrite: false);
		}
		string creamDll = Path.Combine(BundledDir, dllName);
		if (!File.Exists(creamDll))
		{
			return false;
		}
		File.Copy(creamDll, originalDllPath, overwrite: true);
		return true;
	}

	private static void WriteIniConfig(string targetDir, List<long> dlcIds, long appId)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine($"CreamAPI Configuration for App ID {appId}");
		sb.AppendLine();
		sb.AppendLine("[steam]");
		sb.AppendLine($"appid = {appId}");
		sb.AppendLine("unlockall = false");
		sb.AppendLine("orgapi = steam_api_o.dll");
		sb.AppendLine("orgapi64 = steam_api64_o.dll");
		sb.AppendLine("extraprotection = false");
		sb.AppendLine("forceoffline = false");
		sb.AppendLine();
		sb.AppendLine("[steam_misc]");
		sb.AppendLine("disableuserinterface = false");
		sb.AppendLine();
		sb.AppendLine("[dlc]");
		foreach (long dlcId in dlcIds)
		{
			sb.AppendLine($"{dlcId} = DLC_{dlcId}");
		}
		string configPath = Path.Combine(targetDir, "cream_api.ini");
		File.WriteAllText(configPath, sb.ToString());
	}

	public override bool Uninstall(string gameDir)
	{
		try
		{
			UninstallFromDir(gameDir);
			foreach (string sub in Directory.GetDirectories(gameDir))
			{
				UninstallFromDir(sub);
			}
			string rootConfig = Path.Combine(gameDir, "cream_api.ini");
			if (File.Exists(rootConfig))
			{
				File.Delete(rootConfig);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void UninstallFromDir(string dir)
	{
		string backup32 = Path.Combine(dir, "steam_api_o.dll");
		string backup64 = Path.Combine(dir, "steam_api64_o.dll");
		if (File.Exists(backup32))
		{
			string original = Path.Combine(dir, "steam_api.dll");
			if (File.Exists(original))
			{
				File.Delete(original);
			}
			File.Move(backup32, original);
		}
		if (File.Exists(backup64))
		{
			string original = Path.Combine(dir, "steam_api64.dll");
			if (File.Exists(original))
			{
				File.Delete(original);
			}
			File.Move(backup64, original);
		}
		string configPath = Path.Combine(dir, "cream_api.ini");
		if (File.Exists(configPath))
		{
			File.Delete(configPath);
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
