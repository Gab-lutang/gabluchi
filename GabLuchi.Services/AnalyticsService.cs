using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class AnalyticsService(SteamService steam, AuthService auth, LicenseService license, UnlockerService unlocker, SteamAppListCache appList)
{
	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(10.0)
	};

	private static readonly string Version;

	private static readonly string LogPath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"GabLuchi", "analytics-error.log");

	private string Endpoint => Config.KeyCheckerBase.TrimEnd('/') + "/api/analytics";

	private static void LogError(string method, Exception ex)
	{
		try
		{
			string dir = Path.GetDirectoryName(LogPath)!;
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {method} FAILED: {ex}\n");
		}
		catch { }
	}

	public async Task TrackAppLaunchAsync(CancellationToken ct = default)
	{
		try
		{
			string machineId = LicenseService.ComputeMachineId();
			string os = RuntimeInformation.OSDescription;
			string arch = RuntimeInformation.OSArchitecture.ToString();
			string? discordUserId = auth.IsSignedIn ? auth.UserId : null;
			string? discordTag = auth.IsSignedIn ? auth.DisplayName : null;
			string? selectedMode = unlocker.SelectedMode?.ToString();
			bool steamDetected = steam.IsValid;

			object payload = new
			{
				version = Version,
				machineId,
				os,
				arch,
				discordUserId,
				discordTag,
				selectedMode,
				steamDetected,
				timestamp = DateTimeOffset.UtcNow
			};

			await _http.PostAsJsonAsync(Endpoint + "/track", payload, ct);
		}
		catch (Exception ex)
		{
			LogError("TrackAppLaunch", ex);
		}
	}

	public async Task TrackGameFetchAsync(long appId, string gameName, string source, CancellationToken ct = default)
	{
		try
		{
			string machineId = LicenseService.ComputeMachineId();
			string? discordUserId = auth.IsSignedIn ? auth.UserId : null;
			string? discordTag = auth.IsSignedIn ? auth.DisplayName : null;

			object payload = new
			{
				machineId,
				discordUserId,
				discordTag,
				appId,
				gameName,
				source
			};

			await _http.PostAsJsonAsync(Endpoint + "/game-fetch", payload, ct);
		}
		catch (Exception ex)
		{
			LogError("TrackGameFetch", ex);
		}
	}

	public async Task TrackInstalledGamesAsync(CancellationToken ct = default)
	{
		try
		{
			string? luaDir = steam.LuaDir;
			if (luaDir == null || !Directory.Exists(luaDir)) return;

			string[] luaFiles = Directory.GetFiles(luaDir, "*.lua");
			if (luaFiles.Length == 0) return;

			string machineId = LicenseService.ComputeMachineId();
			string? discordUserId = auth.IsSignedIn ? auth.UserId : null;
			string? discordTag = auth.IsSignedIn ? auth.DisplayName : null;

			var games = new List<object>();
			foreach (string file in luaFiles)
			{
				long? appId = LuaInstaller.AppIdFromFileName(file);
				if (appId == null || appId.Value <= 0) continue;
				string? name = appList.GetName(appId.Value);
				games.Add(new { appId = appId.Value, gameName = name ?? "App " + appId.Value, source = "library" });
			}

			if (games.Count == 0) return;

			object payload = new
			{
				machineId,
				discordUserId,
				discordTag,
				games
			};

			await _http.PostAsJsonAsync(Endpoint + "/game-sync", payload, ct);
		}
		catch (Exception ex)
		{
			LogError("TrackInstalledGames", ex);
		}
	}

	static AnalyticsService()
	{
		string? text = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
		if (text != null)
		{
			int num = text.IndexOf('+');
			if (num >= 0)
			{
				Version = text.Substring(0, num);
				return;
			}
		}
		Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?";
	}
}
