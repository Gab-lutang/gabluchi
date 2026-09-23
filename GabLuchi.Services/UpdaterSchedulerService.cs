using System;
using System.Diagnostics;
using System.IO;

namespace GabLuchi.Services;

public static class UpdaterSchedulerService
{
	public const string CoreTaskName = "GabLuchiUpdateCore";

	public const string UaTaskName = "GabLuchiUpdateUA";

	private static string? ResolveUpdaterPath()
	{
		try
		{
			string? exePath = Environment.ProcessPath;
			if (string.IsNullOrWhiteSpace(exePath))
			{
				return null;
			}
			string dir = Path.GetDirectoryName(exePath)!;
			string candidate = Path.Combine(dir, "GabLuchiUpdater.exe");
			return File.Exists(candidate) ? candidate : null;
		}
		catch
		{
			return null;
		}
	}

	public static void Register()
	{
		try
		{
			string? updater = ResolveUpdaterPath();
			if (updater == null)
			{
				return;
			}
			RunSchtasks("/Create /TN \"" + CoreTaskName + "\" /TR \"\\\"" + updater + "\\\" /core\" /SC ONLOGON /RL HIGHEST /F");
			RunSchtasks("/Create /TN \"" + UaTaskName + "\" /TR \"\\\"" + updater + "\\\" /ua /installsource scheduler\" /SC HOURLY /MO 1 /RL HIGHEST /F");
		}
		catch
		{
		}
	}

	public static void RunNow(string taskName)
	{
		try
		{
			RunSchtasks("/Run /TN \"" + taskName + "\"");
		}
		catch
		{
		}
	}

	public static void Unregister()
	{
		foreach (string name in new string[2] { CoreTaskName, UaTaskName })
		{
			try
			{
				RunSchtasks("/Delete /TN \"" + name + "\" /F");
			}
			catch
			{
			}
		}
	}

	private static void RunSchtasks(string arguments)
	{
		using Process? p = Process.Start(new ProcessStartInfo
		{
			FileName = "schtasks.exe",
			Arguments = arguments,
			CreateNoWindow = true,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		});
		p?.WaitForExit(15000);
	}
}