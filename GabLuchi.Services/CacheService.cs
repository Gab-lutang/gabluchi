using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GabLuchi.Services;

public class CacheService
{
	private static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi");

	private static readonly string FilePath = Path.Combine(Dir, "cache.json");

	private CacheData _cache = new CacheData();

	public string? GabLuchiInstalledVersion
	{
		get
		{
			return _cache.GabLuchiInstalledVersion;
		}
		set
		{
			_cache.GabLuchiInstalledVersion = (string.IsNullOrWhiteSpace(value) ? null : value);
			Save();
		}
	}

	public string? GabLuchiInstalledZipDigest
	{
		get
		{
			return _cache.GabLuchiInstalledZipDigest;
		}
		set
		{
			_cache.GabLuchiInstalledZipDigest = (string.IsNullOrWhiteSpace(value) ? null : value);
			Save();
		}
	}

	public bool OnboardingComplete
	{
		get
		{
			return _cache.OnboardingComplete;
		}
		set
		{
			_cache.OnboardingComplete = value;
			Save();
		}
	}

	public CacheService()
	{
		Load();
	}

	public IReadOnlyList<long> GetSteamApiRequestTimes()
	{
		return _cache.SteamApiRequestTimes;
	}

	public void SaveSteamApiRequestTimes(IEnumerable<long> times)
	{
		_cache.SteamApiRequestTimes = times.ToList();
		Save();
	}

	public IReadOnlyCollection<string> GetDonatedAppIds()
	{
		return _cache.DonatedAppIds;
	}

	public void SaveDonatedAppIds(IEnumerable<string> appIds)
	{
		_cache.DonatedAppIds = appIds.Distinct().ToList();
		Save();
	}

	public IReadOnlyList<long> GetHardwareAppIds()
	{
		return _cache.HardwareAppIds;
	}

	public long GetHardwareAppIdsFetchedAt()
	{
		return _cache.HardwareAppIdsFetchedAtMs;
	}

	public void SaveHardwareAppIds(IEnumerable<long> ids, long fetchedAtMs)
	{
		_cache.HardwareAppIds = ids.Distinct().ToList();
		_cache.HardwareAppIdsFetchedAtMs = fetchedAtMs;
		Save();
	}

	public IReadOnlyList<long> GetLoadedAppIds()
	{
		return _cache.LoadedAppIds;
	}

	public void SaveLoadedAppIds(IEnumerable<long> ids)
	{
		_cache.LoadedAppIds = ids.Distinct().ToList();
		Save();
	}

	public void ClearLoadedAppIds()
	{
		_cache.LoadedAppIds = new List<long>();
		Save();
	}

	public void RemoveLoadedAppId(long id)
	{
		if (_cache.LoadedAppIds.Remove(id))
		{
			Save();
		}
	}

	private void Load()
	{
		try
		{
			if (File.Exists(FilePath))
			{
				_cache = JsonSerializer.Deserialize<CacheData>(File.ReadAllText(FilePath)) ?? new CacheData();
			}
		}
		catch
		{
			_cache = new CacheData();
		}
	}

	private void Save()
	{
		if (_cache.GabLuchiInstalledVersion == null && _cache.GabLuchiInstalledZipDigest == null && _cache.SteamApiRequestTimes.Count == 0 && _cache.DonatedAppIds.Count == 0 && _cache.HardwareAppIds.Count == 0 && _cache.HardwareAppIdsFetchedAtMs == 0L && _cache.LoadedAppIds.Count == 0 && !_cache.OnboardingComplete)
		{
			try
			{
				if (File.Exists(FilePath))
				{
					File.Delete(FilePath);
				}
				return;
			}
			catch
			{
				return;
			}
		}
		Directory.CreateDirectory(Dir);
		File.WriteAllText(FilePath, JsonSerializer.Serialize(_cache, new JsonSerializerOptions
		{
			WriteIndented = true
		}));
	}
}
