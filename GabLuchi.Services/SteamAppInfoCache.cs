using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class SteamAppInfoCache
{
	private static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi");

	private static readonly string DetailsDir = Path.Combine(Dir, "details");

	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(15.0)
	};

	private readonly ConcurrentDictionary<long, SteamAppInfo?> _cache = new ConcurrentDictionary<long, SteamAppInfo>();

	private readonly CacheService _cache2;

	private readonly SemaphoreSlim _rateGate = new SemaphoreSlim(1, 1);

	private readonly Queue<DateTime> _requestTimes = new Queue<DateTime>();

	private static readonly TimeSpan Window = TimeSpan.FromSeconds(200.0);

	private const int MaxPerWindow = 190;

	private DateTime _lastPersist = DateTime.MinValue;

	private readonly ConcurrentDictionary<long, AppFilterData> _filterCache = new ConcurrentDictionary<long, AppFilterData>();

	private int _interactiveWaiting;

	public SteamAppInfoCache(CacheService cache)
	{
		_cache2 = cache;
		DateTime utcNow = DateTime.UtcNow;
		foreach (long steamApiRequestTime in _cache2.GetSteamApiRequestTimes())
		{
			DateTime utcDateTime = DateTimeOffset.FromUnixTimeMilliseconds(steamApiRequestTime).UtcDateTime;
			if (utcNow - utcDateTime < Window)
			{
				_requestTimes.Enqueue(utcDateTime);
			}
		}
	}

	public static string GuessHeaderImageUrl(long appid)
	{
		return $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appid}/header.jpg";
	}

	public SteamAppInfo? GetCached(long appid)
	{
		if (_cache.TryGetValue(appid, out SteamAppInfo value))
		{
			return value;
		}
		SteamAppInfo steamAppInfo = ReadInfoFromDetails(appid);
		_cache[appid] = steamAppInfo;
		return steamAppInfo;
	}

	private static SteamAppInfo? ReadInfoFromDetails(long appid)
	{
		try
		{
			if (!File.Exists(DetailsPath(appid)))
			{
				return null;
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(DetailsPath(appid)));
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.ValueKind != JsonValueKind.Object)
			{
				return null;
			}
			JsonElement value;
			string text = (rootElement.TryGetProperty("name", out value) ? value.GetString() : null);
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			JsonElement value2;
			string headerImage = (rootElement.TryGetProperty("header_image", out value2) ? value2.GetString() : null);
			return new SteamAppInfo(text, headerImage);
		}
		catch
		{
			return null;
		}
	}

	public async Task<SteamAppInfo?> ResolveAsync(long appid, CancellationToken ct = default(CancellationToken))
	{
		if (_cache.TryGetValue(appid, out SteamAppInfo value))
		{
			return value;
		}
		string url = $"https://store.steampowered.com/api/appdetails?appids={appid}&cc=us&l=english";
		for (int attempt = 0; attempt < 3; attempt++)
		{
			await ThrottleAsync(ct);
			try
			{
				using HttpResponseMessage res = await _http.GetAsync(url, ct);
				HttpStatusCode statusCode = res.StatusCode;
				if ((statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.TooManyRequests) ? true : false)
				{
					if (attempt >= 2)
					{
						return null;
					}
					await Task.Delay(TimeSpan.FromSeconds(4 * (attempt + 1)), ct);
					continue;
				}
				if (!res.IsSuccessStatusCode)
				{
					return null;
				}
				using JsonDocument jsonDocument = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
				JsonElement property = jsonDocument.RootElement.GetProperty(appid.ToString());
				if (!property.GetProperty("success").GetBoolean())
				{
					SaveFullDetailsAsync(appid, "{}");
					return null;
				}
				JsonElement property2 = property.GetProperty("data");
				string text = property2.GetProperty("name").GetString();
				if (string.IsNullOrWhiteSpace(text))
				{
					return null;
				}
				JsonElement value2;
				string headerImage = (property2.TryGetProperty("header_image", out value2) ? value2.GetString() : null);
				SteamAppInfo steamAppInfo = new SteamAppInfo(text, headerImage);
				_cache[appid] = steamAppInfo;
				SaveFullDetailsAsync(appid, property2.GetRawText());
				return steamAppInfo;
			}
			catch (OperationCanceledException)
			{
				return null;
			}
			catch
			{
				return null;
			}
		}
		return null;
	}

	public bool HasFullDetails(long appid)
	{
		return File.Exists(DetailsPath(appid));
	}

	public string? GetFullDetails(long appid)
	{
		try
		{
			return HasFullDetails(appid) ? File.ReadAllText(DetailsPath(appid)) : null;
		}
		catch
		{
			return null;
		}
	}

	public AppFilterData? GetFilterData(long appid)
	{
		if (_filterCache.TryGetValue(appid, out AppFilterData value))
		{
			return value;
		}
		string fullDetails = GetFullDetails(appid);
		if (fullDetails == null)
		{
			return null;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(fullDetails);
			JsonElement rootElement = jsonDocument.RootElement;
			JsonElement value2;
			string type = (rootElement.TryGetProperty("type", out value2) ? value2.GetString() : null);
			List<string> list = new List<string>();
			if (rootElement.TryGetProperty("genres", out var value3) && value3.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement item in value3.EnumerateArray())
				{
					if (item.TryGetProperty("description", out var value4))
					{
						string text = value4.GetString();
						if (text != null)
						{
							list.Add(text);
						}
					}
				}
			}
			bool windows = false;
			bool mac = false;
			bool linux = false;
			if (rootElement.TryGetProperty("platforms", out var value5) && value5.ValueKind == JsonValueKind.Object)
			{
				windows = value5.TryGetProperty("windows", out var value6) && value6.ValueKind == JsonValueKind.True;
				mac = value5.TryGetProperty("mac", out var value7) && value7.ValueKind == JsonValueKind.True;
				linux = value5.TryGetProperty("linux", out var value8) && value8.ValueKind == JsonValueKind.True;
			}
			int? releaseYear = null;
			DateTime? releaseDate = null;
			string releaseDateText = null;
			if (rootElement.TryGetProperty("release_date", out var value9) && value9.TryGetProperty("date", out var value10))
			{
				string text2 = value10.GetString();
				if (text2 != null)
				{
					releaseDateText = (string.IsNullOrWhiteSpace(text2) ? null : text2.Trim());
					if (DateTime.TryParse(text2, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
					{
						releaseDate = result;
						releaseYear = result.Year;
					}
					else
					{
						Match match = Regex.Match(text2, "\\b(19|20)\\d{2}\\b");
						if (match.Success && int.TryParse(match.Value, out var result2))
						{
							releaseYear = result2;
							releaseDate = new DateTime(result2, 1, 1);
						}
					}
				}
			}
			JsonElement value11;
			bool isFree = rootElement.TryGetProperty("is_free", out value11) && value11.ValueKind == JsonValueKind.True;
			int? metacritic = null;
			if (rootElement.TryGetProperty("metacritic", out var value12) && value12.TryGetProperty("score", out var value13) && value13.TryGetInt32(out var value14))
			{
				metacritic = value14;
			}
			long? reviews = null;
			if (rootElement.TryGetProperty("recommendations", out var value15) && value15.TryGetProperty("total", out var value16) && value16.TryGetInt64(out var value17))
			{
				reviews = value17;
			}
			bool isAdult = false;
			if (rootElement.TryGetProperty("content_descriptors", out var value18) && value18.TryGetProperty("ids", out var value19) && value19.ValueKind == JsonValueKind.Array)
			{
				isAdult = value19.EnumerateArray().Any(delegate(JsonElement e)
				{
					int value20;
					bool flag = e.TryGetInt32(out value20);
					if (flag)
					{
						bool flag2 = (uint)(value20 - 3) <= 1u;
						flag = flag2;
					}
					return flag;
				});
			}
			AppFilterData appFilterData = new AppFilterData(type, list, windows, mac, linux, releaseYear, releaseDate, releaseDateText, isFree, metacritic, reviews, isAdult);
			_filterCache[appid] = appFilterData;
			return appFilterData;
		}
		catch
		{
			return null;
		}
	}

	public async Task<GameDetails?> ResolveGameDetailsAsync(long appid, CancellationToken ct = default(CancellationToken))
	{
		await EnsureFullDetailsAsync(appid, ct);
		return GetGameDetails(appid);
	}

	public GameDetails? GetGameDetails(long appid)
	{
		string fullDetails = GetFullDetails(appid);
		if (string.IsNullOrWhiteSpace(fullDetails) || fullDetails == "{}")
		{
			return null;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(fullDetails);
			JsonElement rootElement = jsonDocument.RootElement;
			JsonElement value;
			string text = (rootElement.TryGetProperty("name", out value) ? value.GetString() : null);
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			List<string> list = new List<string>();
			if (rootElement.TryGetProperty("genres", out var value2) && value2.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement item in value2.EnumerateArray())
				{
					if (item.TryGetProperty("description", out var value3))
					{
						string text2 = value3.GetString();
						if (text2 != null)
						{
							list.Add(text2);
						}
					}
				}
			}
			string baseAppId = null;
			if (rootElement.TryGetProperty("fullgame", out var value4) && value4.ValueKind == JsonValueKind.Object && value4.TryGetProperty("appid", out var value5))
			{
				baseAppId = ((value5.ValueKind == JsonValueKind.String) ? value5.GetString() : value5.GetRawText());
			}
			JsonElement value6;
			long value7;
			JsonElement value8;
			JsonElement value9;
			JsonElement value10;
			JsonElement value11;
			return new GameDetails
			{
				Name = text,
				AppId = ((rootElement.TryGetProperty("steam_appid", out value6) && value6.TryGetInt64(out value7)) ? value7 : appid),
				Type = (rootElement.TryGetProperty("type", out value8) ? (value8.GetString() ?? "") : ""),
				BaseAppId = baseAppId,
				Genres = list,
				HeaderImage = (rootElement.TryGetProperty("header_image", out value9) ? value9.GetString() : null),
				ReleaseDate = ((rootElement.TryGetProperty("release_date", out value10) && value10.TryGetProperty("date", out value11)) ? value11.GetString() : null)
			};
		}
		catch
		{
			return null;
		}
	}

	public async Task<bool> EnsureFullDetailsAsync(long appid, CancellationToken ct = default(CancellationToken), bool background = false)
	{
		if (HasFullDetails(appid))
		{
			return true;
		}
		string url = $"https://store.steampowered.com/api/appdetails?appids={appid}&cc=us&l=english";
		for (int attempt = 0; attempt < 3; attempt++)
		{
			await ThrottleAsync(ct, background);
			try
			{
				using HttpResponseMessage res = await _http.GetAsync(url, ct);
				HttpStatusCode statusCode = res.StatusCode;
				if ((statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.TooManyRequests) ? true : false)
				{
					if (attempt >= 2)
					{
						return false;
					}
					await Task.Delay(TimeSpan.FromSeconds(4 * (attempt + 1)), ct);
					continue;
				}
				if (!res.IsSuccessStatusCode)
				{
					return false;
				}
				using JsonDocument doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
				JsonElement property = doc.RootElement.GetProperty(appid.ToString());
				if (!property.GetProperty("success").GetBoolean())
				{
					await SaveFullDetailsAsync(appid, "{}");
					return true;
				}
				JsonElement data = property.GetProperty("data");
				await SaveFullDetailsAsync(appid, data.GetRawText());
				if (!_cache.ContainsKey(appid) && data.TryGetProperty("name", out var value))
				{
					string text = value.GetString();
					if (text != null && text.Length > 0)
					{
						JsonElement value2;
						string headerImage = (data.TryGetProperty("header_image", out value2) ? value2.GetString() : null);
						_cache[appid] = new SteamAppInfo(text, headerImage);
					}
				}
				return true;
			}
			catch (OperationCanceledException)
			{
				return false;
			}
			catch
			{
				return false;
			}
		}
		return false;
	}

	public async Task BackfillFullDetailsAsync(IEnumerable<long> appids, Action? onProgress = null, CancellationToken ct = default(CancellationToken))
	{
		foreach (long appid in appids)
		{
			if (ct.IsCancellationRequested)
			{
				return;
			}
			if (!HasFullDetails(appid))
			{
				await EnsureFullDetailsAsync(appid, ct, background: true);
				onProgress?.Invoke();
				try
				{
					await Task.Delay(TimeSpan.FromMilliseconds(750.0), ct);
				}
				catch (OperationCanceledException)
				{
					return;
				}
			}
		}
	}

	private static string DetailsPath(long appid)
	{
		return Path.Combine(DetailsDir, $"{appid}.json");
	}

	private async Task SaveFullDetailsAsync(long appid, string rawJson)
	{
		try
		{
			Directory.CreateDirectory(DetailsDir);
			await File.WriteAllTextAsync(DetailsPath(appid), rawJson);
		}
		catch
		{
		}
	}

	private async Task ThrottleAsync(CancellationToken ct, bool background = false)
	{
		if (!background)
		{
			Interlocked.Increment(ref _interactiveWaiting);
		}
		try
		{
			await _rateGate.WaitAsync(ct);
			bool held = true;
			try
			{
				DateTime utcNow;
				while (true)
				{
					if (background && Volatile.Read(in _interactiveWaiting) > 0)
					{
						_rateGate.Release();
						held = false;
						await Task.Delay(50, ct);
						await _rateGate.WaitAsync(ct);
						held = true;
						continue;
					}
					utcNow = DateTime.UtcNow;
					while (_requestTimes.Count > 0 && utcNow - _requestTimes.Peek() > Window)
					{
						_requestTimes.Dequeue();
					}
					if (_requestTimes.Count < 190)
					{
						break;
					}
					TimeSpan timeSpan = _requestTimes.Peek() + Window - utcNow;
					_rateGate.Release();
					held = false;
					await Task.Delay((timeSpan > TimeSpan.Zero) ? timeSpan : TimeSpan.FromMilliseconds(100.0), ct);
					await _rateGate.WaitAsync(ct);
					held = true;
				}
				_requestTimes.Enqueue(utcNow);
				PersistWindow(utcNow);
			}
			finally
			{
				if (held)
				{
					_rateGate.Release();
				}
			}
		}
		finally
		{
			if (!background)
			{
				Interlocked.Decrement(ref _interactiveWaiting);
			}
		}
	}

	private void PersistWindow(DateTime now)
	{
		if (!(now - _lastPersist < TimeSpan.FromSeconds(5.0)))
		{
			_lastPersist = now;
			IEnumerable<long> times = _requestTimes.Select((DateTime t) => new DateTimeOffset(t, TimeSpan.Zero).ToUnixTimeMilliseconds());
			_cache2.SaveSteamApiRequestTimes(times);
		}
	}
}
