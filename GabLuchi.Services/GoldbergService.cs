using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class GoldbergService(SteamLibraryService library)
{
	private static readonly string BundledGoldbergDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "goldberg");
	private static readonly string ToolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "goldberg", "tools");

	public async Task<GoldbergApplyResult> ApplyAsync(long appId, CancellationToken ct = default(CancellationToken))
	{
		return await Task.Run(() => ApplyCore(appId), ct);
	}

	private GoldbergApplyResult ApplyCore(long appId)
	{
		string installDir = library.GetInstallDir(appId);
		if (installDir == null)
		{
			return new GoldbergApplyResult(0, 0, "no-install");
		}
		if (!IsAvailable())
		{
			return new GoldbergApplyResult(0, 0, "no-goldberg");
		}
		bool is64Bit = DetectMainExeBitness(installDir);
		List<string> apiDlls = FindSteamApiDlls(installDir);
		apiDlls = apiDlls.Where(d =>
		{
			bool isX64Dll = d.Contains("steam_api64.dll", StringComparison.OrdinalIgnoreCase);
			return is64Bit == isX64Dll;
		}).ToList();
		if (apiDlls.Count == 0)
		{
			return new GoldbergApplyResult(0, 0, null);
		}
		string configDir = Path.Combine(installDir, "steam_settings");
		Directory.CreateDirectory(configDir);
		File.WriteAllText(Path.Combine(configDir, "steam_appid.txt"), appId.ToString());
		File.WriteAllText(Path.Combine(configDir, "configs.user.ini"), "[user::general]\naccount_name = GabLuchi\naccount_steamid = 76561197960287930\n");
		File.WriteAllText(Path.Combine(configDir, "configs.main.ini"), "[main::connectivity]\ndisable_networking = 1\nlisten_port = 47584\noffline = 1\n");
		string interfacesFile = Path.Combine(configDir, "steam_interfaces.txt");
		bool needGenerate = !File.Exists(interfacesFile) || new FileInfo(interfacesFile).Length == 0;
		if (needGenerate)
		{
			GenerateInterfaces(apiDlls, configDir);
		}
		if (!File.Exists(interfacesFile) || new FileInfo(interfacesFile).Length == 0)
		{
			WriteFallbackInterfaces(interfacesFile);
		}
		int replaced = 0;
		int skipped = 0;
		CleanupThirdPartyCracks(installDir);
		string goldbergDll = Path.Combine(BundledGoldbergDir, "regular", is64Bit ? "x64" : "x86", is64Bit ? "steam_api64.dll" : "steam_api.dll");
		if (!File.Exists(goldbergDll))
		{
			return new GoldbergApplyResult(0, 0, "no-goldberg");
		}
		foreach (string dllPath in apiDlls)
		{
			try
			{
				string bakPath = dllPath + ".bak";
				if (File.Exists(bakPath))
				{
					skipped++;
					continue;
				}
				File.Move(dllPath, bakPath);
				File.Copy(goldbergDll, dllPath);
				string dllDir = Path.GetDirectoryName(dllPath);
				if (Directory.Exists(configDir))
				{
					CopyDirectory(configDir, Path.Combine(dllDir, "steam_settings"));
				}
				replaced++;
			}
			catch
			{
			}
		}
		return new GoldbergApplyResult(replaced, skipped, null);
	}

	private static void CleanupThirdPartyCracks(string installDir)
	{
		string[] knownCrackDlls = ["NeoCustomSteamAPI.dll", "NeoCustomSteamAPI64.dll"];
		string[] knownCrackPatterns = ["steam_emu.ini", "runerip.ini"];
		try
		{
			string[] allFiles = Directory.GetFiles(installDir, "*.*", SearchOption.AllDirectories);
			foreach (string file in allFiles)
			{
				string fileName = Path.GetFileName(file);
				string ext = Path.GetExtension(file);
				if (string.Equals(ext, ".rne", StringComparison.OrdinalIgnoreCase))
				{
					try { File.Delete(file); } catch { }
					continue;
				}
				if (knownCrackDlls.Any(n => string.Equals(fileName, n, StringComparison.OrdinalIgnoreCase)))
				{
					try { File.Delete(file); } catch { }
					continue;
				}
				if (knownCrackPatterns.Any(n => string.Equals(fileName, n, StringComparison.OrdinalIgnoreCase)))
				{
					try { File.Delete(file); } catch { }
				}
			}
		}
		catch
		{
		}
	}

	private void GenerateInterfaces(List<string> apiDlls, string configDir)
	{
		string tool = Path.Combine(ToolsDir, "generate_interfaces_x64.exe");
		if (!File.Exists(tool))
		{
			tool = Path.Combine(ToolsDir, "generate_interfaces_x86.exe");
		}
		if (!File.Exists(tool))
		{
			return;
		}
		foreach (string dllPath in apiDlls)
		{
			try
			{
				string dllDir = Path.GetDirectoryName(dllPath);
				string dllName = Path.GetFileName(dllPath);
				ProcessStartInfo psi = new ProcessStartInfo(tool, "\"" + dllName + "\"")
				{
					UseShellExecute = false,
					CreateNoWindow = true,
					WorkingDirectory = dllDir
				};
				using Process proc = Process.Start(psi);
				proc.WaitForExit(10000);
				string generated = Path.Combine(dllDir, "steam_interfaces.txt");
				string dest = Path.Combine(configDir, "steam_interfaces.txt");
				if (File.Exists(generated) && new FileInfo(generated).Length > 0)
				{
					File.Copy(generated, dest, true);
					try { File.Delete(generated); } catch { }
					return;
				}
			}
			catch
			{
			}
		}
	}

	private static void WriteFallbackInterfaces(string dest)
	{
		string content = "SteamUser023\nSteamFriends017\nSteamUtils010\nSteamMatchMaking009\nSteamMatchMakingServers002\nSteamInput006\nSteamNetworkingSockets012\nSteamNetworking006\nSteamNetworkingUtils003\nSteamRemoteStorage016\nSteamScreenshots003\nSteamHTTP003\nSteamController008\nSteamUGC020\nSteamAppList001\nSteamApps008\nSteamMusic001\nSteamMusicRemote001\nSteamVideo007\nSteamParentalSettings001\nSteamGameServerStats001\nSteamStatistics001\nSteamInventory003\nSteamTimeline002\nSteamRemotePlay002\nSteamGameCoordinator001\nSteamGameServer015\nSteamClient017\n";
		try { File.WriteAllText(dest, content); } catch { }
	}

	public static bool IsAvailable()
	{
		return File.Exists(Path.Combine(BundledGoldbergDir, "regular", "x64", "steam_api64.dll"))
			&& File.Exists(Path.Combine(BundledGoldbergDir, "regular", "x86", "steam_api.dll"));
	}

	private static readonly string[] SkipExePatterns = new string[]
	{
		"createdump", "unins", "setup", "install", "uninstall", "vcredist", "dotnet",
		"crashreport", "crashhandler", "cefprocess", "cef_process", "steamwebhelper"
	};

	private bool DetectMainExeBitness(string installDir)
	{
		try
		{
			List<string> exes = Directory.GetFiles(installDir, "*.exe", SearchOption.TopDirectoryOnly).ToList();
			if (exes.Count == 0)
			{
				return false;
			}
			string mainExe = exes.FirstOrDefault((string e) => !SkipExePatterns.Any((string s) => Path.GetFileNameWithoutExtension(e).Contains(s, StringComparison.OrdinalIgnoreCase))) ?? exes.First();
			using FileStream fs = File.OpenRead(mainExe);
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
			results.AddRange(Directory.GetFiles(installDir, "steam_api.dll", SearchOption.AllDirectories));
			results.AddRange(Directory.GetFiles(installDir, "steam_api64.dll", SearchOption.AllDirectories));
		}
		catch
		{
		}
		return results;
	}

	private static void CopyDirectory(string sourceDir, string destDir)
	{
		Directory.CreateDirectory(destDir);
		foreach (string file in Directory.GetFiles(sourceDir))
		{
			File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
		}
		foreach (string dir in Directory.GetDirectories(sourceDir))
		{
			CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
		}
	}
}
