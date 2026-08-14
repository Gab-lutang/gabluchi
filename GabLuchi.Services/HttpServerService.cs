using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GabLuchi;
using GabLuchi.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GabLuchi.Services;

public class HttpServerService : IHostedService
{
	private readonly LuaInstaller _installer;

	private readonly SteamService _steam;

	private readonly CacheService _cache;

	private readonly IServiceProvider _services;

	private readonly ILogger<HttpServerService> _log;

	private HttpListener? _listener;

	private CancellationTokenSource? _appCts;

	private readonly ConcurrentDictionary<long, DownloadState> _downloads = new ConcurrentDictionary<long, DownloadState>();

	private List<ApiSource> _apiSources = new List<ApiSource>();

	private bool _apiSourcesLoaded;

	private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "GabLuchi", "downloads");

	private static string ManifestBackendUrl => Config.ManifestBackendBase + "/check_apis";

	public HttpServerService(LuaInstaller installer, SteamService steam, CacheService cache, IServiceProvider services, ILogger<HttpServerService> logger)
	{
		_installer = installer;
		_steam = steam;
		_cache = cache;
		_services = services;
		_log = logger;
		Directory.CreateDirectory(TempDir);
	}

	private void LoadApiSources()
	{
		if (_apiSourcesLoaded)
		{
			return;
		}
		_apiSourcesLoaded = true;
		string[] array = new string[2]
		{
			Path.Combine(AppContext.BaseDirectory, "public", "api.json"),
			Path.Combine(AppContext.BaseDirectory, "api.json")
		};
		foreach (string path in array)
		{
			if (!File.Exists(path))
			{
				continue;
			}
			try
			{
				using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(path));
				if (jsonDocument.RootElement.TryGetProperty("api_list", out var value))
				{
					_apiSources = new List<ApiSource>();
					foreach (JsonElement item in value.EnumerateArray())
					{
						JsonElement value2;
						string text = (item.TryGetProperty("name", out value2) ? (value2.GetString() ?? "") : "");
						JsonElement value3;
						string text2 = (item.TryGetProperty("url", out value3) ? (value3.GetString() ?? "") : "");
						JsonElement value4;
						int successCode = (item.TryGetProperty("success_code", out value4) ? value4.GetInt32() : 200);
						JsonElement value5;
						bool flag = !item.TryGetProperty("enabled", out value5) || value5.GetBoolean();
						if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(text2) && flag)
						{
							_apiSources.Add(new ApiSource(text, text2, successCode));
						}
					}
				}
				_log.LogInformation("Loaded {Count} API sources from api.json", _apiSources.Count);
				return;
			}
			catch (Exception ex)
			{
				_log.LogWarning("Failed to parse api.json: {Message}", ex.Message);
			}
		}
		_log.LogWarning("api.json not found — using fallback sources");
		_apiSources = new List<ApiSource>
		{
			new ApiSource("Ryuu", "http://167.235.229.108/<appid>", 200),
			new ApiSource("Sushi", "https://raw.githubusercontent.com/sushi-dev55-alt/sushitools-games-repo-alt/refs/heads/main/<appid>.zip", 200),
			new ApiSource("Luie", "http://167.235.229.108/<appid>", 200)
		};
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_appCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_listener = new HttpListener();
		_listener.Prefixes.Add("http://127.0.0.1:6767/");
		try
		{
			_listener.Start();
		}
		catch (HttpListenerException)
		{
			_log.LogWarning("HttpListener could not start on 127.0.0.1:6767 — attempting netsh reservation");
			try
			{
				Process.Start(new ProcessStartInfo("netsh", "http add urlacl url=http://127.0.0.1:6767/ user=Everyone")
				{
					UseShellExecute = false,
					CreateNoWindow = true
				})?.WaitForExit(3000);
				_listener.Start();
			}
			catch (Exception exception)
			{
				_log.LogError(exception, "Failed to start HTTP server on :6767");
				return Task.CompletedTask;
			}
		}
		_log.LogInformation("HTTP server listening on http://127.0.0.1:6767");
		Task.Run(() => ListenLoop(_appCts.Token), _appCts.Token);
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_appCts?.Cancel();
		try
		{
			_listener?.Stop();
		}
		catch
		{
		}
		return Task.CompletedTask;
	}

	private async Task ListenLoop(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested && (_listener?.IsListening ?? false))
		{
			try
			{
				HttpListenerContext ctx = await _listener.GetContextAsync().WaitAsync(ct);
				Task.Run(() => HandleRequest(ctx), ct);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (HttpListenerException)
			{
				break;
			}
			catch
			{
			}
		}
	}

	private async Task HandleRequest(HttpListenerContext ctx)
	{
		HttpListenerRequest request = ctx.Request;
		HttpListenerResponse resp = ctx.Response;
		SetCors(resp);
		resp.ContentType = "application/json; charset=utf-8";
		try
		{
			string text = request.Url?.AbsolutePath.TrimEnd('/');
			if (text != null && !text.StartsWith("/add-status/") && !text.StartsWith("/has/"))
			{
				PluginLog.Log("HTTP " + request.HttpMethod + " " + text);
			}
			string text2 = text;
			(int, string) tuple;
			string id;
			string id2;
			string id3;
			string id4;
			string id5;
			string id6;
			string id7;
			string id8;
			string id9;
			string id10;
			if (request.HttpMethod == "OPTIONS")
			{
				tuple = (204, "");
			}
			else if (MatchGet(text2, "/has/{appid}", out id))
			{
				tuple = await HandleHas(long.Parse(id));
			}
			else if (MatchPost(text2, "/add/{appid}", out id2))
			{
				tuple = await HandleAdd(long.Parse(id2), request);
			}
			else if (MatchGet(text2, "/add-status/{appid}", out id3))
			{
				tuple = HandleAddStatus(long.Parse(id3));
			}
			else if (MatchPost(text2, "/add-source/{appid}", out id4))
			{
				tuple = await HandleAddSource(long.Parse(id4), request);
			}
			else if (MatchPost(text2, "/check-sources/{appid}", out id5))
			{
				tuple = await HandleCheckSources(long.Parse(id5));
			}
			else if (MatchPost(text2, "/download/{appid}", out id6))
			{
				tuple = await HandleDownload(long.Parse(id6), request);
			}
			else if (MatchGet(text2, "/download-status/{appid}", out id7))
			{
				tuple = HandleStatus(long.Parse(id7));
			}
			else if (MatchPost(text2, "/cancel/{appid}", out id8))
			{
				tuple = HandleCancel(long.Parse(id8));
			}
			else if (MatchPost(text2, "/remove/{appid}", out id9))
			{
				tuple = HandleRemove(long.Parse(id9));
			}
			else if (MatchFixGet(text2, "/fix/{appid}/{slot}", out var fixAppId, out var fixSlot))
			{
				tuple = await HandleFixDownload(fixAppId, fixSlot);
			}
			else if (!MatchPost(text2, "/open/fix/{appid}", out id10))
			{
				if (text2 == null)
				{
					goto IL_0744;
				}
				int length = text2.Length;
				if (length <= 9)
				{
					if (length != 5)
					{
						if (length != 9)
						{
							goto IL_0744;
						}
						char c = text2[1];
						if (c != 'a')
						{
							if (c != 'o' || !(text2 == "/open-url") || !(request.HttpMethod == "POST"))
							{
								goto IL_0744;
							}
							tuple = await HandleOpenUrl(request);
						}
						else
						{
							if (!(text2 == "/api-list") || !(request.HttpMethod == "GET"))
							{
								goto IL_0744;
							}
							tuple = HandleApiList();
						}
					}
					else
					{
						if (!(text2 == "/icon") || !(request.HttpMethod == "GET"))
						{
							goto IL_0744;
						}
						tuple = HandleIcon();
					}
				}
				else if (length != 12)
				{
					if (length != 14)
					{
						goto IL_0744;
					}
					char c = text2[1];
					if (c != 'c')
					{
						if (c != 'o')
						{
							if (c != 'r' || !(text2 == "/restart-steam") || !(request.HttpMethod == "POST"))
							{
								goto IL_0744;
							}
							tuple = HandleRestartSteam();
						}
						else
						{
							if (!(text2 == "/open/settings") || !(request.HttpMethod == "POST"))
							{
								goto IL_0744;
							}
							tuple = HandleOpenSettings();
						}
					}
					else
					{
						if (!(text2 == "/check-updates") || !(request.HttpMethod == "POST"))
						{
							goto IL_0744;
						}
						tuple = await HandleCheckUpdates();
					}
				}
				else
				{
					if (!(text2 == "/loaded-apps"))
					{
						goto IL_0744;
					}
					if (request.HttpMethod == "GET")
					{
						tuple = await HandleReadLoadedApps();
					}
					else
					{
						if (!(request.HttpMethod == "POST"))
						{
							goto IL_0744;
						}
						tuple = HandleDismissLoadedApps();
					}
				}
			}
			else
			{
				tuple = HandleOpenFix(long.Parse(id10));
			}
			goto IL_075a;
			IL_0744:
			tuple = (404, JsonErr("Not found"));
			goto IL_075a;
			IL_075a:
			(int, string) tuple2 = tuple;
			int item = tuple2.Item1;
			string item2 = tuple2.Item2;
			resp.StatusCode = item;
			byte[] bytes = Encoding.UTF8.GetBytes(item2);
			await resp.OutputStream.WriteAsync(bytes);
		}
		catch (Exception ex)
		{
			resp.StatusCode = 500;
			byte[] bytes2 = Encoding.UTF8.GetBytes(JsonErr(ex.Message));
			await resp.OutputStream.WriteAsync(bytes2);
		}
		finally
		{
			resp.Close();
		}
	}

	private static bool MatchGet(string? path, string pattern, out string id)
	{
		id = "";
		if (path == null)
		{
			return false;
		}
		string[] array = pattern.TrimEnd('/').Split('/');
		string[] array2 = path.Split('/');
		if (array.Length != array2.Length)
		{
			return false;
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].StartsWith("{"))
			{
				id = array2[i];
			}
			else if (!string.Equals(array[i], array2[i], StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		return !string.IsNullOrEmpty(id);
	}

	private static bool MatchPost(string? path, string pattern, out string id)
	{
		return MatchGet(path, pattern, out id);
	}

	private static bool MatchFixGet(string? path, string pattern, out string appid, out string slot)
	{
		appid = "";
		slot = "";
		if (path == null)
		{
			return false;
		}
		string[] array = pattern.TrimEnd('/').Split('/');
		string[] array2 = path.Split('/');
		if (array.Length != array2.Length)
		{
			return false;
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == "{appid}")
			{
				appid = array2[i];
			}
			else if (array[i] == "{slot}")
			{
				slot = array2[i];
			}
			else if (!string.Equals(array[i], array2[i], StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		return !string.IsNullOrEmpty(appid) && !string.IsNullOrEmpty(slot);
	}

	private Task<(int, string)> HandleHas(long appId)
	{
		bool exists = _installer.ReadInstalledLua(appId) != null;
		return Task.FromResult((200, Json(new
		{
			success = true,
			exists = exists
		})));
	}

	private async Task<(int, string)> HandleAdd(long appId, HttpListenerRequest req)
	{
		string name = null;
		try
		{
			using StreamReader reader = new StreamReader(req.InputStream, req.ContentEncoding);
			JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(await reader.ReadToEndAsync());
			if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty("name", out var value))
			{
				name = value.GetString();
			}
		}
		catch
		{
		}
		_services.GetRequiredService<PluginAddService>().Start(appId, name);
		return (200, Json(new
		{
			success = true
		}));
	}

	private (int, string) HandleAddStatus(long appId)
	{
		PluginAddService.AddState state = _services.GetRequiredService<PluginAddService>().GetState(appId);
		bool installed = _installer.ReadInstalledLua(appId) != null;
		if (state == null)
		{
			return (200, Json(new
			{
				success = true,
				checking = false,
				sourcesLoaded = false,
				sources = Array.Empty<object>(),
				installed = installed
			}));
		}
		List<object> sources = ((IEnumerable<PluginAddService.SourceRow>)state.Sources).Select((Func<PluginAddService.SourceRow, object>)((PluginAddService.SourceRow s) => new
		{
			name = s.Name,
			displayName = s.DisplayName,
			status = s.Status,
			available = s.Available,
			canDownload = s.CanDownload,
			locked = s.Locked,
			needsKey = s.NeedsKey,
			stats = s.Stats,
			downloading = s.Downloading,
			progress = s.Progress,
			indeterminate = s.Indeterminate
		})).ToList();
		return (200, Json(new
		{
			success = true,
			appid = state.AppId,
			checking = state.Checking,
			fastFetch = state.FastFetch,
			sourcesLoaded = state.SourcesLoaded,
			sources = sources,
			installStatus = state.InstallStatus,
			installFailed = state.InstallFailed,
			error = state.Error,
			installed = installed
		}));
	}

	private async Task<(int, string)> HandleAddSource(long appId, HttpListenerRequest req)
	{
		string text;
		using (StreamReader reader = new StreamReader(req.InputStream, req.ContentEncoding))
		{
			text = await reader.ReadToEndAsync();
		}
		string text2 = "";
		try
		{
			JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(text);
			if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty("source", out var value))
			{
				text2 = value.GetString() ?? "";
			}
		}
		catch
		{
		}
		PluginLog.Log($"/add-source/{appId} body='{text}' parsed source='{text2}'");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return (400, JsonErr("source is required"));
		}
		_services.GetRequiredService<PluginAddService>().Pick(appId, text2);
		return (200, Json(new
		{
			success = true
		}));
	}

	private async Task<(int, string)> HandleCheckSources(long appId)
	{
		try
		{
			List<object> results = ((IEnumerable<KeyValuePair<string, string>>)(await _services.GetRequiredService<GabLuchiApiClient>().CheckSourcesAsync(appId.ToString()))).Select((Func<KeyValuePair<string, string>, object>)((KeyValuePair<string, string> kv) => new
			{
				name = kv.Key,
				available = (kv.Value == "available"),
				url = (string)null
			})).ToList();
			return (200, Json(new
			{
				success = true,
				results = results
			}));
		}
		catch (Exception ex)
		{
			return (200, Json(new
			{
				success = false,
				error = ex.Message,
				results = Array.Empty<object>()
			}));
		}
	}

	private async Task<(int, string)> HandleDownload(long appId, HttpListenerRequest req)
	{
		string json;
		using (StreamReader reader = new StreamReader(req.InputStream, req.ContentEncoding))
		{
			json = await reader.ReadToEndAsync();
		}
		JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
		JsonElement value;
		JsonElement value2;
		string text = (jsonElement.TryGetProperty("source", out value) ? (value.GetString() ?? "") : (jsonElement.TryGetProperty("apiName", out value2) ? (value2.GetString() ?? "") : ""));
		if (string.IsNullOrWhiteSpace(text))
		{
			return (400, JsonErr("source is required"));
		}
		DownloadState value3;
		bool flag = _downloads.TryGetValue(appId, out value3);
		if (flag)
		{
			string status = value3.Status;
			bool flag2 = ((status == "downloading" || status == "processing") ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return (409, JsonErr("Download already in progress for this app"));
		}
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		DownloadState value4 = new DownloadState
		{
			Status = "queued",
			CurrentApi = text,
			Cts = cancellationTokenSource
		};
		_downloads[appId] = value4;
		DownloadAndInstallAsync(appId, text, cancellationTokenSource.Token);
		return (200, Json(new
		{
			success = true
		}));
	}

	private (int, string) HandleStatus(long appId)
	{
		if (!_downloads.TryGetValue(appId, out DownloadState value))
		{
			return (200, Json(new
			{
				success = true,
				state = (object)null
			}));
		}
		var state = new
		{
			status = value.Status,
			bytesRead = value.BytesRead,
			totalBytes = value.TotalBytes,
			currentApi = value.CurrentApi,
			apiErrors = ((value.ApiErrors.Count > 0) ? value.ApiErrors : null),
			error = value.Error,
			installedPath = value.InstalledPath,
			success = value.Success,
			api = value.Api
		};
		return (200, Json(new
		{
			success = true,
			state = state
		}));
	}

	private (int, string) HandleCancel(long appId)
	{
		DownloadState value;
		bool flag = _downloads.TryGetValue(appId, out value);
		if (flag)
		{
			bool flag2;
			switch (value.Status)
			{
			case "queued":
			case "downloading":
			case "processing":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		if (flag)
		{
			value.Cts?.Cancel();
			value.Status = "cancelled";
			value.Error = "Cancelled by user";
			_downloads[appId] = value;
			return (200, Json(new
			{
				success = true
			}));
		}
		return (200, Json(new
		{
			success = true,
			message = "Nothing to cancel"
		}));
	}

	private (int, string) HandleRemove(long appId)
	{
		try
		{
			_cache.RemoveLoadedAppId(appId);
			string text = _installer.ReadInstalledLua(appId);
			if (text != null)
			{
				File.Delete(text);
				string path = Path.Combine(Path.GetDirectoryName(text), $"{appId}.lua.disabled");
				if (File.Exists(path))
				{
					File.Delete(path);
				}
				return (200, Json(new
				{
					success = true,
					deleted = new string[1] { text },
					count = 1
				}));
			}
			return (200, Json(new
			{
				success = true,
				deleted = Array.Empty<string>(),
				count = 0
			}));
		}
		catch (Exception ex)
		{
			return (500, JsonErr(ex.Message));
		}
	}

	private (int, string) HandleOpenFix(long appId)
	{
		return OnUiThread(delegate
		{
			MainWindow requiredService = _services.GetRequiredService<MainWindow>();
			FixesViewModel requiredService2 = _services.GetRequiredService<FixesViewModel>();
			requiredService.RestoreFromTray();
			requiredService.NavigateToFixes();
			requiredService2.OpenForAppIdAsync(appId);
		});
	}

	private async Task<(int, string)> HandleFixDownload(string appid, string slot)
	{
		string? path = FixRepository.Resolve(appid, slot);
		if (path == null || !File.Exists(path))
		{
			return (404, JsonErr("Fix not found."));
		}
		try
		{
			byte[] bytes = await File.ReadAllBytesAsync(path);
			return (200, Json(new
			{
				success = true,
				fileName = Path.GetFileName(path),
				data = Convert.ToBase64String(bytes)
			}));
		}
		catch (Exception ex)
		{
			return (500, JsonErr(ex.Message));
		}
	}

	private (int, string) HandleOpenSettings()
	{
		return OnUiThread(delegate
		{
			MainWindow requiredService = _services.GetRequiredService<MainWindow>();
			requiredService.RestoreFromTray();
			requiredService.NavigateToSettings();
		});
	}

	private (int, string) HandleRestartSteam()
	{
		bool flag = _steam.RestartSteam();
		return (200, Json(flag ? new
		{
			success = true,
			error = (string)null
		} : new
		{
			success = false,
			error = "Failed to restart Steam"
		}));
	}

	private async Task<(int, string)> HandleOpenUrl(HttpListenerRequest req)
	{
		string json;
		using (StreamReader reader = new StreamReader(req.InputStream, req.ContentEncoding))
		{
			json = await reader.ReadToEndAsync();
		}
		string text = "";
		try
		{
			JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
			if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty("url", out var value))
			{
				text = value.GetString() ?? "";
			}
		}
		catch
		{
		}
		if (!text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			return (400, JsonErr("Invalid URL"));
		}
		try
		{
			Process.Start(new ProcessStartInfo(text)
			{
				UseShellExecute = true
			});
			return (200, Json(new
			{
				success = true
			}));
		}
		catch (Exception ex)
		{
			return (500, JsonErr(ex.Message));
		}
	}

	private Task<(int, string)> HandleCheckUpdates()
	{
		try
		{
			Func<Task> runUpdateFlow = App.RunUpdateFlow;
			if (runUpdateFlow != null)
			{
				runUpdateFlow();
			}
			else
			{
				_services.GetRequiredService<UpdateService>().CheckAndStageAsync();
				_services.GetRequiredService<PluginInstallerService>().AutoUpdateAsync();
			}
			return Task.FromResult((200, Json(new
			{
				success = true
			})));
		}
		catch (Exception ex)
		{
			return Task.FromResult((200, Json(new
			{
				success = false,
				error = ex.Message
			})));
		}
	}

	private async Task<(int, string)> HandleReadLoadedApps()
	{
		IReadOnlyList<long> ids = _cache.GetLoadedAppIds();
		SteamAppListCache names = _services.GetRequiredService<SteamAppListCache>();
		try
		{
			await names.EnsureLoadedAsync();
		}
		catch
		{
		}
		var apps = ids.Select((long id) => new
		{
			appid = id,
			name = names.GetName(id)
		}).ToList();
		return (200, Json(new
		{
			success = true,
			apps = apps
		}));
	}

	private (int, string) HandleDismissLoadedApps()
	{
		_cache.ClearLoadedAppIds();
		return (200, Json(new
		{
			success = true
		}));
	}

	private (int, string) OnUiThread(Action action)
	{
		Application current = Application.Current;
		Dispatcher val = ((current != null) ? ((DispatcherObject)current).Dispatcher : null);
		if (val == null)
		{
			return (500, JsonErr("App not ready"));
		}
		val.InvokeAsync((Action)delegate
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				_log.LogWarning("UI action failed: {Message}", ex.Message);
			}
		});
		return (200, Json(new
		{
			success = true
		}));
	}

	private (int, string) HandleApiList()
	{
		LoadApiSources();
		var apis = _apiSources.Select((ApiSource s, int i) => new
		{
			name = s.Name,
			index = i
		}).ToList();
		return (200, Json(new
		{
			success = true,
			apis = apis
		}));
	}

	private (int, string) HandleIcon()
	{
		try
		{
			string text = Path.Combine(AppContext.BaseDirectory, "gabluchi-icon.png");
			if (!File.Exists(text))
			{
				string text2 = Path.Combine(AppContext.BaseDirectory, "icon.ico");
				if (!File.Exists(text2))
				{
					return (200, Json(new
					{
						success = false,
						dataUrl = ""
					}));
				}
				text = text2;
			}
			string text3 = Convert.ToBase64String(File.ReadAllBytes(text));
			string text4 = (text.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/x-icon");
			return (200, Json(new
			{
				success = true,
				dataUrl = "data:" + text4 + ";base64," + text3
			}));
		}
		catch
		{
			return (200, Json(new
			{
				success = false,
				dataUrl = ""
			}));
		}
	}

	private async Task DownloadAndInstallAsync(long appId, string source, CancellationToken ct)
	{
		DownloadState state = _downloads[appId];
		try
		{
			state.Status = "downloading";
			state.BytesRead = 0L;
			state.TotalBytes = 100L;
			DownloadedFile downloadedFile = await _services.GetRequiredService<ManifestDownloader>().DownloadManifestAsync(appId.ToString(), source, null, new Progress<double?>(delegate(double? p)
			{
				if (p.HasValue)
				{
					state.TotalBytes = 100L;
					state.BytesRead = (long)(p.Value * 100.0);
				}
			}), ct);
			state.Status = "processing";
			InstallResult installResult = _installer.InstallZip(downloadedFile.FilePath, appId);
			try
			{
				if (File.Exists(downloadedFile.FilePath))
				{
					File.Delete(downloadedFile.FilePath);
				}
			}
			catch
			{
			}
			if (installResult.Error != null)
			{
				state.Status = "failed";
				state.Error = installResult.Error;
			}
			else
			{
				state.Status = "done";
				state.Success = true;
				state.Api = source;
			}
		}
		catch (OperationCanceledException)
		{
			state.Status = "cancelled";
			state.Error = "Cancelled by user";
		}
		catch (Exception ex2)
		{
			state.Status = "failed";
			state.Error = ex2.Message;
		}
		finally
		{
			state.Cts?.Dispose();
			state.Cts = null;
		}
	}

	private static void SetCors(HttpListenerResponse resp)
	{
		resp.AddHeader("Access-Control-Allow-Origin", "*");
		resp.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
		resp.AddHeader("Access-Control-Allow-Headers", "Content-Type");
	}

	private static string Json(object obj)
	{
		return JsonSerializer.Serialize(obj);
	}

	private static string JsonErr(string msg)
	{
		return JsonSerializer.Serialize(new
		{
			success = false,
			error = msg
		});
	}
}
