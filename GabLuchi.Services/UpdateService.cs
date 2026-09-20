using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace GabLuchi.Services;

public class UpdateService
{
	private readonly UpdateManager[] _managers = AppConfig.GithubReleasesRepos.Select((string repo) => new UpdateManager(new GithubSource(repo, AppConfig.GithubToken, prerelease: true, new ProxiedFileDownloader()))).ToArray();

	private UpdateManager? _stagedMgr;

	private UpdateInfo? _staged;

	private static readonly string LogPath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"GabLuchi", "update.log");

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

	public bool HasStagedUpdate => _staged != null;

	public event Action? UpdateReady;

	public string InstalledVersion { get; private set; } = "unknown";

	public async Task CheckAndStageAsync()
	{
		Log("Update check started");
		if (_managers.Length == 0 || !_managers[0].IsInstalled)
		{
			Log("Update skipped: IsInstalled=" + (_managers.Length > 0 ? _managers[0].IsInstalled.ToString() : "no managers") + " — app was not installed via Velopack Setup.exe");
			return;
		}
		try
		{
			InstalledVersion = _managers[0].CurrentVersion?.ToString() ?? "unknown";
			Log("Current installed version: " + InstalledVersion);
		}
		catch (Exception ex) { Log("Could not read current version: " + ex.Message); }
		UpdateManager[] managers = _managers;
		foreach (UpdateManager mgr in managers)
		{
			try
			{
				Log("Checking for updates from GitHub...");
				UpdateInfo info = await mgr.CheckForUpdatesAsync();
				if (info != null)
				{
				Log("Update found: " + info.TargetFullRelease.Version + " — downloading...");
				await mgr.DownloadUpdatesAsync(info);
				_stagedMgr = mgr;
				_staged = info;
				Log("Update staged: " + info.TargetFullRelease.Version);
					this.UpdateReady?.Invoke();
				}
				else
				{
					Log("No update available");
				}
				break;
			}
			catch (Exception ex)
			{
				Log("Update check failed: " + ex);
			}
		}
	}

	public void ApplyAndRestart(string[]? restartArgs = null)
	{
		if (_stagedMgr != null && _staged != null)
		{
			_stagedMgr.ApplyUpdatesAndRestart(_staged, restartArgs);
		}
	}

	public void ApplyOnExit()
	{
		if (_stagedMgr != null && _staged != null)
		{
			_stagedMgr.WaitExitThenApplyUpdates(_staged, silent: true, restart: false);
		}
	}
}
