using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class HardwareAppIdService
{
	private sealed record HardwareApp([property: JsonPropertyName("appid")] long AppId);

	private static readonly TimeSpan MaxAge = TimeSpan.FromDays(14.0);

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	private readonly GithubProxy _gh;

	private readonly CacheService _cache;

	private readonly HashSet<long> _ids;

	private Task? _loadTask;

	public HardwareAppIdService(GithubProxy gh, CacheService cache)
	{
		_gh = gh;
		_cache = cache;
		HashSet<long> hashSet = new HashSet<long>();
		foreach (long hardwareAppId in cache.GetHardwareAppIds())
		{
			hashSet.Add(hardwareAppId);
		}
		_ids = hashSet;
	}

	public bool IsBlacklisted(long appId)
	{
		return _ids.Contains(appId);
	}

	public Task EnsureFreshAsync()
	{
		return _loadTask ?? (_loadTask = RefreshIfStaleAsync());
	}

	private async Task RefreshIfStaleAsync()
	{
		long hardwareAppIdsFetchedAt = _cache.GetHardwareAppIdsFetchedAt();
		if (hardwareAppIdsFetchedAt > 0 && _ids.Count > 0 && DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(hardwareAppIdsFetchedAt) < MaxAge)
		{
			return;
		}
		try
		{
			using HttpResponseMessage res = await _gh.SendAsync("https://raw.githubusercontent.com/jsnli/steamappidlist/master/data/hardware_appid.json");
			if (res == null || !res.IsSuccessStatusCode)
			{
				return;
			}
			List<HardwareApp> list = JsonSerializer.Deserialize<List<HardwareApp>>(await res.Content.ReadAsStringAsync(), JsonOpts);
			if (list == null || list.Count == 0)
			{
				return;
			}
			List<long> list2 = (from e in list
				where e.AppId > 0
				select e.AppId).Distinct().ToList();
			_ids.Clear();
			foreach (long item in list2)
			{
				_ids.Add(item);
			}
			_cache.SaveHardwareAppIds(list2, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
		}
		catch
		{
		}
	}
}
