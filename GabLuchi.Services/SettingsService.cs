using System;
using System.IO;
using System.Text.Json;

namespace GabLuchi.Services;

public class SettingsService
{
	private static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi");

	private static readonly string FilePath = Path.Combine(Dir, "settings.json");

	private AppSettings _settings = new AppSettings();

	private static readonly string TmpPath = FilePath + ".tmp";

	private static readonly string BakPath = FilePath + ".bak";

	public string? SteamPathOverride
	{
		get
		{
			return _settings.SteamPathOverride;
		}
		set
		{
			_settings.SteamPathOverride = (string.IsNullOrWhiteSpace(value) ? null : value);
			Save();
		}
	}

	public string? SelectedMode
	{
		get
		{
			return _settings.SelectedMode;
		}
		set
		{
			_settings.SelectedMode = (string.IsNullOrWhiteSpace(value) ? null : value);
			Save();
		}
	}

	public bool AutoUpdateApps
	{
		get
		{
			return _settings.AutoUpdateApps ?? true;
		}
		set
		{
			_settings.AutoUpdateApps = value;
			Save();
		}
	}

	public bool DonateKeys
	{
		get
		{
			return _settings.DonateKeys ?? true;
		}
		set
		{
			_settings.DonateKeys = value;
			Save();
		}
	}

	public int ManagePageSize
	{
		get
		{
			return _settings.ManagePageSize ?? 24;
		}
		set
		{
			_settings.ManagePageSize = value;
			Save();
		}
	}

	public int FixesPageSize
	{
		get
		{
			return _settings.FixesPageSize ?? 24;
		}
		set
		{
			_settings.FixesPageSize = value;
			Save();
		}
	}

	public string? Language
	{
		get
		{
			return _settings.Language;
		}
		set
		{
			_settings.Language = (string.IsNullOrWhiteSpace(value) ? null : value);
			Save();
		}
	}

	public string? HubcapApiKey
	{
		get
		{
			return _settings.HubcapApiKey;
		}
		set
		{
			_settings.HubcapApiKey = (string.IsNullOrWhiteSpace(value) ? null : value);
			Save();
		}
	}

	public string? LicenseToken
	{
		get
		{
			return _settings.LicenseToken;
		}
		set
		{
			_settings.LicenseToken = (string.IsNullOrWhiteSpace(value) ? null : value);
			Save();
		}
	}

	public string? LicenseMachineId
	{
		get
		{
			return _settings.LicenseMachineId;
		}
		set
		{
			_settings.LicenseMachineId = (string.IsNullOrWhiteSpace(value) ? null : value);
			Save();
		}
	}

	public bool StartWithWindows
	{
		get
		{
			return _settings.StartWithWindows == true;
		}
		set
		{
			_settings.StartWithWindows = value;
			Save();
		}
	}

	public bool MinimizeToTray
	{
		get
		{
			return _settings.MinimizeToTray == true;
		}
		set
		{
			_settings.MinimizeToTray = value;
			Save();
		}
	}

	public bool FastFetch
	{
		get
		{
			return _settings.FastFetch == true;
		}
		set
		{
			_settings.FastFetch = value;
			Save();
		}
	}

	public SettingsService()
	{
		Load();
	}

	private void Load()
	{
		if (!TryLoad(FilePath))
		{
			PreserveCorrupt(FilePath);
			if (!TryLoad(BakPath))
			{
				_settings = new AppSettings();
			}
		}
	}

	private bool TryLoad(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return false;
			}
			AppSettings appSettings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path));
			if (appSettings != null)
			{
				_settings = appSettings;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static void PreserveCorrupt(string path)
	{
		try
		{
			if (File.Exists(path) && new FileInfo(path).Length > 0)
			{
				File.Move(path, path + ".corrupt", overwrite: true);
			}
		}
		catch
		{
		}
	}

	private void Save()
	{
		if (_settings.SteamPathOverride == null && _settings.SelectedMode == null && !_settings.AutoUpdateApps.HasValue && !_settings.DonateKeys.HasValue && !_settings.ManagePageSize.HasValue && _settings.Language == null && _settings.HubcapApiKey == null && _settings.LicenseToken == null && _settings.LicenseMachineId == null && !_settings.StartWithWindows.HasValue && !_settings.MinimizeToTray.HasValue && !_settings.FastFetch.HasValue)
		{
			string[] array = new string[3] { FilePath, BakPath, TmpPath };
			foreach (string path in array)
			{
				try
				{
					if (File.Exists(path))
					{
						File.Delete(path);
					}
				}
				catch
				{
				}
			}
			return;
		}
		Directory.CreateDirectory(Dir);
		string contents = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
		{
			WriteIndented = true
		});
		File.WriteAllText(TmpPath, contents);
		try
		{
			if (File.Exists(FilePath))
			{
				File.Copy(FilePath, BakPath, overwrite: true);
			}
		}
		catch
		{
		}
		File.Move(TmpPath, FilePath, overwrite: true);
	}
}
