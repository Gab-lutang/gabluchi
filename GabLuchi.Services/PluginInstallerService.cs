using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;
using GabLuchi.Resources;

namespace GabLuchi.Services;

public class PluginInstallerService(SteamService steam, GithubProxy gh, CefInjectorService injector)
{
	private sealed record LoaderSlot(string DllAsset, string RealName, string SystemSourceName)
	{
		public string SystemSourcePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), SystemSourceName);
	}

	private sealed class Manifest
	{
		public string? Tag { get; set; }

		public Dictionary<string, string>? DllShas { get; set; }

		public string? ZipSha { get; set; }

		public Dictionary<string, List<string>>? DisabledMillenniumEntries { get; set; }
	}

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	private const string PluginZipAsset = "plugin.zip";

	private static readonly LoaderSlot[] Slots = new LoaderSlot[1]
	{
		new LoaderSlot("winmm.dll", "winmm_real.dll", "winmm.dll")
	};

	private static readonly string[] LegacyDllNames = new string[5] { "bcrypt.dll", "bcrypt_real.dll", "psapi.dll", "dbghelp.dll", "dbghelp_real.dll" };

	private const string CdpMarkerName = ".cef-enable-remote-debugging";

	private const string CdpMarkerJunctionTarget = "C:\\fuckass\\folder\\that\\shall\\never\\exist\\die\\millennium";

	private const int CdpPort = 8080;

	private static readonly HttpClient PortProbeHttp = new HttpClient
	{
		Timeout = TimeSpan.FromMilliseconds(800.0)
	};

	private const string DllUpdateDisabledMarker = ".gabluchi-dll-update-disabled";

	private GithubRelease? _cachedLatest;

	private const string MillenniumPluginName = "gabluchi";

	private const string MillenniumDisabledName = "gabluchi.disabled-by-gabluchi";

	private static readonly JsonSerializerOptions ConfigWriteOpts = new JsonSerializerOptions
	{
		WriteIndented = true,
		TypeInfoResolver = new DefaultJsonTypeInfoResolver()
	};

	private string? CdpMarkerPath
	{
		get
		{
			string steamDir = SteamDir;
			if (steamDir == null)
			{
				return null;
			}
			return Path.Combine(steamDir, ".cef-enable-remote-debugging");
		}
	}

	private static string FrontendDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi", "plugin");

	private static string GabLuchiJsPath => Path.Combine(FrontendDir, "public", "gabluchi.js");

	private static string? FindFrontendJsPath()
	{
		string[] array = new string[2]
		{
			Path.Combine(FrontendDir, "public", "gabluchi.js"),
			Path.Combine(FrontendDir, "gabluchi.js")
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

	private static string ManifestPath => Path.Combine(FrontendDir, "installed.json");

	private string? SteamDir => steam.EffectivePath;

	private IEnumerable<string> LegacyDllPaths
	{
		get
		{
			string s = SteamDir;
			if (s == null)
			{
				return Enumerable.Empty<string>();
			}
			return LegacyDllNames.Select((string n) => Path.Combine(s, n));
		}
	}

	private bool DllUpdateDisabled
	{
		get
		{
			string steamDir = SteamDir;
			if (steamDir != null)
			{
				return File.Exists(Path.Combine(steamDir, ".gabluchi-dll-update-disabled"));
			}
			return false;
		}
	}

	public bool MillenniumPresent
	{
		get
		{
			string steamDir = SteamDir;
			if (steamDir != null)
			{
				return File.Exists(Path.Combine(steamDir, "millennium", "lib", "millennium.dll"));
			}
			return false;
		}
	}

	private static bool IsReparsePoint(string path)
	{
		try
		{
			return (Directory.Exists(path) || File.Exists(path)) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
		}
		catch
		{
			return false;
		}
	}

	private static void CreateCdpMarkerJunction(string path)
	{
		if (IsReparsePoint(path))
		{
			return;
		}
		try
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive: true);
			}
			else if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
		using Process process = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /j \"{path}\" \"{"C:\\fuckass\\folder\\that\\shall\\never\\exist\\die\\millennium"}\"")
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		});
		process?.WaitForExit(5000);
	}

	private static void RemoveCdpMarkerJunction(string path)
	{
		if (!Directory.Exists(path))
		{
			return;
		}
		using Process process = Process.Start(new ProcessStartInfo("cmd.exe", "/c rmdir \"" + path + "\"")
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		});
		process?.WaitForExit(5000);
	}

	private static async Task<bool> IsPort8080BusyAsync()
	{
		try
		{
			using TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 8080);
			tcpListener.Start();
			tcpListener.Stop();
			return false;
		}
		catch (SocketException)
		{
			try
			{
				return !(await PortProbeHttp.GetStringAsync($"http://127.0.0.1:{8080}/json")).TrimStart().StartsWith('[');
			}
			catch
			{
				return true;
			}
		}
		catch
		{
			return false;
		}
	}

	private string? SlotPath(LoaderSlot slot)
	{
		string steamDir = SteamDir;
		if (steamDir == null)
		{
			return null;
		}
		return Path.Combine(steamDir, slot.DllAsset);
	}

	private string? SlotRealPath(LoaderSlot slot)
	{
		string steamDir = SteamDir;
		if (steamDir == null)
		{
			return null;
		}
		return Path.Combine(steamDir, slot.RealName);
	}

	private static Manifest? ReadManifest()
	{
		try
		{
			return File.Exists(ManifestPath) ? JsonSerializer.Deserialize<Manifest>(File.ReadAllText(ManifestPath), JsonOpts) : null;
		}
		catch
		{
			return null;
		}
	}

	private static void WriteManifest(Manifest m)
	{
		Directory.CreateDirectory(FrontendDir);
		File.WriteAllText(ManifestPath, JsonSerializer.Serialize(m));
	}

	public async Task<GithubRelease?> FetchLatestAsync(bool force, CancellationToken ct = default(CancellationToken))
	{
		if (!force && _cachedLatest != null)
		{
			return _cachedLatest;
		}
		string url = "https://raw.githubusercontent.com/Gab-lutang/gabluchi-plugin/main/release-info.json";
		try
		{
			using HttpResponseMessage res = await gh.SendAsync(url, ct);
			if (res == null || !res.IsSuccessStatusCode)
			{
				return null;
			}
			GithubRelease githubRelease = JsonSerializer.Deserialize<GithubRelease>(await res.Content.ReadAsStringAsync(ct), JsonOpts);
			if (githubRelease != null)
			{
				_cachedLatest = githubRelease;
			}
			return githubRelease;
		}
		catch
		{
			return null;
		}
	}

	public bool IsInstalledLocally()
	{
		if (FindFrontendJsPath() != null)
		{
			if (!Slots.Any(delegate(LoaderSlot s)
			{
				string text = SlotPath(s);
				return text != null && File.Exists(text);
			}))
			{
				return LegacyDllPaths.Any(File.Exists);
			}
			return true;
		}
		return false;
	}

	public async Task<PluginStatus> GetStatusAsync(bool force = false, CancellationToken ct = default(CancellationToken))
	{
		bool frontend = FindFrontendJsPath() != null;
		bool flag = Slots.Any(delegate(LoaderSlot slot)
		{
			string text = SlotPath(slot);
			return text != null && File.Exists(text);
		});
		bool legacy = LegacyDllPaths.Any(File.Exists);
		bool loader = flag || legacy;
		if (loader)
		{
			string cdpMarkerPath = CdpMarkerPath;
			if (cdpMarkerPath != null)
			{
				CreateCdpMarkerJunction(cdpMarkerPath);
			}
		}
		bool port8080Busy = await IsPort8080BusyAsync();
		Manifest manifest = ReadManifest();
		GithubRelease latest = await FetchLatestAsync(force, ct);
		if (latest == null)
		{
			return new PluginStatus(frontend, loader, DllMatches: false, manifest?.Tag, null, UpdateAvailable: false, MillenniumPresent, Offline: true, port8080Busy);
		}
		bool flag2 = Slots.All(delegate(LoaderSlot slot)
		{
			string text = SlotPath(slot);
			if (text != null && File.Exists(text))
			{
				string text2 = AssetDigest(latest, slot.DllAsset);
				if (text2 != null)
				{
					return Sha256OfFile(text) == text2;
				}
			}
			return false;
		});
		bool updateAvailable = frontend && loader && (manifest?.Tag != latest.TagName || !flag2 || legacy);
		return new PluginStatus(frontend, loader, flag2, manifest?.Tag, latest.TagName, updateAvailable, MillenniumPresent, Offline: false, port8080Busy);
	}

	public async Task<(bool ok, string? error)> InstallAsync(IProgress<double?>? progress, CancellationToken ct = default(CancellationToken))
	{
		string steamDir = SteamDir;
		if (steamDir == null)
		{
			return (ok: false, error: Strings.Plugin_Err_SteamNotFound);
		}
		GithubRelease latest = await FetchLatestAsync(force: true, ct);
		if (latest == null)
		{
			return (ok: false, error: Strings.Plugin_Err_GithubUnreachable);
		}
		GithubAsset githubAsset = FindAsset(latest, "plugin.zip");
		if (githubAsset == null)
		{
			return (ok: false, error: string.Format(Strings.Plugin_Err_MissingAssets, latest.TagName, "plugin.zip", Slots[0].DllAsset));
		}
		Dictionary<LoaderSlot, GithubAsset> slotAssets = new Dictionary<LoaderSlot, GithubAsset>();
		LoaderSlot[] slots = Slots;
		foreach (LoaderSlot loaderSlot in slots)
		{
			GithubAsset githubAsset2 = FindAsset(latest, loaderSlot.DllAsset);
			if (githubAsset2 == null)
			{
				return (ok: false, error: string.Format(Strings.Plugin_Err_MissingAssets, latest.TagName, "plugin.zip", loaderSlot.DllAsset));
			}
			slotAssets[loaderSlot] = githubAsset2;
		}
		string tmp = Path.Combine(Path.GetTempPath(), "gabluchi-plugin-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tmp);
		Dictionary<string, List<string>> disabledMillenniumEntries = null;
		try
		{
			string zipPath = Path.Combine(tmp, "plugin.zip");
			await gh.DownloadAsync(githubAsset.DownloadUrl, zipPath, progress, ct);
			Dictionary<LoaderSlot, string> slotDlPaths = new Dictionary<LoaderSlot, string>();
			LoaderSlot key;
			foreach (KeyValuePair<LoaderSlot, GithubAsset> item in slotAssets)
			{
				item.Deconstruct(out key, out var value);
				LoaderSlot slot = key;
				GithubAsset githubAsset3 = value;
				string p = Path.Combine(tmp, slot.DllAsset);
				await gh.DownloadAsync(githubAsset3.DownloadUrl, p, progress, ct);
				slotDlPaths[slot] = p;
			}
			string zipSha = Sha256OfFile(zipPath);
			string text = AssetDigest(latest, "plugin.zip");
			if (text != null && zipSha != text)
			{
				return (ok: false, error: string.Format(Strings.Plugin_Err_VerifyFailed, "plugin.zip"));
			}
			Dictionary<LoaderSlot, string> slotShas = new Dictionary<LoaderSlot, string>();
			foreach (KeyValuePair<LoaderSlot, string> item2 in slotDlPaths)
			{
				item2.Deconstruct(out key, out var value2);
				LoaderSlot loaderSlot2 = key;
				string text2 = Sha256OfFile(value2);
				slotShas[loaderSlot2] = text2;
				string text3 = AssetDigest(latest, loaderSlot2.DllAsset);
				if (text3 != null && text2 != text3)
				{
					return (ok: false, error: string.Format(Strings.Plugin_Err_VerifyFailed, loaderSlot2.DllAsset));
				}
			}
			if (Directory.Exists(FrontendDir))
			{
				Directory.Delete(FrontendDir, recursive: true);
			}
			Directory.CreateDirectory(FrontendDir);
			ZipFile.ExtractToDirectory(zipPath, FrontendDir);
			NormalizeFrontendLayout();
			CanonicalizeFrontendJs();
			BrandFrontend();
			if (!File.Exists(GabLuchiJsPath))
			{
				return (ok: false, error: Strings.Plugin_Err_NoGabLuchiJs);
			}
			await injector.ReloadPluginFilesAsync();
			bool flag = LegacyDllPaths.Any(File.Exists);
			bool flag2 = Slots.Any(delegate(LoaderSlot loaderSlot4)
			{
				string text5 = SlotPath(loaderSlot4);
				return text5 == null || !File.Exists(text5) || Sha256OfFile(text5) != slotShas[loaderSlot4];
			});
			if (!DllUpdateDisabled && (flag2 || flag))
			{
				bool wasRunning = Process.GetProcessesByName("steam").Length != 0;
				steam.StopSteam();
				await Task.Delay(1200, ct);
				slots = Slots;
				foreach (LoaderSlot loaderSlot3 in slots)
				{
					File.Copy(slotDlPaths[loaderSlot3], Path.Combine(steamDir, loaderSlot3.DllAsset), overwrite: true);
					string text4 = SlotRealPath(loaderSlot3);
					if (text4 != null && File.Exists(loaderSlot3.SystemSourcePath))
					{
						File.Copy(loaderSlot3.SystemSourcePath, text4, overwrite: true);
					}
				}
				foreach (string legacyDllPath in LegacyDllPaths)
				{
					if (File.Exists(legacyDllPath))
					{
						try
						{
							File.Delete(legacyDllPath);
						}
						catch
						{
						}
					}
				}
				if (MillenniumPresent)
				{
					RestoreMillenniumPluginFolder(steamDir);
					disabledMillenniumEntries = SetMillenniumGabLuchiEnabled(enable: false);
				}
				if (wasRunning)
				{
					steam.StartSteam();
				}
			}
			string cdpMarkerPath = CdpMarkerPath;
			if (cdpMarkerPath != null)
			{
				CreateCdpMarkerJunction(cdpMarkerPath);
			}
			WriteManifest(new Manifest
			{
				Tag = latest.TagName,
				DllShas = slotShas.ToDictionary<KeyValuePair<LoaderSlot, string>, string, string>((KeyValuePair<LoaderSlot, string> kv) => kv.Key.DllAsset, (KeyValuePair<LoaderSlot, string> kv) => kv.Value),
				ZipSha = zipSha,
				DisabledMillenniumEntries = disabledMillenniumEntries
			});
			return (ok: true, error: null);
		}
		catch (Exception ex)
		{
			return (ok: false, error: ex.Message);
		}
		finally
		{
			try
			{
				Directory.Delete(tmp, recursive: true);
			}
			catch
			{
			}
		}
	}

	public async Task<bool> AutoUpdateAsync(CancellationToken ct = default(CancellationToken))
	{
		_ = 1;
		try
		{
			if (!(await GetStatusAsync(force: true, ct)).UpdateAvailable)
			{
				return false;
			}
			return (await InstallAsync(null, ct)).Item1;
		}
		catch
		{
			return false;
		}
	}

	public Task<(bool ok, string? error)> UninstallAsync(CancellationToken ct = default(CancellationToken))
	{
		return Task.Run(async delegate
		{
			_ = 1;
			try
			{
				Manifest manifest = ReadManifest();
				bool wasRunning = Process.GetProcessesByName("steam").Length != 0;
				steam.StopSteam();
				await Task.Delay(1200, ct);
				LoaderSlot[] slots = Slots;
				foreach (LoaderSlot slot in slots)
				{
					string text = SlotPath(slot);
					if (text != null && File.Exists(text))
					{
						File.Delete(text);
					}
					string text2 = SlotRealPath(slot);
					if (text2 != null && File.Exists(text2))
					{
						File.Delete(text2);
					}
				}
				foreach (string legacyDllPath in LegacyDllPaths)
				{
					if (File.Exists(legacyDllPath))
					{
						File.Delete(legacyDllPath);
					}
				}
				string cdpMarkerPath = CdpMarkerPath;
				if (cdpMarkerPath != null)
				{
					RemoveCdpMarkerJunction(cdpMarkerPath);
				}
				if (Directory.Exists(FrontendDir))
				{
					Directory.Delete(FrontendDir, recursive: true);
				}
				if (MillenniumPresent)
				{
					SetMillenniumGabLuchiEnabled(enable: true, manifest?.DisabledMillenniumEntries);
				}
				await injector.ReloadPluginFilesAsync();
				if (wasRunning)
				{
					steam.StartSteam();
				}
				return ((bool, string))(true, null);
			}
			catch (Exception ex)
			{
				return (false, ex.Message);
			}
		}, ct);
	}

	private IEnumerable<string> MillenniumConfigPaths()
	{
		string s = SteamDir;
		if (s != null)
		{
			yield return Path.Combine(s, "millennium", "config", "config.json");
			yield return Path.Combine(s, "millennium", "config.json");
			yield return Path.Combine(s, "ext", "config.json");
			string environmentVariable = Environment.GetEnvironmentVariable("MILLENNIUM__CONFIG_PATH");
			if (!string.IsNullOrWhiteSpace(environmentVariable))
			{
				yield return Path.Combine(environmentVariable, "config.json");
			}
		}
	}

	private static bool IsGabLuchiEntry(string entry)
	{
		string text = entry.Split(' ', 2)[0];
		if (!text.Equals("gabluchi", StringComparison.OrdinalIgnoreCase))
		{
			return text.Equals("gabluchi.disabled-by-gabluchi", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static JsonArray? NestedEnabled(JsonObject root)
	{
		if (!(root["plugins"] is JsonObject jsonObject))
		{
			return null;
		}
		return jsonObject["enabledPlugins"] as JsonArray;
	}

	private static JsonArray? FlatEnabled(JsonObject root)
	{
		return root["plugins.enabledPlugins"] as JsonArray;
	}

	private Dictionary<string, List<string>> SetMillenniumGabLuchiEnabled(bool enable, IReadOnlyDictionary<string, List<string>>? restore = null)
	{
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
		foreach (string item in MillenniumConfigPaths())
		{
			try
			{
				if (!File.Exists(item) || !(JsonNode.Parse(File.ReadAllText(item)) is JsonObject jsonObject))
				{
					continue;
				}
				bool flag = false;
				if (!enable)
				{
					List<string> list = new List<string>();
					JsonArray jsonArray = NestedEnabled(jsonObject);
					if (jsonArray != null)
					{
						for (int num = jsonArray.Count - 1; num >= 0; num--)
						{
							string text = jsonArray[num]?.GetValue<string>();
							if (text != null && IsGabLuchiEntry(text))
							{
								list.Add(text);
								jsonArray.RemoveAt(num);
								flag = true;
							}
						}
					}
					JsonArray jsonArray2 = FlatEnabled(jsonObject);
					if (jsonArray2 != null)
					{
						for (int num2 = jsonArray2.Count - 1; num2 >= 0; num2--)
						{
							string text2 = jsonArray2[num2]?.GetValue<string>();
							if (text2 != null && IsGabLuchiEntry(text2))
							{
								jsonArray2.RemoveAt(num2);
								flag = true;
							}
						}
					}
					if (list.Count > 0)
					{
						list.Reverse();
						dictionary[item] = list;
					}
				}
				else
				{
					JsonArray jsonArray3 = NestedEnabled(jsonObject);
					if (jsonArray3 != null && !jsonArray3.Any(delegate(JsonNode n)
					{
						string text3 = n?.GetValue<string>();
						return text3 != null && IsGabLuchiEntry(text3);
					}))
					{
						List<string> value;
						foreach (string item2 in (restore != null && restore.TryGetValue(item, out value) && value.Count > 0) ? value : new List<string> { "gabluchi" })
						{
							jsonArray3.Add(item2);
						}
						flag = true;
					}
				}
				if (flag)
				{
					File.WriteAllText(item, jsonObject.ToJsonString(ConfigWriteOpts));
				}
			}
			catch
			{
			}
		}
		return dictionary;
	}

	private static void RestoreMillenniumPluginFolder(string steamDir)
	{
		try
		{
			string path = Path.Combine(steamDir, "millennium", "plugins");
			string text = Path.Combine(path, "gabluchi");
			string text2 = Path.Combine(path, "gabluchi.disabled-by-gabluchi");
			if (Directory.Exists(text2))
			{
				if (Directory.Exists(text))
				{
					Directory.Delete(text2, recursive: true);
				}
				else
				{
					Directory.Move(text2, text);
				}
			}
		}
		catch
		{
		}
	}

	private static void BrandFrontend()
	{
		try
		{
			string publicDir = Path.Combine(FrontendDir, "public");
			Directory.CreateDirectory(publicDir);
			byte[]? logo = LoadEmbeddedLogo();
			if (logo != null)
			{
				File.WriteAllBytes(Path.Combine(publicDir, "gabluchi-icon.png"), logo);
			}
			string pluginJsonPath = Path.Combine(FrontendDir, "plugin.json");
			if (File.Exists(pluginJsonPath) && JsonNode.Parse(File.ReadAllText(pluginJsonPath)) is JsonObject root)
			{
				root["common_name"] = "GabLuchi";
				root["description"] = "GabLuchi Steam Plugin!";
				File.WriteAllText(pluginJsonPath, root.ToJsonString(ConfigWriteOpts));
			}
		}
		catch
		{
		}
	}

	private static byte[]? LoadEmbeddedLogo()
	{
		try
		{
			using Stream s = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/gabluchi_logo.png"))?.Stream;
			if (s == null)
			{
				return null;
			}
			using MemoryStream ms = new MemoryStream();
			s.CopyTo(ms);
			return ms.ToArray();
		}
		catch
		{
			return null;
		}
	}

	private static void CanonicalizeFrontendJs()
	{
		if (File.Exists(GabLuchiJsPath))
		{
			return;
		}
		Directory.CreateDirectory(Path.GetDirectoryName(GabLuchiJsPath) ?? ".");
	}

	private static void NormalizeFrontendLayout()
	{
		if (Directory.Exists(Path.Combine(FrontendDir, "public")))
		{
			return;
		}
		string[] directories = Directory.GetDirectories(FrontendDir);
		foreach (string text in directories)
		{
			if (!Directory.Exists(Path.Combine(text, "public")))
			{
				continue;
			}
			string[] fileSystemEntries = Directory.GetFileSystemEntries(text);
			foreach (string text2 in fileSystemEntries)
			{
				string text3 = Path.Combine(FrontendDir, Path.GetFileName(text2));
				if (Directory.Exists(text2))
				{
					Directory.Move(text2, text3);
				}
				else
				{
					File.Move(text2, text3, overwrite: true);
				}
			}
			try
			{
				Directory.Delete(text, recursive: true);
				break;
			}
			catch
			{
				break;
			}
		}
	}

	private static string? AssetDigest(GithubRelease r, string name)
	{
		return ParseDigest(r.Assets.FirstOrDefault((GithubAsset a) => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Digest);
	}

	private static GithubAsset? FindAsset(GithubRelease r, string name)
	{
		return r.Assets.FirstOrDefault((GithubAsset a) => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
	}

	private static string Sha256OfFile(string path)
	{
		using FileStream source = File.OpenRead(path);
		return Convert.ToHexString(SHA256.HashData(source)).ToLowerInvariant();
	}

	private static string? ParseDigest(string? digest)
	{
		if (string.IsNullOrWhiteSpace(digest))
		{
			return null;
		}
		int num = digest.IndexOf(':');
		return ((num >= 0) ? digest.Substring(num + 1) : digest).Trim().ToLowerInvariant();
	}
}
