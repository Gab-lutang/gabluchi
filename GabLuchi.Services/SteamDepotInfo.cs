using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class SteamDepotInfo
{
	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(15.0)
	};

	private readonly ConcurrentDictionary<long, AppDepotInfo?> _cache = new ConcurrentDictionary<long, AppDepotInfo>();

	private readonly ConcurrentDictionary<long, Task<AppDepotInfo?>> _inFlight = new ConcurrentDictionary<long, Task<AppDepotInfo>>();

	public Task<AppDepotInfo?> GetAsync(long appId, CancellationToken ct = default(CancellationToken))
	{
		if (_cache.TryGetValue(appId, out AppDepotInfo value))
		{
			return Task.FromResult(value);
		}
		return _inFlight.GetOrAdd(appId, (long id) => FetchAsync(id, ct));
	}

	private async Task<AppDepotInfo?> FetchAsync(long appId, CancellationToken ct)
	{
		_ = 1;
		try
		{
			using HttpResponseMessage res = await _http.GetAsync($"https://api.steamcmd.net/v1/info/{appId}", ct);
			if (!res.IsSuccessStatusCode)
			{
				return Cache(appId, null);
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
			if (!jsonDocument.RootElement.TryGetProperty("data", out var value) || !value.TryGetProperty(appId.ToString(), out var value2))
			{
				return Cache(appId, null);
			}
			List<ContentDepot> list = new List<ContentDepot>();
			if (value2.TryGetProperty("depots", out var value3) && value3.ValueKind == JsonValueKind.Object)
			{
				foreach (JsonProperty item in value3.EnumerateObject())
				{
					if (!long.TryParse(item.Name, out var result))
					{
						continue;
					}
					JsonElement value4 = item.Value;
					if (value4.ValueKind != JsonValueKind.Object)
					{
						continue;
					}
					JsonElement value5 = item.Value;
					bool isShared = value5.TryGetProperty("depotfromapp", out value4);
					JsonElement value6;
					long result2;
					long? dlcAppId = ((value5.TryGetProperty("dlcappid", out value6) && long.TryParse(value6.GetString(), out result2)) ? new long?(result2) : ((long?)null));
					string os = null;
					string language = null;
					if (value5.TryGetProperty("config", out var value7) && value7.ValueKind == JsonValueKind.Object)
					{
						if (value7.TryGetProperty("oslist", out var value8))
						{
							os = value8.GetString();
						}
						JsonElement value10;
						if (value7.TryGetProperty("dlclanguage", out var value9))
						{
							language = value9.GetString();
						}
						else if (value7.TryGetProperty("language", out value10))
						{
							language = value10.GetString();
						}
					}
					list.Add(new ContentDepot(result, ReadPublicSize(value5), dlcAppId, isShared, os, language));
				}
			}
			List<long> list2 = new List<long>();
			if (value2.TryGetProperty("extended", out var value11) && value11.TryGetProperty("listofdlc", out var value12))
			{
				string text = value12.GetString();
				if (text != null)
				{
					string[] array = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					for (int i = 0; i < array.Length; i++)
					{
						if (long.TryParse(array[i], out var result3))
						{
							list2.Add(result3);
						}
					}
				}
			}
			IReadOnlyList<string> launchExes = ParseLaunchExes(value2);
			return Cache(appId, new AppDepotInfo(appId, list, list2, launchExes));
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		catch
		{
			return Cache(appId, null);
		}
		finally
		{
			_inFlight.TryRemove(appId, out Task<AppDepotInfo> _);
		}
	}

	private static long ReadPublicSize(JsonElement depot)
	{
		if (depot.TryGetProperty("manifests", out var value) && value.TryGetProperty("public", out var value2) && value2.TryGetProperty("size", out var value3) && long.TryParse(value3.GetString(), out var result))
		{
			return result;
		}
		return 0L;
	}

	private static IReadOnlyList<string> ParseLaunchExes(JsonElement app)
	{
		if (!app.TryGetProperty("config", out var value) || value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("launch", out var value2) || value2.ValueKind != JsonValueKind.Object)
		{
			return Array.Empty<string>();
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (JsonProperty item2 in value2.EnumerateObject())
		{
			JsonElement value3 = item2.Value;
			if (value3.ValueKind != JsonValueKind.Object || !value3.TryGetProperty("executable", out var value4))
			{
				continue;
			}
			string text = value4.GetString();
			if (text == null || text.Length <= 0)
			{
				continue;
			}
			bool flag = true;
			bool flag2 = false;
			bool flag3 = false;
			if (value3.TryGetProperty("config", out var value5) && value5.ValueKind == JsonValueKind.Object)
			{
				if (value5.TryGetProperty("oslist", out var value6))
				{
					string text2 = value6.GetString();
					if (text2 != null)
					{
						flag = text2.Contains("windows", StringComparison.OrdinalIgnoreCase);
					}
				}
				flag2 = value5.TryGetProperty("betakey", out var value7) && !string.IsNullOrWhiteSpace(value7.GetString());
				flag3 = value5.TryGetProperty("ownsdlc", out var _);
			}
			if (!(!flag || flag2 || flag3))
			{
				string item = text.Replace('/', '\\');
				((value3.TryGetProperty("type", out var value9) && string.Equals(value9.GetString(), "default", StringComparison.OrdinalIgnoreCase)) ? list : list2).Add(item);
			}
		}
		return list.Concat(list2).Distinct<string>(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private AppDepotInfo? Cache(long appId, AppDepotInfo? info)
	{
		_cache[appId] = info;
		return info;
	}
}
