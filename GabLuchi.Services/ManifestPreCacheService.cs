using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class ManifestPreCacheService(SteamService steam, SettingsService settings)
{
	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(30.0)
	};

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	public async Task<PreCacheResult> PreCacheAsync(string steamRoot, IProgress<string>? progress = null, CancellationToken ct = default)
	{
		string luaDir = steam.LuaDir;
		string depotCacheDir = steam.DepotCacheDir;
		if (luaDir == null || depotCacheDir == null)
		{
			return new PreCacheResult(0, 0, 0, "Steam location not found.");
		}
		if (!Directory.Exists(luaDir))
		{
			return new PreCacheResult(0, 0, 0, "Lua directory not found: " + luaDir);
		}
		Directory.CreateDirectory(depotCacheDir);
		string[] luaFiles = Directory.GetFiles(luaDir, "*.lua");
		if (luaFiles.Length == 0)
		{
			return new PreCacheResult(0, 0, 0, "No Lua files found.");
		}
		int skipped = 0;
		int downloaded = 0;
		int failed = 0;
		foreach (string luaFile in luaFiles)
		{
			ct.ThrowIfCancellationRequested();
			string fileName = Path.GetFileNameWithoutExtension(luaFile);
			if (!long.TryParse(fileName, out long appId))
			{
				continue;
			}
			progress?.Report($"Processing {appId}...");
			LuaContents? contents = LuaFileParser.Parse(luaFile, appId);
			if (contents == null || contents.Entries.Count == 0)
			{
				continue;
			}
			Dictionary<long, string> manifestGids = await FetchManifestGidsAsync(appId, contents, ct);
			if (manifestGids.Count == 0)
			{
				continue;
			}
			foreach (LuaEntry entry in contents.Entries)
			{
				ct.ThrowIfCancellationRequested();
				if (!manifestGids.TryGetValue(entry.Id, out string manifestGid))
				{
					continue;
				}
				string manifestPath = Path.Combine(depotCacheDir, $"{entry.Id}_{manifestGid}.manifest");
				if (File.Exists(manifestPath))
				{
					skipped++;
					continue;
				}
				progress?.Report($"  Depot {entry.Id} (manifest {manifestGid})...");
				bool ok = await DownloadManifestAsync(entry.Id, manifestGid, manifestPath, ct);
				if (ok)
				{
					downloaded++;
					progress?.Report($"  Depot {entry.Id} - downloaded");
				}
				else
				{
					failed++;
					progress?.Report($"  Depot {entry.Id} - failed");
				}
			}
		}
		return new PreCacheResult(skipped, downloaded, failed, null);
	}

	private async Task<Dictionary<long, string>> FetchManifestGidsAsync(long appId, LuaContents contents, CancellationToken ct)
	{
		Dictionary<long, string> result = new Dictionary<long, string>();
		try
		{
			using HttpResponseMessage res = await _http.GetAsync($"{AppConfig.SteamCmdInfoUrl}/{appId}", ct);
			if (!res.IsSuccessStatusCode)
			{
				return FallbackManifestIds(contents);
			}
			using JsonDocument doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
			if (!doc.RootElement.TryGetProperty("data", out JsonElement dataProp) ||
				!dataProp.TryGetProperty(appId.ToString(), out JsonElement appProp) ||
				!appProp.TryGetProperty("depots", out JsonElement depotsProp))
			{
				return FallbackManifestIds(contents);
			}
			foreach (LuaEntry entry in contents.Entries)
			{
				string depotKey = entry.Id.ToString();
				if (!depotsProp.TryGetProperty(depotKey, out JsonElement depotProp))
				{
					continue;
				}
				if (depotProp.TryGetProperty("manifests", out JsonElement manifestsProp) &&
					manifestsProp.TryGetProperty("public", out JsonElement publicProp) &&
					publicProp.TryGetProperty("gid", out JsonElement gidProp))
				{
					string? gid = gidProp.GetString();
					if (!string.IsNullOrWhiteSpace(gid))
					{
						result[entry.Id] = gid;
					}
				}
			}
		}
		catch
		{
			return FallbackManifestIds(contents);
		}
		return result.Count > 0 ? result : FallbackManifestIds(contents);
	}

	private static Dictionary<long, string> FallbackManifestIds(LuaContents contents)
	{
		Dictionary<long, string> result = new Dictionary<long, string>();
		foreach (LuaEntry entry in contents.Entries)
		{
			if (!string.IsNullOrWhiteSpace(entry.ManifestId))
			{
				result[entry.Id] = entry.ManifestId;
			}
		}
		return result;
	}

	private async Task<bool> DownloadManifestAsync(long depotId, string manifestGid, string destPath, CancellationToken ct)
	{
		if (await TryGitHubMirrorAsync(depotId, manifestGid, destPath, ct))
		{
			return true;
		}
		string? hubcapKey = settings.HubcapApiKey;
		if (!string.IsNullOrWhiteSpace(hubcapKey))
		{
			if (await TryHubcapAsync(depotId, manifestGid, hubcapKey, destPath, ct))
			{
				return true;
			}
		}
		string? manifestHubKey = settings.ManifestHubApiKey;
		if (!string.IsNullOrWhiteSpace(manifestHubKey))
		{
			if (await TryManifestHubAsync(depotId, manifestGid, manifestHubKey, destPath, ct))
			{
				return true;
			}
		}
		return false;
	}

	private async Task<bool> TryGitHubMirrorAsync(long depotId, string manifestGid, string destPath, CancellationToken ct)
	{
		string url = $"{AppConfig.GitHubMirrorBaseUrl}/{depotId}_{manifestGid}.manifest";
		try
		{
			using HttpResponseMessage res = await _http.GetAsync(url, ct);
			if (!res.IsSuccessStatusCode)
			{
				return false;
			}
			byte[] data = await res.Content.ReadAsByteArrayAsync(ct);
			if (data.Length == 0)
			{
				return false;
			}
			await File.WriteAllBytesAsync(destPath, data, ct);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private async Task<bool> TryHubcapAsync(long depotId, string manifestGid, string apiKey, string destPath, CancellationToken ct)
	{
		string url = $"{AppConfig.HubcapBaseUrl}/api/v1/generate/manifest?depot_id={depotId}&manifest_id={manifestGid}&api_key={Uri.EscapeDataString(apiKey)}";
		try
		{
			using HttpResponseMessage res = await _http.GetAsync(url, ct);
			if (!res.IsSuccessStatusCode)
			{
				return false;
			}
			byte[] data = await res.Content.ReadAsByteArrayAsync(ct);
			if (data.Length == 0)
			{
				return false;
			}
			await File.WriteAllBytesAsync(destPath, data, ct);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private async Task<bool> TryManifestHubAsync(long depotId, string manifestGid, string apiKey, string destPath, CancellationToken ct)
	{
		string url = $"{AppConfig.ManifestHubBaseUrl}/manifest?apikey={Uri.EscapeDataString(apiKey)}&depotid={depotId}&manifestid={manifestGid}";
		try
		{
			using HttpResponseMessage res = await _http.GetAsync(url, ct);
			if (!res.IsSuccessStatusCode)
			{
				return false;
			}
			byte[] data = await res.Content.ReadAsByteArrayAsync(ct);
			if (data.Length == 0)
			{
				return false;
			}
			await File.WriteAllBytesAsync(destPath, data, ct);
			return true;
		}
		catch
		{
			return false;
		}
	}
}

public record PreCacheResult(int Skipped, int Downloaded, int Failed, string? Error);
