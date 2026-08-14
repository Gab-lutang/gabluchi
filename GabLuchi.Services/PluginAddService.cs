using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GabLuchi.Models;
using GabLuchi.Resources;

namespace GabLuchi.Services;

public class PluginAddService(GabLuchiApiClient api, ManifestDownloader manifestDownloader, HubcapService hubcap, SettingsService settings, LuaInstaller installer)
{
	public class SourceRow
	{
		public string Name { get; set; } = "";

		public string DisplayName { get; set; } = "";

		public string Status { get; set; } = "";

		public bool NeedsKey { get; set; }

		public bool Locked { get; set; }

		public string? Stats { get; set; }

		public bool Downloading { get; set; }

		public double Progress { get; set; }

		public bool Indeterminate { get; set; }

		public bool Available => Status == "available";

		public bool CanDownload
		{
			get
			{
				if (Available)
				{
					return !Locked;
				}
				return false;
			}
		}
	}

	public class AddState
	{
		public long AppId;

		public string? GameName;

		public bool Checking;

		public bool FastFetch;

		public bool SourcesLoaded;

		public List<SourceRow> Sources = new List<SourceRow>();

		public string? InstallStatus;

		public bool InstallFailed;

		public string? Error;

		public bool Busy;
	}

	private const string HubcapSourceName = "Sadie (Morrenus)";

	private readonly ConcurrentDictionary<long, AddState> _states = new ConcurrentDictionary<long, AddState>();

	private static bool IsZip(string path)
	{
		try
		{
			using FileStream fileStream = File.OpenRead(path);
			Span<byte> buffer = stackalloc byte[4];
			return fileStream.Read(buffer) == 4 && buffer[0] == 80 && buffer[1] == 75 && buffer[2] == 3 && buffer[3] == 4;
		}
		catch
		{
			return false;
		}
	}

	public AddState? GetState(long appId)
	{
		if (!_states.TryGetValue(appId, out AddState value))
		{
			return null;
		}
		return value;
	}

	public void Start(long appId, string? gameName = null)
	{
		AddState state = new AddState
		{
			AppId = appId,
			Checking = true,
			FastFetch = settings.FastFetch,
			GameName = (string.IsNullOrWhiteSpace(gameName) ? null : gameName.Trim())
		};
		_states[appId] = state;
		PluginLog.Log($"PluginAdd.Start appid={appId} fastFetch={state.FastFetch}");
		Task.Run(() => CheckAsync(appId, state));
	}

	public void Pick(long appId, string sourceName)
	{
		if (!_states.TryGetValue(appId, out AddState state))
		{
			PluginLog.Log($"PluginAdd.Pick appid={appId} source='{sourceName}' -> NO STATE (Start not called?)");
			return;
		}
		if (state.Busy)
		{
			PluginLog.Log($"PluginAdd.Pick appid={appId} source='{sourceName}' -> BUSY, ignored");
			return;
		}
		SourceRow row = state.Sources.FirstOrDefault((SourceRow r) => string.Equals(r.Name, sourceName, StringComparison.OrdinalIgnoreCase) || string.Equals(r.DisplayName, sourceName, StringComparison.OrdinalIgnoreCase));
		if (row == null)
		{
			PluginLog.Log($"PluginAdd.Pick appid={appId} source='{sourceName}' -> ROW NOT FOUND. have=[{string.Join(", ", state.Sources.Select((SourceRow s) => s.Name))}]");
			return;
		}
		PluginLog.Log($"PluginAdd.Pick appid={appId} source='{row.Name}' canDownload={row.CanDownload} needsKey={row.NeedsKey} -> downloading");
		Task.Run(() => DownloadAsync(appId, state, row));
	}

	private async Task CheckAsync(long appId, AddState state)
	{
		_ = 8;
		try
		{
			Task<string?> nameTask = (string.IsNullOrEmpty(state.GameName) ? SafeGetGameNameAsync(appId) : null);
			string key = settings.HubcapApiKey;
			bool hubcapAvailable = false;
			if (!string.IsNullOrEmpty(key))
			{
				hubcapAvailable = (await hubcap.CheckStatusAsync(key, appId.ToString()))?.ManifestFileExists ?? false;
			}
			if (state.FastFetch && hubcapAvailable)
			{
				HubcapStats hubcapStats = await hubcap.GetStatsAsync(key);
				if (hubcapStats != null && hubcapStats.CanMakeRequests)
				{
					SourceMeta.Meta meta = SourceMeta.Get("Sadie (Morrenus)");
					SourceRow hubRow = new SourceRow
					{
						Name = "Sadie (Morrenus)",
						DisplayName = (meta.DisplayName ?? "Sadie (Morrenus)"),
						Status = "available",
						NeedsKey = true,
						Stats = $"{hubcapStats.DailyUsage}/{hubcapStats.DailyLimit}"
					};
					if (nameTask != null)
					{
						AddState addState = state;
						addState.GameName = await nameTask;
					}
					PublishSources(state, new List<SourceRow> { hubRow }, appId);
					await DownloadAsync(appId, state, hubRow);
					return;
				}
			}
			Dictionary<string, string> dictionary = await api.CheckSourcesAsync(appId.ToString());
			dictionary["Sadie (Morrenus)"] = (string.IsNullOrEmpty(key) ? "unknown" : (hubcapAvailable ? "available" : "unavailable"));
			List<SourceRow> rows = dictionary.OrderByDescending((KeyValuePair<string, string> kv) => SourceMeta.Get(kv.Key).RequiresUserKey ? 1 : 0).Select(delegate(KeyValuePair<string, string> kv)
			{
				SourceMeta.Meta meta2 = SourceMeta.Get(kv.Key);
				return new SourceRow
				{
					Name = kv.Key,
					DisplayName = (meta2.DisplayName ?? kv.Key),
					Status = kv.Value,
					NeedsKey = meta2.RequiresUserKey
				};
			}).ToList();
			List<SourceRow> keyRows = rows.Where((SourceRow r) => r.NeedsKey).ToList();
			if (keyRows.Count > 0 && string.IsNullOrEmpty(key))
			{
				foreach (SourceRow item in keyRows)
				{
					item.Locked = true;
				}
			}
			if (nameTask != null)
			{
				AddState addState = state;
				addState.GameName = await nameTask;
			}
			if (state.FastFetch)
			{
				if (keyRows.Count > 0 && !string.IsNullOrEmpty(key))
				{
					await FillHubcapBadgeAsync(keyRows, key);
				}
				PublishSources(state, rows, appId);
				SourceRow sourceRow = rows.FirstOrDefault((SourceRow r) => r.CanDownload);
				if (sourceRow == null)
				{
					state.Error = "No available source for this game.";
					PluginLog.Log($"PluginAdd.Check appid={appId} FastFetch: no downloadable source");
				}
				else
				{
					await DownloadAsync(appId, state, sourceRow);
				}
			}
			else
			{
				PublishSources(state, rows, appId);
				await FillBadgesAsync(rows, keyRows, key);
			}
		}
		catch (Exception ex)
		{
			state.Error = ex.Message;
			state.Checking = false;
			PluginLog.Log($"PluginAdd.Check appid={appId} EXCEPTION: {ex}");
		}
	}

	private void PublishSources(AddState state, List<SourceRow> rows, long appId)
	{
		state.Sources = rows;
		state.SourcesLoaded = true;
		state.Checking = false;
		PluginLog.Log($"PluginAdd.Check appid={appId} sources=[{string.Join(", ", rows.Select((SourceRow r) => $"{r.Name}({r.Status},lock={r.Locked})"))}] fastFetch={state.FastFetch}");
	}

	private async Task<string?> SafeGetGameNameAsync(long appId)
	{
		try
		{
			return (await api.GetDetailsAsync(appId.ToString()))?.Name;
		}
		catch
		{
			return null;
		}
	}

	private async Task FillBadgesAsync(List<SourceRow> rows, List<SourceRow> keyRows, string? key)
	{
		if (keyRows.Count > 0 && !string.IsNullOrEmpty(key))
		{
			await FillHubcapBadgeAsync(keyRows, key);
		}
	}

	private async Task FillHubcapBadgeAsync(List<SourceRow> keyRows, string key)
	{
		try
		{
			HubcapStats hubcapStats = await hubcap.GetStatsAsync(key);
			foreach (SourceRow keyRow in keyRows)
			{
				keyRow.Locked = hubcapStats == null || !hubcapStats.CanMakeRequests;
				if (hubcapStats != null)
				{
					keyRow.Stats = $"{hubcapStats.DailyUsage}/{hubcapStats.DailyLimit}";
				}
			}
		}
		catch
		{
		}
	}

	private async Task DownloadAsync(long appId, AddState state, SourceRow row)
	{
		if (state.Busy)
		{
			return;
		}
		state.Busy = true;
		state.Error = null;
		state.InstallStatus = null;
		state.InstallFailed = false;
		row.Downloading = true;
		row.Indeterminate = true;
		row.Progress = 0.0;
		try
		{
			Progress<double?> progress = new Progress<double?>(delegate(double? p)
			{
				row.Indeterminate = !p.HasValue;
				if (p.HasValue)
				{
					row.Progress = p.Value * 100.0;
				}
			});
			DownloadedFile downloadedFile = ((!row.NeedsKey) ? (await manifestDownloader.DownloadManifestAsync(appId.ToString(), row.Name, state.GameName, progress)) : (await hubcap.DownloadManifestAsync(appId.ToString(), settings.HubcapApiKey ?? "", progress)));
			DownloadedFile downloadedFile2 = downloadedFile;
			InstallResult installResult = (IsZip(downloadedFile2.FilePath) ? installer.InstallZip(downloadedFile2.FilePath, appId) : installer.InstallLua(downloadedFile2.FilePath, appId));
			try
			{
				if (File.Exists(downloadedFile2.FilePath))
				{
					File.Delete(downloadedFile2.FilePath);
				}
			}
			catch
			{
			}
			if (installResult.Error != null)
			{
				state.Error = installResult.Error;
				state.InstallFailed = true;
				PluginLog.Log($"PluginAdd.Download appid={appId} source='{row.Name}' INSTALL ERROR: {installResult.Error}");
			}
			else
			{
				string arg = (string.IsNullOrEmpty(state.GameName) ? "lua" : state.GameName);
				state.InstallStatus = ((installResult.ManifestCount > 0) ? string.Format(Strings.Add_Status_AddedManifests, arg, installResult.ManifestCount) : string.Format(Strings.Add_Status_AddedFetch, arg));
				state.InstallStatus = state.InstallStatus + " " + string.Format(Strings.Add_FastFetch_Via, row.Name);
				PluginLog.Log($"PluginAdd.Download appid={appId} source='{row.Name}' OK: {state.InstallStatus}");
			}
		}
		catch (Exception ex)
		{
			state.Error = ex.Message;
			state.InstallFailed = true;
			PluginLog.Log($"PluginAdd.Download appid={appId} source='{row.Name}' EXCEPTION: {ex}");
		}
		finally
		{
			row.Downloading = false;
			row.Indeterminate = false;
			state.Busy = false;
		}
	}
}
