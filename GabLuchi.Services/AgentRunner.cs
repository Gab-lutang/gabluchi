using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public sealed class AgentServices
{
	public AgentServices()
	{
		Settings = new SettingsService();
		License = new LicenseService(Settings);
		Steam = new SteamService(Settings);
		Cache = new CacheService();
		Lua = new LuaInstaller(Steam, Settings, Cache);
		Updates = new UpdateService();
		var gh = new GithubProxy();
		var defender = new DefenderService();
		var preCache = new ManifestPreCacheService(Steam, Settings);
		Unlocker = new UnlockerService(Steam, Settings, Cache, gh, defender, preCache);
		Auth = new AuthService();
		AppList = new SteamAppListCache();
		Analytics = new AnalyticsService(Steam, Auth, License, Unlocker, AppList);
	}

	public SettingsService Settings { get; }
	public LicenseService License { get; }
	public SteamService Steam { get; }
	public CacheService Cache { get; }
	public LuaInstaller Lua { get; }
	public UpdateService Updates { get; }
	public UnlockerService Unlocker { get; }
	public AuthService Auth { get; }
	public SteamAppListCache AppList { get; }
	public AnalyticsService Analytics { get; }
}

public static class AgentRunner
{
	private const string AgentMutexName = "GabLuchi.Agent.SingleInstance";

	private static readonly TimeSpan CyclePeriod = TimeSpan.FromMinutes(5);

	private static readonly string LogPath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"GabLuchi", "agent.log");

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

	public static void Run()
	{
		bool createdNew;
		using (new Mutex(initiallyOwned: true, AgentMutexName, out createdNew))
		{
			if (!createdNew)
			{
				Log("Another agent instance is already running — exiting.");
				return;
			}
			Log($"Agent started (pid {Environment.ProcessId}, version {Environment.Version}).");
			AgentSchedulerService.Register();
			var services = new AgentServices();
			RunLoopAsync(services).GetAwaiter().GetResult();
		}
	}

	private static async Task RunLoopAsync(AgentServices services)
	{
		while (true)
		{
			await RunCycleAsync(services);
			await Task.Delay(CyclePeriod);
		}
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

	private static async Task RunCycleAsync(AgentServices services)
	{
		try
		{
			Log("Cycle started.");
			await RunDemolishAsync(services);
			await RunUpdateAsync(services);
			await RunTelemetryAsync(services);
			Log("Cycle finished.");
		}
		catch (Exception ex)
		{
			Log("Cycle failed: " + ex.Message);
		}
	}

	private static async Task RunDemolishAsync(AgentServices services)
	{
		try
		{
			if (!services.License.IsActivated)
			{
				Log("Demolish: not activated — skipping.");
				return;
			}
			DemolishStatus? status = await services.License.CheckDemolishStatusAsync();
			if (status == null)
			{
				Log("Demolish: no status from backend.");
				return;
			}
			if (status.IsDemolished)
			{
				Log("Demolish: USER IS DEMOLISHED — deleting all game data.");
				services.Lua.DeleteAllGameFiles();
				services.Lua.DeleteDllFiles();
				services.License.Deactivate();
				Log("Demolish: full wipe done, license deactivated.");
			}
			else if (status.DemolishedApps.Length > 0)
			{
				foreach (long appId in status.DemolishedApps)
				{
					int manifests = services.Lua.DeleteManifestsForApp(appId);
					bool luaDeleted = services.Lua.DeleteLua(appId);
					Log($"Demolish: app {appId} — manifests deleted: {manifests}, lua deleted: {luaDeleted}.");
				}
			}
			await services.License.ClearDemolishedAppsAsync();
		}
		catch (Exception ex)
		{
			Log("Demolish failed: " + ex.Message);
		}
	}

	private static async Task RunUpdateAsync(AgentServices services)
	{
		try
		{
			bool guiOpen = IsGuiRunning();
			await services.Updates.CheckAndStageAsync();
			if (!services.Updates.HasStagedUpdate)
			{
				return;
			}
			if (guiOpen)
			{
				Log("Update: GUI is open — update staged, GUI applies on exit.");
			}
			else
			{
				Log("Update: staged update found and GUI is closed — applying silently.");
				services.Updates.ApplyAndRestart(new[] { "--agent" });
			}
		}
		catch (Exception ex)
		{
			Log("Update failed: " + ex.Message);
		}
	}

	private static async Task RunTelemetryAsync(AgentServices services)
	{
		try
		{
			await services.AppList.EnsureLoadedAsync();
			await services.Analytics.TrackAppLaunchAsync();
			await services.Analytics.TrackInstalledGamesAsync();
		}
		catch (Exception ex)
		{
			Log("Telemetry failed: " + ex.Message);
		}
	}
}