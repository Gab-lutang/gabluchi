using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace GabLuchi.Services;

public class LuaInstaller(SteamService steam, SettingsService settings, CacheService cache)
{
	private bool AutoUpdate => settings.AutoUpdateApps;

	public event Action<long>? Installed;

	private void RecordLoaded(long appId)
	{
		try
		{
			cache.SaveLoadedAppIds(cache.GetLoadedAppIds().Append(appId));
		}
		catch
		{
		}
		try
		{
			this.Installed?.Invoke(appId);
		}
		catch
		{
		}
	}

	private static Regex SetManifestLineRegex()
	{
		return _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__SetManifestLineRegex_6.Instance;
	}

	private static string CommentOutManifestPins(string lua)
	{
		string[] array = lua.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].TrimStart().StartsWith("--"))
			{
				array[i] = SetManifestLineRegex().Replace(array[i], "$1-- $2");
			}
		}
		return string.Join('\n', array);
	}

	private void WriteLua(string sourceLuaPath, string dest, bool forceLocked = false)
	{
		if (AutoUpdate && !forceLocked)
		{
			string lua = File.ReadAllText(sourceLuaPath);
			File.WriteAllText(dest, CommentOutManifestPins(lua));
		}
		else
		{
			File.Copy(sourceLuaPath, dest, overwrite: true);
		}
		StampNow(dest);
	}

	public string? ReadInstalledLua(long appId)
	{
		string stPlugInDir = steam.StPlugInDir;
		if (stPlugInDir == null)
		{
			return null;
		}
		string text = Path.Combine(stPlugInDir, $"{appId}.lua");
		if (!File.Exists(text))
		{
			return null;
		}
		return text;
	}

	public InstallResult InstallLua(string luaPath, long appId, bool forceLocked = false)
	{
		string stPlugInDir = steam.StPlugInDir;
		if (stPlugInDir == null)
		{
			return InstallResult.Fail("Steam location not found — set it in Settings.");
		}
		try
		{
			Directory.CreateDirectory(stPlugInDir);
			string dest = Path.Combine(stPlugInDir, $"{appId}.lua");
			WriteLua(luaPath, dest, forceLocked);
			RecordLoaded(appId);
			return new InstallResult(LuaInstalled: true, 0, Array.Empty<string>(), null);
		}
		catch (Exception ex)
		{
			return InstallResult.Fail(ex.Message);
		}
	}

	private static void StampNow(string path)
	{
		try
		{
			DateTime now = DateTime.Now;
			File.SetCreationTime(path, now);
			File.SetLastWriteTime(path, now);
		}
		catch
		{
		}
	}

	public static bool IsInstallable(string path)
	{
		string extension = Path.GetExtension(path);
		if (!extension.Equals(".lua", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".manifest", StringComparison.OrdinalIgnoreCase))
		{
			return extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public static long? AppIdFromFileName(string path)
	{
		Match match = Regex.Match(Path.GetFileNameWithoutExtension(path), "^\\s*(\\d+)");
		if (!match.Success || !long.TryParse(match.Groups[1].Value, out var result))
		{
			return null;
		}
		return result;
	}

	public static long? AppIdForZip(string zipPath)
	{
		long? num = AppIdFromFileName(zipPath);
		if (num.HasValue)
		{
			return num.GetValueOrDefault();
		}
		try
		{
			using ZipArchive zipArchive = ZipFile.OpenRead(zipPath);
			foreach (ZipArchiveEntry entry in zipArchive.Entries)
			{
				if (entry.Name.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) && long.TryParse(Path.GetFileNameWithoutExtension(entry.Name), out var result))
				{
					return result;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	public InstallResult InstallLuaFile(string luaPath, long appId, bool forceLocked = false)
	{
		return InstallLua(luaPath, appId, forceLocked);
	}

	public InstallResult InstallManifestFile(string manifestPath)
	{
		string depotCacheDir = steam.DepotCacheDir;
		if (depotCacheDir == null)
		{
			return InstallResult.Fail("Steam location not found — set it in Settings.");
		}
		try
		{
			Directory.CreateDirectory(depotCacheDir);
			string text = Path.Combine(depotCacheDir, Path.GetFileName(manifestPath));
			if (!File.Exists(text))
			{
				File.Copy(manifestPath, text, overwrite: false);
				StampNow(text);
			}
			return new InstallResult(LuaInstalled: false, 1, Array.Empty<string>(), null);
		}
		catch (Exception ex)
		{
			return InstallResult.Fail(ex.Message);
		}
	}

	public InstallResult InstallZip(string zipPath, long appId, bool forceLocked = false)
	{
		string stPlugInDir = steam.StPlugInDir;
		string depotCacheDir = steam.DepotCacheDir;
		if (stPlugInDir == null || depotCacheDir == null)
		{
			return InstallResult.Fail("Steam location not found — set it in Settings.");
		}
		ZipArchive zipArchive;
		try
		{
			zipArchive = ZipFile.OpenRead(zipPath);
		}
		catch (Exception ex)
		{
			return InstallResult.Fail("Couldn't open the download: " + ex.Message);
		}
		bool luaInstalled = false;
		int num = 0;
		List<string> list = new List<string>();
		using (zipArchive)
		{
			try
			{
				Directory.CreateDirectory(stPlugInDir);
			}
			catch
			{
			}
			try
			{
				Directory.CreateDirectory(depotCacheDir);
			}
			catch
			{
			}
			foreach (ZipArchiveEntry entry in zipArchive.Entries)
			{
				if (string.IsNullOrEmpty(entry.Name))
				{
					continue;
				}
				string name = entry.Name;
				bool flag = name.EndsWith(".lua", StringComparison.OrdinalIgnoreCase);
				bool flag2 = name.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase);
				if (!flag && !flag2)
				{
					continue;
				}
				string text = (flag ? Path.Combine(stPlugInDir, $"{appId}.lua") : Path.Combine(depotCacheDir, name));
				if (flag2 && File.Exists(text))
				{
					num++;
					continue;
				}
				try
				{
					if (flag)
					{
						string text2 = Path.Combine(Path.GetTempPath(), $"gabluchi_{Guid.NewGuid():N}.lua");
						try
						{
							entry.ExtractToFile(text2, overwrite: true);
							WriteLua(text2, text, forceLocked);
							luaInstalled = true;
							RecordLoaded(appId);
						}
						finally
						{
							try
							{
								File.Delete(text2);
							}
							catch
							{
							}
						}
					}
					else
					{
						entry.ExtractToFile(text, overwrite: true);
						StampNow(text);
						num++;
					}
				}
				catch
				{
					list.Add(name);
				}
			}
		}
		return new InstallResult(luaInstalled, num, list, null);
	}
}
