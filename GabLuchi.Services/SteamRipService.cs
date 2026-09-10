using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class SteamRipService
{
	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromMinutes(10.0),
		DefaultRequestHeaders =
		{
			{ "User-Agent", "GabLuchi/1.0" }
		}
	};

	private static readonly string CacheDir = Path.Combine(Path.GetTempPath(), "GabLuchi");
	private static readonly string CacheFile = Path.Combine(CacheDir, "steamrip_cache.json");
	private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24.0);
	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	private const string SteamRipJsonUrl =
		"https://raw.githubusercontent.com/7ROBE/SteamRip-Json/refs/heads/main/steamrip_games.json";

	public async Task<List<SteamRipEntry>> FetchGamesAsync(CancellationToken ct = default)
	{
		if (File.Exists(CacheFile))
		{
			try
			{
				DateTime lastWrite = File.GetLastWriteTimeUtc(CacheFile);
				if (DateTime.UtcNow - lastWrite < CacheTtl)
				{
					string cached = await File.ReadAllTextAsync(CacheFile, ct);
					SteamRipRoot? cachedRoot = JsonSerializer.Deserialize<SteamRipRoot>(cached, JsonOpts);
					if (cachedRoot?.Downloads != null && cachedRoot.Downloads.Count > 0)
						return cachedRoot.Downloads;
				}
			}
			catch { }
		}

		HttpResponseMessage res = await Http.GetAsync(SteamRipJsonUrl, ct);
		res.EnsureSuccessStatusCode();
		string json = await res.Content.ReadAsStringAsync(ct);
		SteamRipRoot? root = JsonSerializer.Deserialize<SteamRipRoot>(json, JsonOpts);

		try
		{
			Directory.CreateDirectory(CacheDir);
			await File.WriteAllTextAsync(CacheFile, json, ct);
		}
		catch { }

		return root?.Downloads ?? new List<SteamRipEntry>();
	}

	public List<SteamRipEntry> Search(string query, List<SteamRipEntry> allGames)
	{
		if (string.IsNullOrWhiteSpace(query))
			return allGames.Take(50).ToList();

		string normalized = query.Trim().ToLowerInvariant();

		List<SteamRipEntry> exact = allGames
			.Where(e => RemoveDownloadSuffix(e.Title).Equals(normalized, StringComparison.OrdinalIgnoreCase))
			.ToList();
		if (exact.Count > 0) return exact;

		List<SteamRipEntry> contains = allGames
			.Where(e => RemoveDownloadSuffix(e.Title).Contains(normalized, StringComparison.OrdinalIgnoreCase))
			.ToList();
		if (contains.Count > 0) return contains;

		string[] tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return allGames
			.Where(e => tokens.All(t => RemoveDownloadSuffix(e.Title).Contains(t, StringComparison.OrdinalIgnoreCase)))
			.OrderByDescending(e => CountMatches(RemoveDownloadSuffix(e.Title), tokens))
			.ToList();
	}

	public static string GetHosterName(string url)
	{
		if (url.Contains("megadb.net", StringComparison.OrdinalIgnoreCase)) return "MegaDB";
		if (url.Contains("buzzheavier.com", StringComparison.OrdinalIgnoreCase)) return "BuzzHeavier";
		if (url.Contains("gofile.io", StringComparison.OrdinalIgnoreCase)) return "GoFile";
		if (url.Contains("1fichier.com", StringComparison.OrdinalIgnoreCase)) return "1fichier";
		if (url.Contains("filecrypt", StringComparison.OrdinalIgnoreCase)) return "FileCrypt";
		if (url.Contains("datanodes.to", StringComparison.OrdinalIgnoreCase)) return "DataNodes";
		if (url.Contains("pixeldrain.com", StringComparison.OrdinalIgnoreCase)) return "Pixeldrain";
		return "Link";
	}

	public static void OpenInBrowser(string url)
	{
		Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
	}

	private static string RemoveDownloadSuffix(string title)
	{
		int idx = title.IndexOf(" Free Download", StringComparison.OrdinalIgnoreCase);
		if (idx > 0) return title.Substring(0, idx).Trim();
		return title.Trim();
	}

	private static int CountMatches(string name, string[] tokens)
	{
		int count = 0;
		foreach (string token in tokens)
		{
			if (name.Contains(token, StringComparison.OrdinalIgnoreCase))
				count++;
		}
		return count;
	}
}
