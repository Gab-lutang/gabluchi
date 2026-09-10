using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class RestoreService(SteamLibraryService library, SteamApiCheckBypassService bypass)
{
	private static readonly string[] BypassDlls = new string[3] { "winmm.dll", "version.dll", "winhttp.dll" };

	private static readonly string[] ConfigFiles = new string[3] { "steam_interfaces.txt", "local_save.txt", "SteamAPICheckBypass.json" };

	public async Task<RestoreResult> RestoreAsync(long appId, CancellationToken ct = default(CancellationToken))
	{
		string installDir = library.GetInstallDir(appId);
		if (installDir == null)
		{
			return new RestoreResult(0, 0, "no-install");
		}
		return await Task.Run(() => RestoreDirectory(installDir), ct);
	}

	public async Task<RestoreResult> RestoreSelectedAsync(IEnumerable<int> appIds, CancellationToken ct = default(CancellationToken))
	{
		int totalRestored = 0;
		int totalDeleted = 0;
		int failed = 0;
		foreach (int appId in appIds)
		{
			ct.ThrowIfCancellationRequested();
			RestoreResult result = await RestoreAsync(appId, ct);
			if (result.Failed)
			{
				failed++;
			}
			else
			{
				totalRestored += result.Restored;
				totalDeleted += result.Deleted;
			}
		}
		if (failed > 0 && totalRestored == 0 && totalDeleted == 0)
		{
			return new RestoreResult(0, 0, "all-failed");
		}
		return new RestoreResult(totalRestored, totalDeleted, null);
	}

	private RestoreResult RestoreDirectory(string installDir)
	{
		int restored = 0;
		int deleted = 0;
		bypass.Remove(installDir);
		try
		{
			foreach (string dllName in BypassDlls)
			{
				foreach (string path in Directory.GetFiles(installDir, dllName, SearchOption.AllDirectories))
				{
					try
					{
						File.Delete(path);
						deleted++;
					}
					catch
					{
					}
				}
			}
			foreach (string fileName in ConfigFiles)
			{
				foreach (string path in Directory.GetFiles(installDir, fileName, SearchOption.AllDirectories))
				{
					try
					{
						File.Delete(path);
						deleted++;
					}
					catch
					{
					}
				}
			}
			string[] bakFiles = Directory.GetFiles(installDir, "*.bak", SearchOption.AllDirectories);
			foreach (string bak in bakFiles)
			{
				try
				{
					string original = bak[..^4];
					if (File.Exists(original))
					{
						File.Delete(original);
					}
					File.Move(bak, original);
					restored++;
				}
				catch
				{
				}
			}
			string[] settingsDirs = Directory.GetDirectories(installDir, "steam_settings", SearchOption.AllDirectories);
			foreach (string dir in settingsDirs)
			{
				try
				{
					Directory.Delete(dir, recursive: true);
					deleted++;
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return new RestoreResult(restored, deleted, null);
	}
}
