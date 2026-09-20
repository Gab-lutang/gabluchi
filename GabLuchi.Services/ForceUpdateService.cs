using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace GabLuchi.Services;

public class ForceUpdateService : IHostedService
{
	private readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
	private Timer? _timer;

	private static readonly string LogPath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"GabLuchi", "force-update.log");

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

	private string Endpoint => Config.KeyCheckerBase.TrimEnd('/') + "/api/update-check";

	public Task StartAsync(CancellationToken cancellationToken)
	{
		Log("ForceUpdateService started, polling every 5 minutes");
		_timer = new Timer(async _ => await CheckAsync(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_timer?.Dispose();
		_timer = null;
		Log("ForceUpdateService stopped");
		return Task.CompletedTask;
	}

	private async Task CheckAsync()
	{
		try
		{
			string response = await _http.GetStringAsync(Endpoint);
			using JsonDocument doc = JsonDocument.Parse(response);
			bool forceUpdate = doc.RootElement.TryGetProperty("forceUpdate", out JsonElement el) && el.GetBoolean();
			if (forceUpdate)
			{
				Log("Force update detected — triggering update flow");
				if (App.RunUpdateFlow != null)
				{
					await App.RunUpdateFlow();
				}
			}
		}
		catch (Exception ex)
		{
			Log("Poll check failed: " + ex.Message);
		}
	}
}
