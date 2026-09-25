using System;
using System.Collections.Generic;
using System.IO;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class UplayR1Unlocker : DlcUnlockerBase
{
	private static readonly string BundledDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dlc_unlockers", "uplayr1");

	public override DlcUnlockerType Type => DlcUnlockerType.UplayR1;

	public override string DisplayName => "UplayR1 (Older Ubisoft)";

	public override string[] ConfigFileNames => Array.Empty<string>();

	public override DlcUnlockerType[] ConflictsWith => Array.Empty<DlcUnlockerType>();

	public override bool IsAvailable() => Directory.Exists(BundledDir);

	public override bool IsInstalled(string gameDir)
	{
		return File.Exists(Path.Combine(gameDir, "uplay_r1_loader.dll"))
			|| File.Exists(Path.Combine(gameDir, "uplay_r1_loader64.dll"));
	}

	public override DlcUnlockerInstallResult Install(string gameDir, List<long> dlcIds, long appId)
	{
		if (!Directory.Exists(gameDir))
		{
			return new DlcUnlockerInstallResult(false, "Game directory not found.", 0);
		}
		if (!Directory.Exists(BundledDir))
		{
			return new DlcUnlockerInstallResult(false, "UplayR1 DLLs not found in installation.", 0);
		}
		try
		{
			int installed = 0;
			string dll32 = Path.Combine(BundledDir, "uplay_r1_loader.dll");
			string dll64 = Path.Combine(BundledDir, "uplay_r1_loader64.dll");
			if (File.Exists(dll32))
			{
				File.Copy(dll32, Path.Combine(gameDir, "uplay_r1_loader.dll"), overwrite: true);
				installed++;
			}
			if (File.Exists(dll64))
			{
				File.Copy(dll64, Path.Combine(gameDir, "uplay_r1_loader64.dll"), overwrite: true);
				installed++;
			}
			if (installed == 0)
			{
				return new DlcUnlockerInstallResult(false, "No UplayR1 DLLs found to install.", 0);
			}
			return new DlcUnlockerInstallResult(true, null, dlcIds.Count);
		}
		catch (Exception ex)
		{
			return new DlcUnlockerInstallResult(false, ex.Message, 0);
		}
	}

	public override bool Uninstall(string gameDir)
	{
		try
		{
			string f1 = Path.Combine(gameDir, "uplay_r1_loader.dll");
			string f2 = Path.Combine(gameDir, "uplay_r1_loader64.dll");
			if (File.Exists(f1)) File.Delete(f1);
			if (File.Exists(f2)) File.Delete(f2);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
