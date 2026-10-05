using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GabLuchi.Services;

/// <summary>
/// The unlisted-game search index from Gab-lutang/gabluchi-search: games our
/// sources can add but Steam storesearch no longer returns (delisted titles
/// like GTA San Andreas). Disk-cached for 14 days and served stale when
/// offline, matching SteamAppListCache behaviour.
/// </summary>
public class SearchIndexService
{
	private const string IndexUrl = "https://raw.githubusercontent.com/Gab-lutang/gabluchi-search/main/unlisted.json";

	private static readonly string CacheFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi", "search-unlisted.json");

	private static readonly TimeSpan MaxAge = TimeSpan.FromDays(14.0);

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(30.0)
	};

	public class Entry
	{
		public long AppId { get; set; }

		public string Name { get; set; } = "";
	}

	private class IndexFile
	{
		public string? Generated { get; set; }

		public int Count { get; set; }

		public List<Entry>? Games { get; set; }
	}

	private List<Entry> _games = new List<Entry>();

	private Task? _loading;

	private bool _loaded;

	public IReadOnlyList<Entry> Games => _games;

	/// <summary>
	/// Loads the index once, but a load that ends with no games (offline, 404,
	/// repo not published yet) is retried on the next search instead of being
	/// cached as a permanent failure.
	/// </summary>
	public async Task EnsureLoadedAsync()
	{
		if (_loaded && _games.Count > 0)
		{
			return;
		}
		if (_loading != null)
		{
			await _loading;
			return;
		}
		_loading = LoadAsync();
		try
		{
			await _loading;
		}
		finally
		{
			_loading = null;
		}
		_loaded = _games.Count > 0;
	}

	private async Task LoadAsync()
	{
		List<Entry>? disk = ReadCache();
		if (disk != null && IsFresh() && disk.Count > 0)
		{
			_games = disk;
			return;
		}
		if (disk != null && disk.Count > 0)
		{
			_games = disk;
		}
		try
		{
			string json = await Http.GetStringAsync(IndexUrl);
			IndexFile? file = JsonSerializer.Deserialize<IndexFile>(json, JsonOpts);
			if (file?.Games != null && file.Games.Count > 0)
			{
				_games = file.Games;
				WriteCache(json);
			}
		}
		catch
		{
		}
	}

	private static bool IsFresh()
	{
		try
		{
			return File.Exists(CacheFile) && DateTime.UtcNow - File.GetLastWriteTimeUtc(CacheFile) < MaxAge;
		}
		catch
		{
			return false;
		}
	}

	private static List<Entry>? ReadCache()
	{
		try
		{
			if (!File.Exists(CacheFile))
			{
				return null;
			}
			return JsonSerializer.Deserialize<IndexFile>(File.ReadAllText(CacheFile), JsonOpts)?.Games;
		}
		catch
		{
			return null;
		}
	}

	private static void WriteCache(string json)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(CacheFile)!);
			File.WriteAllText(CacheFile, json);
		}
		catch
		{
		}
	}

	/// <summary>
	/// Token match: every whitespace-separated query token must appear in the
	/// game name (case-insensitive) or in its word initials, so "GTA" hits
	/// "Grand Theft Auto: San Andreas". A digits-only query matches the appid
	/// exactly instead, so pasting "12120" surfaces the card directly.
	/// </summary>
	public List<Entry> Match(string query)
	{
		List<Entry> hits = new List<Entry>();
		string q = query.Trim();
		if (q.Length == 0 || _games.Count == 0)
		{
			return hits;
		}
		bool byAppId = true;
		foreach (char c in q)
		{
			if (!char.IsDigit(c))
			{
				byAppId = false;
				break;
			}
		}
		if (byAppId && long.TryParse(q, out long id))
		{
			foreach (Entry e in _games)
			{
				if (e.AppId == id)
				{
					hits.Add(e);
				}
			}
			return hits;
		}
		string[] tokens = q.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (Entry e in _games)
		{
			string initials = BuildInitials(e.Name);
			bool all = true;
			foreach (string t in tokens)
			{
				if (!e.Name.Contains(t, StringComparison.OrdinalIgnoreCase) &&
					!initials.Contains(t, StringComparison.OrdinalIgnoreCase))
				{
					all = false;
					break;
				}
			}
			if (all)
			{
				hits.Add(e);
			}
		}
		return hits;
	}

	private static string BuildInitials(string name)
	{
		StringBuilder sb = new StringBuilder(name.Length);
		bool atWordStart = true;
		foreach (char c in name)
		{
			if (char.IsLetterOrDigit(c))
			{
				if (atWordStart)
				{
					sb.Append(c);
					atWordStart = false;
				}
			}
			else
			{
				atWordStart = true;
			}
		}
		return sb.ToString();
	}
}
