using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class DemolishService(SteamService steam, LuaInstaller lua, SteamLibraryService library, SteamOwnershipService ownership)
{
	public async Task DemolishAllGamesAsync()
	{
		bool restartSteam = steam.IsRunning;
		steam.StopSteam();
		try
		{
			await DeleteAllGamesPermanentlyAsync();
		}
		finally
		{
			if (restartSteam)
			{
				steam.StartSteam();
			}
		}
	}

	public async Task DemolishAppAsync(long appId)
	{
		bool restartSteam = steam.IsRunning;
		steam.StopSteam();
		try
		{
			DeleteAppPermanently(appId);
		}
		finally
		{
			if (restartSteam)
			{
				steam.StartSteam();
			}
		}
	}

	public async Task DeleteAllGamesPermanentlyAsync()
	{
		HashSet<long> appIds = new HashSet<long>(library.GetInstalledAppIds());
		string? luaDir = steam.LuaDir;
		if (luaDir != null && Directory.Exists(luaDir))
		{
			foreach (string file in Directory.GetFiles(luaDir, "*.lua"))
			{
				long? appId = LuaInstaller.AppIdFromFileName(file);
				if (appId.HasValue)
				{
					appIds.Add(appId.Value);
				}
			}
		}

		if (appIds.Count == 0)
		{
			lua.DeleteDllFiles();
			return;
		}

		HashSet<long> owned = new HashSet<long>();
		try
		{
			string? owner = FindLastOwner();
			if (!string.IsNullOrWhiteSpace(owner))
			{
				owned = (await ownership.GetOwnedAppIdsAsync(owner)) ?? new HashSet<long>();
			}
		}
		catch
		{
			owned = new HashSet<long>();
		}

		foreach (long appId in appIds)
		{
			if (owned.Contains(appId))
			{
				continue;
			}
			DeleteAppPermanently(appId);
		}

		lua.DeleteDllFiles();
	}

	public void DeleteAppPermanently(long appId)
	{
		DeleteManifestsForApp(appId);
		lua.DeleteLua(appId);

		string? installDir = library.GetInstallDir(appId);
		if (installDir != null && Directory.Exists(installDir))
		{
			DeleteWithRetry(installDir);
		}

		foreach (string root in library.GetLibraryRootsList())
		{
			string acf = Path.Combine(root, "steamapps", $"appmanifest_{appId}.acf");
			if (File.Exists(acf))
			{
				try { File.Delete(acf); } catch { }
			}
		}
	}

	private void DeleteManifestsForApp(long appId)
	{
		string? depotDir = steam.DepotCacheDir;
		if (depotDir == null || !Directory.Exists(depotDir))
		{
			return;
		}

		HashSet<long> depotIds = new HashSet<long>();

		string? luaDir = steam.LuaDir;
		if (luaDir != null)
		{
			string luaPath = Path.Combine(luaDir, $"{appId}.lua");
			if (File.Exists(luaPath))
			{
				try
				{
					string lua = File.ReadAllText(luaPath);
					foreach (Match m in Regex.Matches(lua, @"setmanifest\s+(\d+)"))
					{
						if (long.TryParse(m.Groups[1].Value, out long depotId))
						{
							depotIds.Add(depotId);
						}
					}
				}
				catch { }
			}
		}

		foreach (string root in library.GetLibraryRootsList())
		{
			string acfPath = Path.Combine(root, "steamapps", $"appmanifest_{appId}.acf");
			if (!File.Exists(acfPath))
			{
				continue;
			}
			try
			{
				string acf = File.ReadAllText(acfPath);
				int idx = acf.IndexOf("\"InstalledDepots\"", StringComparison.OrdinalIgnoreCase);
				string section = idx >= 0 ? acf.Substring(idx) : acf;
				foreach (Match m in Regex.Matches(section, @"""(\d+)""\s*[\r\n]*\{[^}]*""manifest""\s*""(\d+)"""))
				{
					if (long.TryParse(m.Groups[1].Value, out long depotId))
					{
						depotIds.Add(depotId);
					}
				}
			}
			catch { }
		}

		if (depotIds.Count == 0)
		{
			return;
		}

		foreach (long depotId in depotIds)
		{
			try
			{
				foreach (string mf in Directory.GetFiles(depotDir, $"{depotId}_*.manifest"))
				{
					File.Delete(mf);
				}
			}
			catch { }
		}
	}

	private string? FindLastOwner()
	{
		try
		{
			foreach (string root in library.GetLibraryRootsList())
			{
				string steamappsDir = Path.Combine(root, "steamapps");
				if (!Directory.Exists(steamappsDir))
				{
					continue;
				}
				foreach (string acfPath in Directory.GetFiles(steamappsDir, "appmanifest_*.acf"))
				{
					try
					{
						string text = File.ReadAllText(acfPath);
						Match m = Regex.Match(text, @"""LastOwner""\s*""(\d+)""");
						if (m.Success)
						{
							return m.Groups[1].Value;
						}
					}
					catch { }
				}
			}
		}
		catch { }
		return null;
	}

	private static void DeleteWithRetry(string dir)
	{
		for (int attempt = 0; attempt < 5; attempt++)
		{
			try
			{
				Directory.Delete(dir, recursive: true);
				return;
			}
			catch
			{
				if (attempt == 4)
				{
					return;
				}
				Thread.Sleep(500);
			}
		}
	}
}