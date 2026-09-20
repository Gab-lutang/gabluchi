using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class AnalyticsService(SteamService steam, AuthService auth, LicenseService license, UnlockerService unlocker)
{
	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(10.0)
	};

	private static readonly string Version;

	private string Endpoint => Config.KeyCheckerBase.TrimEnd('/') + "/api/analytics";

	public async Task TrackAppLaunchAsync(CancellationToken ct = default)
	{
		try
		{
			string machineId = LicenseService.ComputeMachineId();
			string os = RuntimeInformation.OSDescription;
			string arch = RuntimeInformation.OSArchitecture.ToString();
			string? discordUserId = auth.IsSignedIn ? auth.UserId : null;
			string? selectedMode = unlocker.SelectedMode?.ToString();
			bool steamDetected = steam.IsValid;

			object payload = new
			{
				version = Version,
				machineId,
				os,
				arch,
				discordUserId,
				selectedMode,
				steamDetected,
				timestamp = DateTimeOffset.UtcNow
			};

			await _http.PostAsJsonAsync(Endpoint + "/track", payload, ct);
		}
		catch
		{
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
		catch
		{
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
