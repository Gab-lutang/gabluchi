using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GabLuchi.Services;

public class CefInjectorService : IHostedService
{
	private readonly SteamService _steam;

	private readonly ILogger<CefInjectorService> _log;

	private CancellationTokenSource? _cts;

	private string _gabluchiJs = "";

	private string _polyfillJs = "";

	private bool _forceReload;

	private string _loadedFingerprint = "";

	private string _persistedFingerprint = "";

	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(30.0)
	};

	private readonly Dictionary<string, ClientWebSocket> _sockets = new Dictionary<string, ClientWebSocket>();

	private int _cdpId;

	private const string CefDebugUrl = "http://127.0.0.1:8080/json";

	private const string BaseUrl = "http://127.0.0.1:6767";

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	private const int TickMs = 150;

	private const int InjectEveryTicks = 7;

	public CefInjectorService(SteamService steam, ILogger<CefInjectorService> logger)
	{
		_steam = steam;
		_log = logger;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		try
		{
			string path = GetFingerprintStorePath();
			if (File.Exists(path))
			{
				_persistedFingerprint = (await File.ReadAllTextAsync(path, cancellationToken)).Trim();
			}
		}
		catch (Exception ex)
		{
			_log.LogDebug("CEF: failed to read persisted fingerprint: {Message}", ex.Message);
		}
		await ReloadPluginFilesAsync();
		Task.Run(() => InjectionLoop(_cts.Token), _cts.Token);
		_log.LogInformation("CEF injector started");
	}

	public async Task ReloadPluginFilesAsync()
	{
		CancellationToken ct = _cts?.Token ?? CancellationToken.None;
		string text = FindGabLuchiJs();
		if (text != null && File.Exists(text))
		{
			_gabluchiJs = await File.ReadAllTextAsync(text, ct);
			_log.LogInformation("Loaded gabluchi.js ({Length} bytes)", _gabluchiJs.Length);
		}
		else
		{
			_gabluchiJs = "";
			_log.LogWarning("gabluchi.js not found");
		}
		string text2 = FindPolyfillJs();
		if (text2 != null && File.Exists(text2))
		{
			_polyfillJs = await File.ReadAllTextAsync(text2, ct);
		}
		else
		{
			_polyfillJs = BuildInlinePolyfill();
		}
		string newFingerprint = ComputeFingerprint(_polyfillJs + "\n" + _gabluchiJs);
		if (newFingerprint == "")
		{
			return;
		}
		bool changed = (_persistedFingerprint != "" && _persistedFingerprint != newFingerprint) || (_loadedFingerprint != "" && _loadedFingerprint != newFingerprint);
		_loadedFingerprint = newFingerprint;
		try
		{
			File.WriteAllText(GetFingerprintStorePath(), newFingerprint);
		}
		catch (Exception ex)
		{
			_log.LogDebug("CEF: failed to persist frontend fingerprint: {Message}", ex.Message);
		}
		if (changed)
		{
			_forceReload = true;
			_log.LogInformation("CEF: frontend content changed ({Old} -> {New}); store tabs will reload once", _persistedFingerprint != "" ? _persistedFingerprint : _loadedFingerprint, newFingerprint);
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_cts?.Cancel();
		foreach (ClientWebSocket value in _sockets.Values)
		{
			try
			{
				value.Dispose();
			}
			catch
			{
			}
		}
		_sockets.Clear();
		return Task.CompletedTask;
	}

	private async Task InjectionLoop(CancellationToken ct)
	{
		List<(string Id, string Ws)> storeTabs = new List<(string, string)>();
		int tick = 0;
		while (!ct.IsCancellationRequested)
		{
			try
			{
				if (tick % 7 == 0)
				{
					string text = await _http.GetStringAsync("http://127.0.0.1:8080/json", ct);
					if (!string.IsNullOrWhiteSpace(text))
					{
						List<CefTabInfo> list = JsonSerializer.Deserialize<List<CefTabInfo>>(text, JsonOpts) ?? new List<CefTabInfo>();
					string script = _polyfillJs + "\n" + _gabluchiJs;
					List<(string, string)> live = new List<(string, string)>();
					HashSet<string> seen = new HashSet<string>();
					bool needReload = _forceReload;
					_forceReload = false;
					foreach (CefTabInfo tab in list)
					{
						if ((tab.Url?.Contains("store.steampowered.com", StringComparison.OrdinalIgnoreCase) ?? false) && !string.IsNullOrEmpty(tab.WebSocketDebuggerUrl) && !string.IsNullOrEmpty(tab.Id))
						{
							seen.Add(tab.Id);
							bool ready = await EvaluateReturnAsync(tab.Id, tab.WebSocketDebuggerUrl, "String(window.__GabLuchiReady === true)", ct) == "true";
							if (ready && needReload)
							{
								await EvaluateAsync(tab.Id, tab.WebSocketDebuggerUrl, "location.reload(); void 0", ct);
								_log.LogInformation("CEF: reloaded store tab {TabId} for updated frontend", tab.Id);
							}
							else if (!ready)
							{
								await EvaluateAsync(tab.Id, tab.WebSocketDebuggerUrl, script, ct);
							}
							live.Add((tab.Id, tab.WebSocketDebuggerUrl));
						}
					}
						storeTabs = live;
						foreach (string item in _sockets.Keys.Where((string k) => !seen.Contains(k)).ToList())
						{
							EvictSocket(item);
						}
					}
				}
				foreach (var (tabId, wsUrl) in storeTabs)
				{
					await ProcessSingleTab(tabId, wsUrl, ct);
				}
				tick++;
				await Task.Delay(150, ct);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex2)
			{
				_log.LogDebug("CEF cycle: {Message}", ex2.Message);
				try
				{
					await Task.Delay(1000, ct);
				}
				catch
				{
					break;
				}
			}
		}
	}

	private async Task ProcessSingleTab(string tabId, string wsUrl, CancellationToken ct)
	{
		_ = 2;
		try
		{
			string text = await EvaluateReturnAsync(tabId, wsUrl, "JSON.stringify(Object.values(window.Millennium._pending||{}).map(function(r){return{m:r.method,a:JSON.stringify(r.args)}}))", ct);
			if (string.IsNullOrWhiteSpace(text) || text == "[]" || text == "null")
			{
				return;
			}
			List<PendingRequest> list = JsonSerializer.Deserialize<List<PendingRequest>>(text, JsonOpts);
			if ((list == null || list.Count == 0) ? true : false)
			{
				return;
			}
			List<object> responses = new List<object>();
			foreach (PendingRequest item in list)
			{
				try
				{
					string text2 = await CallBackendMethod(item.m ?? "", item.a ?? "{}");
					responses.Add(new
					{
						v = (JsonSerializer.Deserialize<object>(text2) ?? text2)
					});
				}
				catch (Exception ex)
				{
					responses.Add(new
					{
						e = ex.Message
					});
				}
			}
			string text3 = JsonSerializer.Serialize(responses);
			await EvaluateReturnAsync(tabId, wsUrl, "(function(r){var ks=Object.keys(window.Millennium._pending);for(var i=0;i<ks.length&&i<r.length;i++){var id=ks[i];window.Millennium._readyResponses[id]=r[i];delete window.Millennium._pending[id];}})(" + text3 + ");", ct);
		}
		catch
		{
		}
	}

	private async Task<string> CallBackendMethod(string method, string argsRaw)
	{
		if (!new Dictionary<string, (string, string)>
		{
			["HasGabLuchiForApp"] = ("GET", "/has/{appid}"),
			["CheckApisForApp"] = ("POST", "/check-sources/{appid}"),
			["CheckForFixes"] = ("POST", "/check-sources/{appid}"),
			["StartAddViaGabLuchiFromUrl"] = ("POST", "/download/{appid}"),
			["GetAddViaGabLuchiStatus"] = ("GET", "/download-status/{appid}"),
			["CancelAddViaGabLuchi"] = ("POST", "/cancel/{appid}"),
			["DeleteGabLuchiForApp"] = ("POST", "/remove/{appid}"),
			["StartGabLuchiAdd"] = ("POST", "/add/{appid}"),
			["GetGabLuchiAddStatus"] = ("GET", "/add-status/{appid}"),
			["PickGabLuchiAddSource"] = ("POST", "/add-source/{appid}"),
			["OpenSettings"] = ("POST", "/open/settings"),
			["OpenFix"] = ("POST", "/open/fix/{appid}"),
			["RestartSteam"] = ("POST", "/restart-steam"),
			["OpenExternalUrl"] = ("POST", "/open-url"),
			["CheckForUpdatesNow"] = ("POST", "/check-updates"),
			["ReadLoadedApps"] = ("GET", "/loaded-apps"),
			["DismissLoadedApps"] = ("POST", "/loaded-apps"),
			["GetApiList"] = ("GET", "/api-list"),
			["GetIconDataUrl"] = ("GET", "/icon"),
			["GetGamesDatabase"] = ("GET", "/games-database"),
			["Logger"] = ("POST", "/log")
		}.TryGetValue(method, out var value))
		{
			return "{\"success\":true}";
		}
		string text = value.Item2;
		if (argsRaw != null)
		{
			try
			{
				using JsonDocument jsonDocument = JsonDocument.Parse(argsRaw);
				foreach (JsonProperty item in jsonDocument.RootElement.EnumerateObject())
				{
					text = text.Replace("{" + item.Name + "}", Uri.EscapeDataString(item.Value.ToString()));
				}
			}
			catch
			{
			}
		}
		string requestUri = "http://127.0.0.1:6767" + text;
		if (value.Item1 == "GET")
		{
			return await _http.GetStringAsync(requestUri);
		}
		StringContent content = new StringContent(argsRaw ?? "{}", Encoding.UTF8, "application/json");
		return await (await _http.PostAsync(requestUri, content)).Content.ReadAsStringAsync();
	}

	private async Task<ClientWebSocket> GetSocketAsync(string tabId, string wsUrl, CancellationToken ct)
	{
		if (_sockets.TryGetValue(tabId, out ClientWebSocket value))
		{
			if (value.State == WebSocketState.Open)
			{
				return value;
			}
			EvictSocket(tabId);
		}
		ClientWebSocket ws = new ClientWebSocket();
		await ws.ConnectAsync(new Uri(wsUrl), ct);
		_sockets[tabId] = ws;
		return ws;
	}

	private void EvictSocket(string tabId)
	{
		if (_sockets.Remove(tabId, out ClientWebSocket value))
		{
			try
			{
				value.Dispose();
			}
			catch
			{
			}
		}
	}

	private async Task<string> EvaluateReturnAsync(string tabId, string wsUrl, string expression, CancellationToken ct)
	{
		_ = 2;
		try
		{
			ClientWebSocket ws = await GetSocketAsync(tabId, wsUrl, ct);
			int id = ++_cdpId;
			string s = JsonSerializer.Serialize(new
			{
				id = id,
				method = "Runtime.evaluate",
				@params = new
				{
					expression = expression,
					returnByValue = true
				}
			});
			await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(s)), WebSocketMessageType.Text, endOfMessage: true, ct);
			byte[] buffer = new byte[65536];
			using CancellationTokenSource timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10.0));
			using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
			while (!linked.Token.IsCancellationRequested)
			{
				StringBuilder sb = new StringBuilder();
				WebSocketReceiveResult webSocketReceiveResult;
				do
				{
					webSocketReceiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), linked.Token);
					if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
					{
						EvictSocket(tabId);
						return "";
					}
					sb.Append(Encoding.UTF8.GetString(buffer, 0, webSocketReceiveResult.Count));
				}
				while (!webSocketReceiveResult.EndOfMessage);
				string text = sb.ToString();
				JsonElement jsonElement;
				try
				{
					using JsonDocument jsonDocument = JsonDocument.Parse(text);
					jsonElement = jsonDocument.RootElement.Clone();
				}
				catch
				{
					continue;
				}
				if (jsonElement.TryGetProperty("id", out var value) && value.TryGetInt32(out var value2) && value2 == id)
				{
					if (jsonElement.TryGetProperty("result", out var value3) && value3.TryGetProperty("result", out var value4) && value4.TryGetProperty("value", out var value5) && value5.ValueKind == JsonValueKind.String)
					{
						return value5.GetString() ?? text;
					}
					return text;
				}
			}
			return "";
		}
		catch (Exception ex)
		{
			_log.LogDebug("EvaluateReturnAsync: CDP call failed: {Message}", ex.Message);
			EvictSocket(tabId);
			return "";
		}
	}

	private async Task EvaluateAsync(string tabId, string wsUrl, string script, CancellationToken ct)
	{
		await EvaluateReturnAsync(tabId, wsUrl, script, ct);
	}

	private string? FindGabLuchiJs()
	{
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		string[] array = new string[2]
		{
			Path.Combine(folderPath, "GabLuchi", "plugin", "public", "gabluchi.js"),
			Path.Combine(folderPath, "GabLuchi", "plugin", "gabluchi.js")
		};
		foreach (string text in array)
		{
			if (File.Exists(text))
			{
				return text;
			}
		}
		return null;
	}

	private string? FindPolyfillJs()
	{
		string[] array = new string[2]
		{
			Path.Combine(AppContext.BaseDirectory, "public", "millennium-polyfill.js"),
			Path.Combine(AppContext.BaseDirectory, "millennium-polyfill.js")
		};
		foreach (string text in array)
		{
			if (File.Exists(text))
			{
				return text;
			}
		}
		return null;
	}

	private static string GetFingerprintStorePath()
	{
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi", "plugin", ".frontend-fingerprint");
	}

	private static string ComputeFingerprint(string text)
	{
		try
		{
			using SHA256 sha = SHA256.Create();
			byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""));
			return Convert.ToHexString(hash).Substring(0, 16);
		}
		catch
		{
			return "";
		}
	}

	private string BuildInlinePolyfill()
	{
		return "\n(function(){\nvar real=window.Millennium;\nvar pending={},ready={},reqId=0;\nfunction ltCall(p,m,a){\n  var i='_ltr_'+(++reqId);\n  pending[i]={method:m,args:a,ts:Date.now()};\n  return new Promise(function(rv,rj){\n    var mx=100;\n    function ck(){\n      var r=ready[i];\n      if(r!==undefined){delete ready[i];if(r.e){rj(new Error(r.e))}else{rv(r.v)}return}\n      if(--mx>0){setTimeout(ck,50)}else{delete pending[i];rv({success:true})}\n    }\n    ck();\n  });\n}\nif(real&&typeof real.callServerMethod==='function'){\n  var realCall=real.callServerMethod.bind(real);\n  real.callServerMethod=function(p,m,a){return p==='gabluchi'?ltCall(p,m,a):realCall(p,m,a)};\n  real._pending=pending;\n  real._readyResponses=ready;\n  window.Millennium=real;\n}else{\n  window.Millennium={_pending:pending,_readyResponses:ready,callServerMethod:function(p,m,a){return ltCall(p,m,a)}};\n}\n})();\n";
	}
}
