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

	private static readonly string LogDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi");

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
		string logPath = Path.Combine(LogDir, "precache.log");
		try
		{
			Directory.CreateDirectory(LogDir);
			File.WriteAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Pre-cache started\n");
		}
		catch
		{
		}
		int skipped = 0;
		int downloaded = 0;
		int failed = 0;
		int totalDepots = 0;
		int processed = 0;
		foreach (string luaFile in luaFiles)
		{
			ct.ThrowIfCancellationRequested();
			string fileName = Path.GetFileNameWithoutExtension(luaFile);
			if (!long.TryParse(fileName, out long appId))
			{
				continue;
			}
			LuaContents? contents = LuaFileParser.Parse(luaFile, appId);
			if (contents == null || contents.Entries.Count == 0)
			{
				continue;
			}
			totalDepots += contents.Entries.Count;
		}
		foreach (string luaFile in luaFiles)
		{
			ct.ThrowIfCancellationRequested();
			string fileName = Path.GetFileNameWithoutExtension(luaFile);
			if (!long.TryParse(fileName, out long appId))
			{
				continue;
			}
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
					processed++;
					progress?.Report($"Pre-caching manifests: {processed}/{totalDepots} ({downloaded} downloaded, {skipped} skipped)");
					LogLine(logPath, $"Depot {entry.Id} manifest {manifestGid} - skipped (exists)");
					continue;
				}
				bool ok = await DownloadManifestAsync(entry.Id, manifestGid, manifestPath, logPath, ct);
				if (ok)
				{
					downloaded++;
					LogLine(logPath, $"Depot {entry.Id} manifest {manifestGid} - downloaded");
				}
				else
				{
					failed++;
					LogLine(logPath, $"Depot {entry.Id} manifest {manifestGid} - FAILED (all sources)");
				}
				processed++;
				progress?.Report($"Pre-caching manifests: {processed}/{totalDepots} ({downloaded} downloaded, {skipped} skipped)");
			}
		}
		try
		{
			File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Pre-cache complete: {downloaded} downloaded, {skipped} skipped, {failed} failed\n");
		}
		catch
		{
		}
		return new PreCacheResult(skipped, downloaded, failed, null);
	}

	private static void LogLine(string logPath, string message)
	{
		try
		{
			File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
		}
		catch
		{
		}
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

	private async Task<bool> DownloadManifestAsync(long depotId, string manifestGid, string destPath, string logPath, CancellationToken ct)
	{
		if (await TryGitHubMirrorAsync(depotId, manifestGid, destPath, logPath, ct))
		{
			return true;
		}
		string? hubcapKey = settings.HubcapApiKey;
		if (!string.IsNullOrWhiteSpace(hubcapKey))
		{
			if (await TryHubcapAsync(depotId, manifestGid, hubcapKey, destPath, logPath, ct))
			{
				return true;
			}
		}
		return false;
	}

	private async Task<bool> TryGitHubMirrorAsync(long depotId, string manifestGid, string destPath, string logPath, CancellationToken ct)
	{
		string url = $"{AppConfig.GitHubMirrorBaseUrl}/{depotId}_{manifestGid}.manifest";
		for (int attempt = 0; attempt < 3; attempt++)
		{
			try
			{
				using HttpResponseMessage res = await _http.GetAsync(url, ct);
				if (res.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
				{
					LogLine(logPath, $"  GitHub rate-limited, waiting 10s (attempt {attempt + 1}/3)");
					await Task.Delay(10000, ct);
					continue;
				}
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
				await Task.Delay(100, ct);
				return true;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch
			{
				return false;
			}
		}
		return false;
	}

	private async Task<bool> TryHubcapAsync(long depotId, string manifestGid, string apiKey, string destPath, string logPath, CancellationToken ct)
	{
		string url = $"{AppConfig.HubcapBaseUrl}/api/v1/generate/manifest?depot_id={depotId}&manifest_id={manifestGid}&api_key={Uri.EscapeDataString(apiKey)}";
		try
		{
			using HttpResponseMessage res = await _http.GetAsync(url, ct);
			if (!res.IsSuccessStatusCode)
			{
				LogLine(logPath, $"  Hubcap returned {(int)res.StatusCode}");
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
