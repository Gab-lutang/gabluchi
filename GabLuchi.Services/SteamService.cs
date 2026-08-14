using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace GabLuchi.Services;

public class SteamService(SettingsService settings)
{
	private static readonly (RegistryHive Hive, RegistryView View, string SubKey, string Value)[] RegistryLocations = new(RegistryHive, RegistryView, string, string)[3]
	{
		(RegistryHive.CurrentUser, RegistryView.Registry64, "SOFTWARE\\Valve\\Steam", "SteamPath"),
		(RegistryHive.LocalMachine, RegistryView.Registry64, "SOFTWARE\\WOW6432Node\\Valve\\Steam", "InstallPath"),
		(RegistryHive.LocalMachine, RegistryView.Registry64, "SOFTWARE\\Valve\\Steam", "InstallPath")
	};

	public string? AutoDetectedPath => DetectFromRegistry();

	public string? EffectivePath
	{
		get
		{
			string steamPathOverride = settings.SteamPathOverride;
			if (string.IsNullOrWhiteSpace(steamPathOverride))
			{
				return AutoDetectedPath;
			}
			return Normalize(steamPathOverride);
		}
	}

	public bool IsOverridden => !string.IsNullOrWhiteSpace(settings.SteamPathOverride);

	public bool IsValid
	{
		get
		{
			if (EffectivePath != null)
			{
				return File.Exists(SteamExePathFor(EffectivePath));
			}
			return false;
		}
	}

	public string? StPlugInDir
	{
		get
		{
			string effectivePath = EffectivePath;
			if (effectivePath == null)
			{
				return null;
			}
			return Path.Combine(effectivePath, "config", "stplug-in");
		}
	}

	public string? DepotCacheDir
	{
		get
		{
			string effectivePath = EffectivePath;
			if (effectivePath == null)
			{
				return null;
			}
			return Path.Combine(effectivePath, "config", "depotcache");
		}
	}

	public static string SteamExePathFor(string steamPath)
	{
		return Path.Combine(steamPath, "steam.exe");
	}

	public static void OpenUrl(string url)
	{
		Process.Start(new ProcessStartInfo(url)
		{
			UseShellExecute = true
		});
	}

	public static void RevealInExplorer(string filePath)
	{
		Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + filePath + "\"")
		{
			UseShellExecute = true
		});
	}

	public void StopSteam()
	{
		Process[] processesByName = Process.GetProcessesByName("steam");
		foreach (Process process in processesByName)
		{
			try
			{
				process.Kill(entireProcessTree: true);
				process.WaitForExit(8000);
			}
			catch
			{
			}
			finally
			{
				process.Dispose();
			}
		}
	}

	public bool StartSteam()
	{
		string effectivePath = EffectivePath;
		if (effectivePath == null)
		{
			return false;
		}
		string text = SteamExePathFor(effectivePath);
		if (!File.Exists(text))
		{
			return false;
		}
		try
		{
			Process.Start(new ProcessStartInfo(text)
			{
				UseShellExecute = true
			});
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool RestartSteam()
	{
		StopSteam();
		return StartSteam();
	}

	private static string? DetectFromRegistry()
	{
		(RegistryHive, RegistryView, string, string)[] registryLocations = RegistryLocations;
		for (int i = 0; i < registryLocations.Length; i++)
		{
			var (hKey, view, name, name2) = registryLocations[i];
			try
			{
				using RegistryKey registryKey = RegistryKey.OpenBaseKey(hKey, view);
				using RegistryKey registryKey2 = registryKey.OpenSubKey(name);
				if (registryKey2?.GetValue(name2) is string text && !string.IsNullOrWhiteSpace(text))
				{
					string text2 = Normalize(text);
					if (File.Exists(SteamExePathFor(text2)))
					{
						return text2;
					}
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static string Normalize(string path)
	{
		try
		{
			return Path.GetFullPath(path.Trim().Replace('/', '\\'));
		}
		catch
		{
			return path.Trim().Replace('/', '\\');
		}
	}
}
