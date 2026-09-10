using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class SteamApiCheckBypassService
{
	private static readonly string BundledBypassDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steam_api_bypass");

	private static readonly string[] BypassDllNames = new string[3] { "winmm.dll", "version.dll", "winhttp.dll" };

	private static readonly string[] SteamSettingsFiles = new string[12]
	{
		"achievements.json", "branches.json", "configs.app.ini", "configs.main.ini",
		"configs.overlay.ini", "configs.user.ini", "default_items.json", "items.json",
		"stats.txt", "steam_appid.txt", "supported_languages.txt", "achievement_images"
	};

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		WriteIndented = true
	};

	public static bool IsAvailable()
	{
		return File.Exists(Path.Combine(BundledBypassDir, "SteamAPICheckBypass.dll"))
			&& File.Exists(Path.Combine(BundledBypassDir, "SteamAPICheckBypass_x32.dll"));
	}

	public void Apply(string installDir, bool goldbergApplied = false)
	{
		if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
		{
			return;
		}
		string mainExe = FindMainExe(installDir);
		if (mainExe == null)
		{
			return;
		}
		string mainExeDir = Path.GetDirectoryName(mainExe);
		bool is64Bit = DetectBitness(mainExe);
		string bypassSource = Path.Combine(BundledBypassDir, is64Bit ? "SteamAPICheckBypass.dll" : "SteamAPICheckBypass_x32.dll");
		if (!File.Exists(bypassSource))
		{
			return;
		}
		string targetDll = Path.Combine(mainExeDir, "winmm.dll");
		if (!File.Exists(targetDll))
		{
			File.Copy(bypassSource, targetDll);
		}
		string jsonPath = Path.Combine(mainExeDir, "SteamAPICheckBypass.json");
		if (File.Exists(jsonPath))
		{
			return;
		}
		WriteBypassJson(installDir, mainExe, goldbergApplied);
	}

	public bool IsApplied(string installDir)
	{
		if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
		{
			return false;
		}
		try
		{
			if (Directory.GetFiles(installDir, "SteamAPICheckBypass.json", SearchOption.AllDirectories).Length > 0)
			{
				return true;
			}
			foreach (string dllName in BypassDllNames)
			{
				if (Directory.GetFiles(installDir, dllName, SearchOption.AllDirectories).Length > 0)
				{
					return true;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	public void Remove(string installDir)
	{
		if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
		{
			return;
		}
		try
		{
			foreach (string dllName in BypassDllNames)
			{
				foreach (string path in Directory.GetFiles(installDir, dllName, SearchOption.AllDirectories))
				{
					try
					{
						File.Delete(path);
					}
					catch
					{
					}
				}
			}
			foreach (string path in Directory.GetFiles(installDir, "SteamAPICheckBypass.json", SearchOption.AllDirectories))
			{
				try
				{
					File.Delete(path);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private void WriteBypassJson(string installDir, string mainExe, bool goldbergApplied)
	{
		string mainExeDir = Path.GetDirectoryName(mainExe);
		string mainExeName = Path.GetFileName(mainExe);
		Dictionary<string, object> config = new Dictionary<string, object>();
		if (!goldbergApplied)
		{
			config[mainExeName] = new Dictionary<string, object>
			{
				["mode"] = "file_redirect",
				["to"] = mainExeName + ".bak",
				["file_must_exist"] = true
			};
		}
		List<string> apiDlls = goldbergApplied ? FindBundledSteamApiDlls(installDir) : FindSteamApiDlls(installDir);
		if (!goldbergApplied)
		{
			foreach (string dllPath in apiDlls)
			{
				string rel = Path.GetRelativePath(mainExeDir, dllPath);
				config[rel] = new Dictionary<string, object>
				{
					["mode"] = "file_redirect",
					["to"] = rel + ".bak",
					["file_must_exist"] = true
				};
			}
		}
		foreach (string dllPath in apiDlls)
		{
			string dllDir = Path.GetDirectoryName(dllPath);
			string relDir = Path.GetRelativePath(mainExeDir, dllDir);
			string steamSettingsRel = Path.Combine(relDir, "steam_settings");
			config[steamSettingsRel] = new Dictionary<string, object>
			{
				["mode"] = "file_hide"
			};
			foreach (string fileName in SteamSettingsFiles)
			{
				string fileRel = Path.Combine(steamSettingsRel, fileName);
				config[fileRel] = new Dictionary<string, object>
				{
					["mode"] = "file_hide"
				};
			}
		}
		string json = JsonSerializer.Serialize(config, JsonOpts);
		File.WriteAllText(Path.Combine(mainExeDir, "SteamAPICheckBypass.json"), json);
	}

	private static readonly string[] SkipExePatterns = new string[]
	{
		"createdump", "unins", "setup", "install", "uninstall", "vcredist", "dotnet",
		"crashreport", "crashhandler", "cefprocess", "cef_process", "steamwebhelper"
	};

	private static string FindMainExe(string installDir)
	{
		try
		{
			string[] exes = Directory.GetFiles(installDir, "*.exe", SearchOption.TopDirectoryOnly);
			if (exes.Length == 0)
			{
				return null;
			}
			string candidate = exes.FirstOrDefault((string e) => !SkipExePatterns.Any((string s) => Path.GetFileNameWithoutExtension(e).Contains(s, StringComparison.OrdinalIgnoreCase)));
			return candidate ?? exes[0];
		}
		catch
		{
			return null;
		}
	}

	private static bool DetectBitness(string exePath)
	{
		try
		{
			using FileStream fs = File.OpenRead(exePath);
			if (fs.Length < 0x40)
			{
				return false;
			}
			byte[] buf = new byte[4];
			fs.Seek(0x3C, SeekOrigin.Begin);
			if (fs.Read(buf, 0, 4) != 4)
			{
				return false;
			}
			int peOffset = BitConverter.ToInt32(buf, 0);
			if (peOffset + 6 > fs.Length)
			{
				return false;
			}
			fs.Seek(peOffset + 4, SeekOrigin.Begin);
			byte[] machine = new byte[2];
			if (fs.Read(machine, 0, 2) != 2)
			{
				return false;
			}
			ushort machineVal = BitConverter.ToUInt16(machine, 0);
			return machineVal == 0x8664 || machineVal == 0xAA64;
		}
		catch
		{
			return false;
		}
	}

	private static List<string> FindSteamApiDlls(string installDir)
	{
		List<string> results = new List<string>();
		try
		{
			results.AddRange(Directory.GetFiles(installDir, "steam_api.dll.bak", SearchOption.AllDirectories));
			results.AddRange(Directory.GetFiles(installDir, "steam_api64.dll.bak", SearchOption.AllDirectories));
		}
		catch
		{
		}
		return results;
	}

	private static List<string> FindBundledSteamApiDlls(string installDir)
	{
		List<string> results = new List<string>();
		try
		{
			results.AddRange(Directory.GetFiles(installDir, "steam_api.dll", SearchOption.AllDirectories));
			results.AddRange(Directory.GetFiles(installDir, "steam_api64.dll", SearchOption.AllDirectories));
		}
		catch
		{
		}
		return results;
	}
}
