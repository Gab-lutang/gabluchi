using System;
using System.Linq;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace GabLuchi.Services;

public class UpdateService
{
	private readonly UpdateManager[] _managers = AppConfig.GithubReleasesRepos.Select((string repo) => new UpdateManager(new GithubSource(repo, null, prerelease: false, new ProxiedFileDownloader()))).ToArray();

	private UpdateManager? _stagedMgr;

	private UpdateInfo? _staged;

	public bool HasStagedUpdate => _staged != null;

	public event Action? UpdateReady;

	public async Task CheckAndStageAsync()
	{
		if (_managers.Length == 0 || !_managers[0].IsInstalled)
		{
			return;
		}
		UpdateManager[] managers = _managers;
		foreach (UpdateManager mgr in managers)
		{
			try
			{
				UpdateInfo info = await mgr.CheckForUpdatesAsync();
				if (info != null)
				{
					await mgr.DownloadUpdatesAsync(info);
					_stagedMgr = mgr;
					_staged = info;
					this.UpdateReady?.Invoke();
				}
				break;
			}
			catch
			{
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
