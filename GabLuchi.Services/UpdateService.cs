using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace GabLuchi.Services;

public class UpdateService
{
	private readonly UpdateManager[] _managers = AppConfig.GithubReleasesRepos.Select((string repo) => new UpdateManager(new GithubSource(repo, AppConfig.GithubToken, prerelease: true, new ProxiedFileDownloader()))).ToArray();

	private readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

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

	public string? BackendLatestVersion { get; private set; }

	public bool BackendSaysUpdateAvailable =>
		BackendLatestVersion != null &&
		InstalledVersion != "unknown" &&
		Version.TryParse(InstalledVersion, out var installed) &&
		Version.TryParse(BackendLatestVersion, out var latest) &&
		latest > installed;

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

		bool velopackFound = false;
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
					velopackFound = true;
					this.UpdateReady?.Invoke();
				}
				else
				{
					Log("Velopack: no update available");
				}
				break;
			}
			catch (Exception ex)
			{
				Log("Velopack check failed: " + ex.Message);
			}
		}

		if (!velopackFound)
		{
			try
			{
				Log("Checking backend for latest version...");
				string endpoint = Config.KeyCheckerBase.TrimEnd('/') + "/api/update-check";
				string json = await _http.GetStringAsync(endpoint);
				using JsonDocument doc = JsonDocument.Parse(json);
				if (doc.RootElement.TryGetProperty("latestVersion", out JsonElement verEl) && verEl.ValueKind == JsonValueKind.String)
				{
					BackendLatestVersion = verEl.GetString();
					Log("Backend reports latest version: " + BackendLatestVersion);
					if (BackendSaysUpdateAvailable)
					{
						Log("Backend says update available: v" + BackendLatestVersion + " (installed: " + InstalledVersion + ")");
					}
				}
				else
				{
					Log("Backend response missing latestVersion");
				}
			}
			catch (Exception ex)
			{
				Log("Backend fallback check failed: " + ex.Message);
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
