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
		HashSet<long> appIds = new HashSet<long>(DiscoverAppIds());

		if (appIds.Count == 0)
		{
			PurgeLibraryState();
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

		PurgeLibraryState();
		lua.DeleteDllFiles();
	}

	private List<long> DiscoverAppIds()
	{
		HashSet<long> ids = new HashSet<long>(library.GetInstalledAppIds());

		foreach (string luaDir in new string?[] { steam.LuaDir, steam.PlugInDir })
		{
			if (luaDir == null || !Directory.Exists(luaDir))
			{
				continue;
			}
			try
			{
				foreach (string file in Directory.GetFiles(luaDir, "*.lua"))
				{
					long? appId = LuaInstaller.AppIdFromFileName(file);
					if (appId.HasValue)
					{
						ids.Add(appId.Value);
					}
				}
			}
			catch
			{
			}
		}

		string? tileDir = steam.AppCacheLibraryCacheDir;
		if (tileDir != null && Directory.Exists(tileDir))
		{
			try
			{
				foreach (string tile in Directory.GetFiles(tileDir))
				{
					string name = Path.GetFileName(tile);
					if (long.TryParse(name, out long appId))
					{
						ids.Add(appId);
					}
				}
			}
			catch
			{
			}
		}

		foreach (string cfgDir in steam.GetUserDataConfigDirs())
		{
			string libCache = Path.Combine(cfgDir, "librarycache");
			if (!Directory.Exists(libCache))
			{
				continue;
			}
			try
			{
				foreach (string file in Directory.GetFiles(libCache, "*.json"))
				{
					string name = Path.GetFileNameWithoutExtension(file);
					if (long.TryParse(name, out long appId))
					{
						ids.Add(appId);
					}
				}
			}
			catch
			{
			}
		}

		return ids.ToList();
	}

	public void DeleteAppPermanently(long appId)
	{
		DeleteManifestsForApp(appId);
		lua.DeleteLua(appId);
		DeletePluginLua(appId);

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

			foreach (string sub in new[] { "shadercache", "compatdata", "downloading", "temp" })
			{
				string path = Path.Combine(root, "steamapps", sub, appId.ToString());
				if (Directory.Exists(path))
				{
					DeleteWithRetry(path);
				}
			}
		}

		ScrubAppTiles(appId);
	}

	private void DeletePluginLua(long appId)
	{
		string? pluginDir = steam.PlugInDir;
		if (pluginDir == null || !Directory.Exists(pluginDir))
		{
			return;
		}
		foreach (string file in Directory.GetFiles(pluginDir, $"{appId}.lua*"))
		{
			try { File.Delete(file); } catch { }
		}
	}

	private void ScrubAppTiles(long appId)
	{
		string? tileDir = steam.AppCacheLibraryCacheDir;
		if (tileDir != null && Directory.Exists(tileDir))
		{
			string tile = Path.Combine(tileDir, appId.ToString());
			if (File.Exists(tile))
			{
				try { File.Delete(tile); } catch { }
			}
		}

		foreach (string cfgDir in steam.GetUserDataConfigDirs())
		{
			string libCache = Path.Combine(cfgDir, "librarycache");
			if (!Directory.Exists(libCache))
			{
				continue;
			}
			foreach (string file in Directory.GetFiles(libCache, $"{appId}.*"))
			{
				try { File.Delete(file); } catch { }
			}
		}
	}

	private void PurgeLibraryState()
	{
		foreach (string luaDir in new string?[] { steam.LuaDir, steam.PlugInDir })
		{
			if (luaDir == null || !Directory.Exists(luaDir))
			{
				continue;
			}
			try
			{
				foreach (string file in Directory.GetFiles(luaDir, "*.lua*"))
				{
					File.Delete(file);
				}
			}
			catch
			{
			}
		}

		string? tileDir = steam.AppCacheLibraryCacheDir;
		if (tileDir != null && Directory.Exists(tileDir))
		{
			try
			{
				foreach (string file in Directory.GetFiles(tileDir))
				{
					File.Delete(file);
				}
			}
			catch
			{
			}
		}

		foreach (string cfgDir in steam.GetUserDataConfigDirs())
		{
			string libCache = Path.Combine(cfgDir, "librarycache");
			if (Directory.Exists(libCache))
			{
				try
				{
					foreach (string file in Directory.GetFiles(libCache))
					{
						File.Delete(file);
					}
				}
				catch
				{
				}
			}

			string localConfig = Path.Combine(cfgDir, "localconfig.vdf");
			if (File.Exists(localConfig))
			{
				try { File.Delete(localConfig); } catch { }
			}

			string shortcuts = Path.Combine(cfgDir, "shortcuts.vdf");
			if (File.Exists(shortcuts))
			{
				try { File.Delete(shortcuts); } catch { }
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