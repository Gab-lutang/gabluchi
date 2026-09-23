using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Services;

namespace GabLuchiUpdater;

public static class Program
{
	private const string UpdaterMutexName = "GabLuchi.Updater.SingleInstance";

	private static readonly string LogPath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"GabLuchi", "updater.log");

	private static void Log(string msg)
	{
		try
		{
			string dir = Path.GetDirectoryName(LogPath)!;
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}\n");
		}
		catch { }
	}

	private static bool HasArg(string[] args, string name)
	{
		return args != null && args.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
	}

	private static bool IsGuiRunning()
	{
		try
		{
			int self = Environment.ProcessId;
			return Process.GetProcessesByName("GabLuchi").Any(p => p.Id != self);
		}
		catch
		{
			return false;
		}
	}

	private static async Task RunUpdateCheckAsync()
	{
		try
		{
			var updates = new UpdateService();
			Log("Checking for updates...");
			await updates.CheckAndStageAsync();
			if (!updates.HasStagedUpdate)
			{
				Log("No update staged.");
				return;
			}
			if (IsGuiRunning())
			{
				Log("GUI is open — update staged, GUI applies on exit.");
				return;
			}
			Log("GUI is closed — applying update silently.");
			updates.ApplyAndRestart(new[] { "--ua", "/installsource", "scheduler" });
		}
		catch (Exception ex)
		{
			Log("Update check failed: " + ex.Message);
		}
	}

	[STAThread]
	public static void Main(string[] args)
	{
		bool coreMode = HasArg(args, "/core");
		bool uaMode = HasArg(args, "/ua");

		if (!coreMode && !uaMode)
		{
			return;
		}

		if (uaMode)
		{
			bool createdNew;
			using (new Mutex(initiallyOwned: true, UpdaterMutexName, out createdNew))
			{
				if (!createdNew)
				{
					Log("Another updater instance is already running — exiting.");
					return;
				}
				UpdaterSchedulerService.Register();
				RunUpdateCheckAsync().GetAwaiter().GetResult();
			}
			return;
		}

		try
		{
			UpdaterSchedulerService.Register();
			Log("Core: scheduled tasks registered/verified.");
		}
		catch (Exception ex)
		{
			Log("Core run failed: " + ex.Message);
		}
	}
}