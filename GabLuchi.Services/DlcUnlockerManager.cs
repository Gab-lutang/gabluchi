using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using GabLuchi.Models;

namespace GabLuchi.Services;

public partial class DlcUnlockerManager(SteamService steam, SteamLibraryService library)
{
	private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(15.0) };

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

	private readonly Dictionary<DlcUnlockerType, DlcUnlockerBase> _unlockers = new Dictionary<DlcUnlockerType, DlcUnlockerBase>();

	public void Register(DlcUnlockerBase unlocker)
	{
		_unlockers[unlocker.Type] = unlocker;
	}

	public IReadOnlyList<DlcUnlockerBase> GetAll() => _unlockers.Values.ToList();

	public DlcUnlockerBase? Get(DlcUnlockerType type) => _unlockers.TryGetValue(type, out var u) ? u : null;

	public string? DetectPlatform(string gameDir)
	{
		if (File.Exists(Path.Combine(gameDir, "steam_api.dll")) || File.Exists(Path.Combine(gameDir, "steam_api64.dll")))
		{
			return "steam";
		}
		if (File.Exists(Path.Combine(gameDir, "uplay_r1_loader.dll")) || File.Exists(Path.Combine(gameDir, "uplay_r1_loader64.dll")))
		{
			return "ubisoft_r1";
		}
		if (File.Exists(Path.Combine(gameDir, "upc_r2_loader.dll")) || File.Exists(Path.Combine(gameDir, "upc_r2_loader64.dll")))
		{
			return "ubisoft_r2";
		}
		string[] allDlls = Directory.GetFiles(gameDir, "*.dll", SearchOption.AllDirectories);
		foreach (string dll in allDlls)
		{
			string name = Path.GetFileName(dll).ToLowerInvariant();
			if (name == "steam_api.dll" || name == "steam_api64.dll")
			{
				return "steam";
			}
			if (name == "uplay_r1_loader.dll" || name == "uplay_r1_loader64.dll")
			{
				return "ubisoft_r1";
			}
			if (name == "upc_r2_loader.dll" || name == "upc_r2_loader64.dll")
			{
				return "ubisoft_r2";
			}
		}
		return null;
	}

	public List<DlcUnlockerBase> GetCompatibleUnlockers(string? platform)
	{
		if (platform == null)
		{
			return _unlockers.Values.ToList();
		}
		List<DlcUnlockerBase> result = new List<DlcUnlockerBase>();
		foreach (DlcUnlockerBase u in _unlockers.Values)
		{
			if (!u.IsAvailable())
			{
				continue;
			}
			if (platform == "steam" && (u.Type == DlcUnlockerType.SmokeApi || u.Type == DlcUnlockerType.CreamApi))
			{
				result.Add(u);
			}
			else if (platform == "ubisoft_r1" && u.Type == DlcUnlockerType.UplayR1)
			{
				result.Add(u);
			}
			else if (platform == "ubisoft_r2" && u.Type == DlcUnlockerType.UplayR2)
			{
				result.Add(u);
			}
		}
		return result;
	}

	public DlcUnlockerBase? GetInstalledUnlocker(string gameDir)
	{
		foreach (DlcUnlockerBase u in _unlockers.Values)
		{
			if (u.IsInstalled(gameDir))
			{
				return u;
			}
		}
		return null;
	}

	public long? DetectAppId(string gameDir)
	{
		try
		{
			string? effectivePath = steam.EffectivePath;
			if (effectivePath == null)
			{
				return null;
			}
			foreach (string root in GetLibraryRoots(effectivePath))
			{
				string steamappsDir = Path.Combine(root, "steamapps");
				if (!Directory.Exists(steamappsDir))
				{
					continue;
				}
				foreach (string acfFile in Directory.GetFiles(steamappsDir, "appmanifest_*.acf"))
				{
					string content = File.ReadAllText(acfFile);
					string? installdir = ExtractAcfValue(content, "installdir");
					if (installdir == null)
					{
						continue;
					}
					string expectedDir = Path.Combine(root, "steamapps", "common", installdir);
					if (string.Equals(expectedDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
						gameDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
						StringComparison.OrdinalIgnoreCase))
					{
						string? appidStr = ExtractAcfValue(content, "appid");
						if (appidStr != null && long.TryParse(appidStr, out long appid))
						{
							return appid;
						}
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	public async System.Threading.Tasks.Task<List<long>> FetchDlcIdsAsync(long appId, System.Threading.CancellationToken ct = default)
	{
		try
		{
			string url = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=us&l=english";
			HttpResponseMessage res = await Http.GetAsync(url, ct);
			res.EnsureSuccessStatusCode();
			string json = await res.Content.ReadAsStringAsync(ct);
			using JsonDocument doc = JsonDocument.Parse(json);
			if (SteamAppDetailsParser.TryGetAppElement(doc.RootElement, appId, out JsonElement appElement)
				&& appElement.TryGetProperty("data", out JsonElement dataElement)
				&& dataElement.TryGetProperty("dlc", out JsonElement dlcElement)
				&& dlcElement.ValueKind == JsonValueKind.Array)
			{
				List<long> dlcIds = new List<long>();
				foreach (JsonElement item in dlcElement.EnumerateArray())
				{
					if (item.TryGetInt64(out long dlcId))
					{
						dlcIds.Add(dlcId);
					}
				}
				return dlcIds;
			}
		}
		catch
		{
		}
		return new List<long>();
	}

	public DlcUnlockerInstallResult Install(DlcUnlockerType type, string gameDir, List<long> dlcIds, long appId)
	{
		if (!_unlockers.TryGetValue(type, out DlcUnlockerBase? unlocker))
		{
			return new DlcUnlockerInstallResult(false, $"Unlocker type {type} not registered.", 0);
		}
		DlcUnlockerBase? installed = GetInstalledUnlocker(gameDir);
		if (installed != null)
		{
			if (installed.Type == type)
			{
				installed.Uninstall(gameDir);
			}
			else if (installed.ConflictsWith.Contains(type) || unlocker.ConflictsWith.Contains(installed.Type))
			{
				return new DlcUnlockerInstallResult(false, $"Conflicting unlocker ({installed.DisplayName}) is already installed. Uninstall it first.", 0);
			}
		}
		return unlocker.Install(gameDir, dlcIds, appId);
	}

	public bool Uninstall(string gameDir)
	{
		DlcUnlockerBase? installed = GetInstalledUnlocker(gameDir);
		if (installed == null)
		{
			return false;
		}
		return installed.Uninstall(gameDir);
	}

	private static IEnumerable<string> GetLibraryRoots(string steamRoot)
	{
		yield return steamRoot;
		string vdfPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
		if (!File.Exists(vdfPath))
		{
			yield break;
		}
		string content;
		try
		{
			content = File.ReadAllText(vdfPath);
		}
		catch
		{
			yield break;
		}
		foreach (Match m in VdfPathRegex().Matches(content))
		{
			string text = m.Groups[1].Value.Replace("\\\\", "\\");
			if (!string.Equals(text, steamRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(text))
			{
				yield return text;
			}
		}
	}

	private static string? ExtractAcfValue(string acfContent, string key)
	{
		string pattern = $"\"{Regex.Escape(key)}\"\\s+\"([^\"]+)\"";
		Match m = Regex.Match(acfContent, pattern);
		return m.Success ? m.Groups[1].Value : null;
	}

	[GeneratedRegex("\"path\"\\s*\"([^\"]+)\"", RegexOptions.Compiled)]
	private static partial Regex VdfPathRegex();
}
