using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class SteamAppListCache
{
	private const string MorrenusUrl = "https://applist.morrenus.xyz/";

	private const string SteamUrl = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";

	private static readonly string CacheFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi", "steam-applist.json");

	private static readonly TimeSpan MaxAge = TimeSpan.FromDays(14.0);

	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(60.0)
	};

	private readonly ConcurrentDictionary<long, string> _names = new ConcurrentDictionary<long, string>();

	private Task? _loadTask;

	public string? GetName(long appid)
	{
		if (!_names.TryGetValue(appid, out string value))
		{
			return null;
		}
		return value;
	}

	public Task EnsureLoadedAsync()
	{
		return _loadTask ?? (_loadTask = LoadAsync());
	}

	private async Task LoadAsync()
	{
		long key;
		string value;
		try
		{
			if (File.Exists(CacheFile) && DateTime.UtcNow - File.GetLastWriteTimeUtc(CacheFile) < MaxAge)
			{
				Dictionary<long, string> dictionary = JsonSerializer.Deserialize<Dictionary<long, string>>(await File.ReadAllTextAsync(CacheFile));
				if (dictionary != null && dictionary.Count > 0)
				{
					foreach (KeyValuePair<long, string> item in dictionary)
					{
						item.Deconstruct(out key, out value);
						long key2 = key;
						string value2 = value;
						_names[key2] = value2;
					}
					return;
				}
			}
		}
		catch
		{
		}
		bool flag = await TryDownloadAsync("https://applist.morrenus.xyz/", flatArray: true);
		if (!flag)
		{
			flag = await TryDownloadAsync("https://api.steampowered.com/ISteamApps/GetAppList/v2/", flatArray: false);
		}
		if (flag)
		{
			await SaveAsync();
			return;
		}
		try
		{
			if (!_names.IsEmpty || !File.Exists(CacheFile))
			{
				return;
			}
			Dictionary<long, string> dictionary2 = JsonSerializer.Deserialize<Dictionary<long, string>>(await File.ReadAllTextAsync(CacheFile));
			if (dictionary2 == null)
			{
				return;
			}
			foreach (KeyValuePair<long, string> item2 in dictionary2)
			{
				item2.Deconstruct(out key, out value);
				long key3 = key;
				string value3 = value;
				_names[key3] = value3;
			}
		}
		catch
		{
		}
	}

	private async Task<bool> TryDownloadAsync(string url, bool flatArray)
	{
		_ = 2;
		try
		{
			bool result;
			await using (Stream stream = await _http.GetStreamAsync(url))
			{
				using JsonDocument jsonDocument = await JsonDocument.ParseAsync(stream);
				foreach (JsonElement item in (flatArray ? jsonDocument.RootElement : jsonDocument.RootElement.GetProperty("applist").GetProperty("apps")).EnumerateArray())
				{
					long @int = item.GetProperty("appid").GetInt64();
					string value = item.GetProperty("name").GetString();
					if (!string.IsNullOrWhiteSpace(value))
					{
						_names[@int] = value;
					}
				}
				result = !_names.IsEmpty;
			}
			return result;
		}
		catch
		{
			return false;
		}
	}

	private async Task SaveAsync()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(CacheFile));
			await File.WriteAllTextAsync(CacheFile, JsonSerializer.Serialize(_names));
		}
		catch
		{
		}
	}
}
