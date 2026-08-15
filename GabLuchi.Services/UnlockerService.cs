using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;
using GabLuchi.Resources;

namespace GabLuchi.Services;

public class UnlockerService(SteamService steam, SettingsService settings, CacheService cache, GithubProxy gh)
{
	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5.0);

	private readonly Dictionary<UnlockerMode, (GithubRelease release, DateTime fetchedAt)> _releaseCache = new Dictionary<UnlockerMode, (GithubRelease, DateTime)>();

	private const string MirrorRepoOwner = "mendy-tools";

	private const string MirrorRepo = "verynotsusdllsthataredefnotstrelated";

	private const string OstLuaPath = "config/stplug-in";

	private const string CloudRedirectDll = "cloud_redirect.dll";

	private (GithubRelease release, DateTime fetchedAt)? _crReleaseCache;

	public IReadOnlyList<ModeDefinition> Modes { get; } = new _003C_003Ez__ReadOnlyArray<ModeDefinition>(new ModeDefinition[4]
	{
		new ModeDefinition(UnlockerMode.SteamTools, "SteamTools", Strings.Mode_Desc_SteamTools, ModeKind.Loose, "mendy-tools", "verynotsusdllsthataredefnotstrelated", null, new string[2] { "dwmapi.dll", "xinput1_4.dll" }, null, null, null, null),
		new ModeDefinition(UnlockerMode.OpenSteamTools, "GabLuchi Unlocker", Strings.Mode_Desc_OpenSteamTools, ModeKind.Zip, "Gab-lutang", "gabluchi-unlocker", null, new string[4] { "dwmapi.dll", "xinput1_4.dll", "OpenSteamTool.dll", "opensteamtool.toml" }, "gabluchi-{version}-Release.zip", null, null, null),
		new ModeDefinition(UnlockerMode.OpenSteamToolsNightly, "GabLuchi Unlocker Nightly", Strings.Mode_Desc_OpenSteamToolsNightly, ModeKind.Zip, "Gab-lutang", "gabluchi-nightly", null, new string[4] { "dwmapi.dll", "xinput1_4.dll", "OpenSteamTool.dll", "opensteamtool.toml" }, "gabluchi-{version}-Release.zip", null, null, null),
		new ModeDefinition(UnlockerMode.CloudRedirect, "CloudRedirect (SteamTools Fix)", Strings.Mode_Desc_CloudRedirect, ModeKind.Cli, "Selectively11", "CloudRedirect", null, new string[1] { "cloud_redirect.dll" }, null, "CloudRedirectCLI.exe", "/stfixer", "cloud_redirect.dll")
	});

	public UnlockerMode? SelectedMode
	{
		get
		{
			if (!Enum.TryParse<UnlockerMode>(settings.SelectedMode, out var result))
			{
				return null;
			}
			return result;
		}
	}

	public string? SelectedModeDisplayName
	{
		get
		{
			UnlockerMode? selectedMode = SelectedMode;
			if (selectedMode.HasValue)
			{
				UnlockerMode valueOrDefault = selectedMode.GetValueOrDefault();
				return Def(valueOrDefault).DisplayName;
			}
			return null;
		}
	}

	private ModeDefinition Def(UnlockerMode mode)
	{
		return Modes.First((ModeDefinition m) => m.Mode == mode);
	}

	public async Task<ModeState> GetStateAsync(UnlockerMode mode, bool forceRefresh = false, CancellationToken ct = default(CancellationToken))
	{
		ModeDefinition def = Def(mode);
		bool active = SelectedMode == mode;
		string root = steam.EffectivePath;
		if (root == null || !steam.IsValid)
		{
			return new ModeState(mode, ModeStatus.Unknown, active, null);
		}
		switch (mode)
		{
		case UnlockerMode.OpenSteamTools:
			var (status2, latestVersion2) = await NightlyStatusAsync(root, ct);
			return new ModeState(mode, status2, active, latestVersion2);
		case UnlockerMode.SteamTools:
		{
			List<GithubRelease> list = await FetchAllReleasesAsync(def.Owner, def.Repo, null, ct);
			if (list == null)
			{
				return new ModeState(mode, ModeStatus.Unknown, active, null);
			}
			var (status, latestVersion) = SteamToolsStatus(def, list, root);
			return new ModeState(mode, status, active, latestVersion);
		}
		default:
		{
			GithubRelease githubRelease = await FetchReleaseAsync(def, forceRefresh, ct);
			if (githubRelease == null)
			{
				return new ModeState(mode, ModeStatus.Unknown, active, null);
			}
			return new ModeState(mode, LooseModeStatus(def, githubRelease, root), active, githubRelease.TagName);
		}
		}
	}

	private static ModeStatus LooseModeStatus(ModeDefinition def, GithubRelease release, string root)
	{
		bool flag = false;
		bool flag2 = false;
		string[] placeFiles = def.PlaceFiles;
		foreach (string file in placeFiles)
		{
			string path = Path.Combine(root, file);
			string text = ParseDigest(release.Assets.FirstOrDefault((GithubAsset a) => a.Name.Equals(file, StringComparison.OrdinalIgnoreCase))?.Digest);
			if (!File.Exists(path))
			{
				flag2 = true;
				continue;
			}
			flag = true;
			if (text == null || !Sha256OfFile(path).Equals(text, StringComparison.OrdinalIgnoreCase))
			{
				flag2 = true;
			}
		}
		if (!flag)
		{
			return ModeStatus.NotInstalled;
		}
		if (!flag2)
		{
			return ModeStatus.UpToDate;
		}
		return ModeStatus.UpdateAvailable;
	}

	private static (ModeStatus status, string? latestTag) SteamToolsStatus(ModeDefinition def, IReadOnlyList<GithubRelease> releases, string root)
	{
		bool flag = false;
		bool flag2 = false;
		string[] placeFiles = def.PlaceFiles;
		foreach (string text in placeFiles)
		{
			string path = Path.Combine(root, text);
			string text2 = ParseDigest(LatestAssetFor(releases, text)?.Digest);
			if (!File.Exists(path))
			{
				flag2 = true;
				continue;
			}
			flag = true;
			if (text2 == null || !Sha256OfFile(path).Equals(text2, StringComparison.OrdinalIgnoreCase))
			{
				flag2 = true;
			}
		}
		string item = (from r in releases
			where r.TagName.StartsWith("st", StringComparison.OrdinalIgnoreCase)
			orderby r.PublishedAt ?? DateTimeOffset.MinValue descending
			select r).FirstOrDefault()?.TagName;
		if (!flag)
		{
			return (status: ModeStatus.NotInstalled, latestTag: item);
		}
		return (status: flag2 ? ModeStatus.UpdateAvailable : ModeStatus.UpToDate, latestTag: item);
	}

	private async Task<(ModeStatus status, string? latestTag)> NightlyStatusAsync(string root, CancellationToken ct)
	{
		string dwmapi = Path.Combine(root, "dwmapi.dll");
		if (!File.Exists(dwmapi))
		{
			return (status: ModeStatus.NotInstalled, latestTag: null);
		}
		List<GithubRelease> list = await FetchAllReleasesAsync("mendy-tools", "verynotsusdllsthataredefnotstrelated", null, ct);
		if (list == null)
		{
			return (status: ModeStatus.Unknown, latestTag: null);
		}
		List<GithubRelease> list2 = (from r in list
			where r.TagName.StartsWith("ost-", StringComparison.OrdinalIgnoreCase)
			orderby r.PublishedAt ?? DateTimeOffset.MinValue descending
			select r).ToList();
		if (list2.Count == 0)
		{
			return (status: ModeStatus.Unknown, latestTag: null);
		}
		GithubRelease githubRelease = list2[0];
		string text = Sha256OfFile(dwmapi);
		if (AssetDigest(githubRelease, "dwmapi.dll") == text)
		{
			return (status: ModeStatus.UpToDate, latestTag: githubRelease.TagName);
		}
		return (status: ModeStatus.UpdateAvailable, latestTag: githubRelease.TagName);
	}

	public async Task<ModeInstallResult> InstallAsync(UnlockerMode mode, IProgress<double?>? progress = null, CancellationToken ct = default(CancellationToken))
	{
		ModeDefinition def = Def(mode);
		string root = steam.EffectivePath;
		if (root == null || !steam.IsValid)
		{
			return ModeInstallResult.Fail("Steam location not found — set it in Settings.");
		}
		Func<string, GithubAsset?> resolveSteamToolsAsset = null;
		if (mode == UnlockerMode.SteamTools)
		{
			List<GithubRelease> releases = await FetchAllReleasesAsync(def.Owner, def.Repo, null, ct);
			if (releases == null)
			{
				return ModeInstallResult.Fail("Couldn't reach GitHub — check your connection and try again.");
			}
			resolveSteamToolsAsset = (string file2) => LatestAssetFor(releases, file2);
			_ = (from r in releases
				where r.TagName.StartsWith("st", StringComparison.OrdinalIgnoreCase)
				orderby r.PublishedAt ?? DateTimeOffset.MinValue descending
				select r).FirstOrDefault()?.TagName;
		}
		GithubRelease release = null;
		if (mode != UnlockerMode.SteamTools)
		{
			release = await FetchReleaseAsync(def, forceRefresh: false, ct);
			if (release == null)
			{
				return ModeInstallResult.Fail("Couldn't reach GitHub — check your connection and try again.");
			}
		}
		if (def.Kind == ModeKind.Cli)
		{
			return await InstallViaCliAsync(def, release, root, progress, ct);
		}
		string staging = Path.Combine(Path.GetTempPath(), "GabLuchi", "mode", Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(staging);
			string zipDigest = null;
			Dictionary<string, string> staged;
			if (def.Kind == ModeKind.Zip)
			{
				GithubAsset asset = FindZipAsset(def, release);
				if (asset == null)
				{
					return ModeInstallResult.Fail("Release is missing the expected download.");
				}
				string zipPath = Path.Combine(staging, asset.Name);
				await DownloadToFileAsync(asset.DownloadUrl, zipPath, progress, ct);
				zipDigest = Sha256OfFile(zipPath);
				string text = ParseDigest(asset.Digest);
				if (text != null && !zipDigest.Equals(text, StringComparison.OrdinalIgnoreCase))
				{
					return ModeInstallResult.Fail("Download failed verification (sha256 mismatch).");
				}
				staged = ExtractWanted(zipPath, def.PlaceFiles, staging);
				List<string> list = def.PlaceFiles.Where((string f) => !staged.ContainsKey(f)).ToList();
				if (list.Count > 0)
				{
					return ModeInstallResult.Fail("Download is missing: " + string.Join(", ", list) + ".");
				}
			}
			else
			{
				staged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				string[] placeFiles = def.PlaceFiles;
				foreach (string file in placeFiles)
				{
					GithubAsset asset = ((resolveSteamToolsAsset != null) ? resolveSteamToolsAsset(file) : release.Assets.FirstOrDefault((GithubAsset a) => a.Name.Equals(file, StringComparison.OrdinalIgnoreCase)));
					if (asset == null)
					{
						return ModeInstallResult.Fail("Couldn't find " + file + " in any release.");
					}
					string zipPath = Path.Combine(staging, file);
					await DownloadToFileAsync(asset.DownloadUrl, zipPath, progress, ct);
					string text2 = ParseDigest(asset.Digest);
					if (text2 != null && !Sha256OfFile(zipPath).Equals(text2, StringComparison.OrdinalIgnoreCase))
					{
						return ModeInstallResult.Fail(file + " failed verification (sha256 mismatch).");
					}
					staged[file] = zipPath;
				}
			}
			List<string> list2 = new List<string>();
			string[] placeFiles2 = def.PlaceFiles;
			foreach (string text3 in placeFiles2)
			{
				try
				{
					string text4 = Path.Combine(root, text3);
					File.Copy(staged[text3], text4, overwrite: true);
					StampNow(text4);
				}
				catch
				{
					list2.Add(text3);
				}
			}
			settings.SelectedMode = mode.ToString();
			if (def.Kind == ModeKind.Zip)
			{
				cache.GabLuchiInstalledZipDigest = zipDigest;
				cache.GabLuchiInstalledVersion = release.TagName;
			}
			UnlockerMode unlockerMode = mode;
			if ((unlockerMode == UnlockerMode.OpenSteamTools || unlockerMode == UnlockerMode.OpenSteamToolsNightly) ? true : false)
			{
				try
				{
					EnsureGabLuchiLuaPath(root);
				}
				catch
				{
				}
			}
			return (list2.Count > 0) ? new ModeInstallResult(Success: false, $"Couldn't write {list2.Count} file(s) — close Steam and try again.", list2) : ModeInstallResult.Ok();
		}
		catch (OperationCanceledException)
		{
			return ModeInstallResult.Fail("Cancelled.");
		}
		catch (Exception ex2)
		{
			return ModeInstallResult.Fail(ex2.Message);
		}
		finally
		{
			try
			{
				Directory.Delete(staging, recursive: true);
			}
			catch
			{
			}
		}
	}

	private async Task<ModeInstallResult> InstallViaCliAsync(ModeDefinition def, GithubRelease release, string root, IProgress<double?>? progress, CancellationToken ct)
	{
		GithubAsset cliAsset = release.Assets.FirstOrDefault((GithubAsset a) => a.Name.Equals(def.CliAssetName, StringComparison.OrdinalIgnoreCase));
		if (cliAsset == null)
		{
			return ModeInstallResult.Fail("Release is missing " + def.CliAssetName + ".");
		}
		string wantedDigest = ParseDigest(release.Assets.FirstOrDefault((GithubAsset a) => a.Name.Equals(def.VerifyFile, StringComparison.OrdinalIgnoreCase))?.Digest);
		string staging = Path.Combine(Path.GetTempPath(), "GabLuchi", "mode", Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(staging);
			string cliPath = Path.Combine(staging, def.CliAssetName);
			await DownloadToFileAsync(cliAsset.DownloadUrl, cliPath, progress, ct);
			string text = ParseDigest(cliAsset.Digest);
			if (text != null && !Sha256OfFile(cliPath).Equals(text, StringComparison.OrdinalIgnoreCase))
			{
				return ModeInstallResult.Fail(def.CliAssetName + " failed verification (sha256 mismatch).");
			}
			progress?.Report(null);
			int num = await RunProcessAsync(cliPath, def.CliArgs ?? "", ct);
			if (num != 0)
			{
				return ModeInstallResult.Fail($"{def.CliAssetName} exited with code {num}.");
			}
			string path = Path.Combine(root, def.VerifyFile);
			if (!File.Exists(path))
			{
				return ModeInstallResult.Fail(def.VerifyFile + " was not deployed — the fix didn't complete.");
			}
			if (wantedDigest != null && !Sha256OfFile(path).Equals(wantedDigest, StringComparison.OrdinalIgnoreCase))
			{
				return ModeInstallResult.Fail(def.VerifyFile + " is not the expected version — the update didn't apply.");
			}
			settings.SelectedMode = def.Mode.ToString();
			return ModeInstallResult.Ok();
		}
		catch (OperationCanceledException)
		{
			return ModeInstallResult.Fail("Cancelled.");
		}
		catch (Exception ex2)
		{
			return ModeInstallResult.Fail(ex2.Message);
		}
		finally
		{
			try
			{
				Directory.Delete(staging, recursive: true);
			}
			catch
			{
			}
		}
	}

	private static async Task<int> RunProcessAsync(string exePath, string args, CancellationToken ct)
	{
		ProcessStartInfo startInfo = new ProcessStartInfo(exePath, args)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = (Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory)
		};
		using Process proc = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the fixer.");
		await proc.WaitForExitAsync(ct);
		return proc.ExitCode;
	}

	public async Task<UnlockerMode?> DetectActiveModeAsync(CancellationToken ct = default(CancellationToken))
	{
		string root = steam.EffectivePath;
		if (root == null || !steam.IsValid)
		{
			return null;
		}
		string dwmapi = Path.Combine(root, "dwmapi.dll");
		string xinput = Path.Combine(root, "xinput1_4.dll");
		UnlockerMode? detected = null;
		string ostDll = Path.Combine(root, "OpenSteamTool.dll");
		if (File.Exists(ostDll))
		{
			List<GithubRelease> list = await FetchAllReleasesAsync("Gab-lutang", "gabluchi-nightly", null, ct);
			if (list != null)
			{
				string ostHash = Sha256OfFile(ostDll);
				if (list.Any((GithubRelease r) => AssetDigest(r, "OpenSteamTool.dll") == ostHash))
				{
					detected = UnlockerMode.OpenSteamToolsNightly;
				}
			}
		}
		if (!detected.HasValue)
		{
			List<GithubRelease> list2 = await FetchAllReleasesAsync("mendy-tools", "verynotsusdllsthataredefnotstrelated", null, ct);
			if (list2 != null)
			{
				if (BothDllsMatchPrefix(list2, "ost-"))
				{
					detected = UnlockerMode.OpenSteamTools;
				}
				else if (BothDllsMatchPrefix(list2, "st"))
				{
					detected = UnlockerMode.SteamTools;
				}
			}
		}
		if (!detected.HasValue)
		{
			string crDll = Path.Combine(root, "cloud_redirect.dll");
			if (File.Exists(crDll))
			{
				List<GithubRelease> list3 = await FetchAllReleasesAsync("Selectively11", "CloudRedirect", null, ct);
				string crHash = Sha256OfFile(crDll);
				if (list3 != null && list3.Any((GithubRelease r) => AssetDigest(r, "cloud_redirect.dll") == crHash))
				{
					detected = UnlockerMode.CloudRedirect;
				}
			}
		}
		if (detected.HasValue)
		{
			UnlockerMode valueOrDefault = detected.GetValueOrDefault();
			settings.SelectedMode = valueOrDefault.ToString();
		}
		return detected;
		bool BothDllsMatchPrefix(IReadOnlyList<GithubRelease> releases, string tagPrefix)
		{
			if (!File.Exists(dwmapi) || !File.Exists(xinput))
			{
				return false;
			}
			List<GithubRelease> list4 = releases.Where((GithubRelease r) => r.TagName.StartsWith(tagPrefix, StringComparison.OrdinalIgnoreCase)).ToList();
			if (list4.Count == 0)
			{
				return false;
			}
			string dwmHash = Sha256OfFile(dwmapi);
			string xinHash = Sha256OfFile(xinput);
			bool flag = list4.Any((GithubRelease r) => AssetDigest(r, "dwmapi.dll") == dwmHash);
			bool flag2 = list4.Any((GithubRelease r) => AssetDigest(r, "xinput1_4.dll") == xinHash);
			return flag && flag2;
		}
	}

	private static string? AssetDigest(GithubRelease r, string assetName)
	{
		return ParseDigest(r.Assets.FirstOrDefault((GithubAsset a) => a.Name.Equals(assetName, StringComparison.OrdinalIgnoreCase))?.Digest);
	}

	private static GithubAsset? FindAsset(GithubRelease r, string assetName)
	{
		return r.Assets.FirstOrDefault((GithubAsset a) => a.Name.Equals(assetName, StringComparison.OrdinalIgnoreCase));
	}

	private static GithubAsset? LatestAssetFor(IReadOnlyList<GithubRelease> releases, string file)
	{
		return (from r in releases
			where r.TagName.StartsWith("st", StringComparison.OrdinalIgnoreCase)
			orderby r.PublishedAt ?? DateTimeOffset.MinValue descending
			select FindAsset(r, file)).FirstOrDefault((GithubAsset a) => a != null);
	}

	private async Task<List<GithubRelease>?> FetchAllReleasesAsync(string owner, string repo, string? tag, CancellationToken ct)
	{
		string url = ((tag != null) ? $"https://api.github.com/repos/{owner}/{repo}/releases/tags/{tag}" : $"https://api.github.com/repos/{owner}/{repo}/releases?per_page=100");
		try
		{
			using HttpResponseMessage res = await gh.SendAsync(url, ct);
			if (res == null || !res.IsSuccessStatusCode)
			{
				return null;
			}
			string json = await res.Content.ReadAsStringAsync(ct);
			if (tag != null)
			{
				GithubRelease githubRelease = JsonSerializer.Deserialize<GithubRelease>(json, JsonOpts);
				return (githubRelease == null) ? null : new List<GithubRelease>(1) { githubRelease };
			}
			return JsonSerializer.Deserialize<List<GithubRelease>>(json, JsonOpts);
		}
		catch
		{
			return null;
		}
	}

	private static void EnsureGabLuchiLuaPath(string steamRoot)
	{
		string path = Path.Combine(steamRoot, "opensteamtool.toml");
		if (!File.Exists(path))
		{
			File.WriteAllText(path, "[lua]\r\npaths = [\"config\\\\stplug-in\"]");
			return;
		}
		List<string> list = File.ReadAllLines(path).ToList();
		int num = list.FindIndex((string l) => IsActiveTableHeader(l, "lua"));
		if (num < 0)
		{
			if (list.Count > 0)
			{
				if (list[list.Count - 1].Trim().Length > 0)
				{
					list.Add("");
				}
			}
			list.Add("[lua]");
			list.Add("paths = [\"config\\\\stplug-in\"]");
			File.WriteAllLines(path, list);
			return;
		}
		int num2 = list.FindIndex(num + 1, IsActiveAnyTableHeader);
		if (num2 < 0)
		{
			num2 = list.Count;
		}
		int num3 = -1;
		for (int num4 = num + 1; num4 < num2; num4++)
		{
			string text = list[num4].TrimStart();
			if (!text.StartsWith('#') && Regex.IsMatch(text, "^paths\\s*="))
			{
				num3 = num4;
				break;
			}
		}
		if (num3 < 0)
		{
			list.Insert(num + 1, "paths = [\"config\\\\stplug-in\"]");
			File.WriteAllLines(path, list);
			return;
		}
		int num5;
		for (num5 = num3; num5 < num2 && !list[num5].Contains(']'); num5++)
		{
		}
		if (num5 >= num2)
		{
			num5 = num2 - 1;
		}
		if (!Regex.IsMatch(string.Join("\n", list.GetRange(num3, num5 - num3 + 1)), "[\"']\\s*" + Regex.Escape("config/stplug-in").Replace("/", "[/\\\\]+") + "\\s*[\"']", RegexOptions.IgnoreCase))
		{
			int index = num5;
			string text2 = list[index];
			int num6 = text2.LastIndexOf(']');
			string text3 = text2.Substring(0, num6).TrimEnd();
			string text4 = (Regex.IsMatch(text3, "\\[\\s*$") ? (text3 + " \"config\\\\stplug-in\"") : (text3 + ", \"config\\\\stplug-in\""));
			list[index] = text4 + text2.Substring(num6);
			File.WriteAllLines(path, list);
		}
	}

	private static bool IsActiveTableHeader(string line, string name)
	{
		string text = line.TrimStart();
		if (!text.StartsWith('#'))
		{
			return Regex.IsMatch(text, "^\\[\\s*" + Regex.Escape(name) + "\\s*\\]");
		}
		return false;
	}

	private static bool IsActiveAnyTableHeader(string line)
	{
		string text = line.TrimStart();
		if (!text.StartsWith('#'))
		{
			return Regex.IsMatch(text, "^\\[[^\\[].*\\]");
		}
		return false;
	}

	private async Task<GithubRelease?> FetchCloudRedirectReleaseAsync(bool forceRefresh, CancellationToken ct)
	{
		if (!forceRefresh)
		{
			(GithubRelease, DateTime)? crReleaseCache = _crReleaseCache;
			if (crReleaseCache.HasValue)
			{
				(GithubRelease, DateTime) valueOrDefault = crReleaseCache.GetValueOrDefault();
				if (DateTime.UtcNow - valueOrDefault.Item2 < CacheTtl)
				{
					return valueOrDefault.Item1;
				}
			}
		}
		string url = "https://api.github.com/repos/Selectively11/CloudRedirect/releases/latest";
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
				_crReleaseCache = (githubRelease, DateTime.UtcNow);
			}
			return githubRelease;
		}
		catch
		{
			return null;
		}
	}

	public async Task<CloudRedirectAddonState> GetCloudRedirectStateAsync(bool checkUpdate, bool forceRefresh = false, CancellationToken ct = default(CancellationToken))
	{
		string effectivePath = steam.EffectivePath;
		if (effectivePath == null || !steam.IsValid)
		{
			return new CloudRedirectAddonState(Installed: false, Enabled: false, UpdateAvailable: false, null);
		}
		string dll = Path.Combine(effectivePath, "cloud_redirect.dll");
		bool installed = File.Exists(dll);
		bool enabled = ReadGabLuchiCloudEnabled(effectivePath);
		bool updateAvailable = false;
		string latest = null;
		if (checkUpdate && installed)
		{
			GithubRelease githubRelease = await FetchCloudRedirectReleaseAsync(forceRefresh, ct);
			if (githubRelease != null)
			{
				latest = githubRelease.TagName;
				string text = AssetDigest(githubRelease, "cloud_redirect.dll");
				if (text != null && !Sha256OfFile(dll).Equals(text, StringComparison.OrdinalIgnoreCase))
				{
					updateAvailable = true;
				}
			}
		}
		return new CloudRedirectAddonState(installed, enabled, updateAvailable, latest);
	}

	public async Task<ModeInstallResult> EnableCloudRedirectAsync(IProgress<double?>? progress = null, CancellationToken ct = default(CancellationToken))
	{
		string root = steam.EffectivePath;
		if (root == null || !steam.IsValid)
		{
			return ModeInstallResult.Fail("Steam location not found — set it in Settings.");
		}
		if (!File.Exists(Path.Combine(root, "cloud_redirect.dll")))
		{
			ModeInstallResult modeInstallResult = await DownloadCloudRedirectDllAsync(root, progress, ct);
			if (!modeInstallResult.Success)
			{
				return modeInstallResult;
			}
		}
		try
		{
			SetGabLuchiCloudEnabled(root, enabled: true);
		}
		catch (Exception ex)
		{
			return ModeInstallResult.Fail(ex.Message);
		}
		return ModeInstallResult.Ok();
	}

	public ModeInstallResult DisableCloudRedirect()
	{
		string effectivePath = steam.EffectivePath;
		if (effectivePath == null || !steam.IsValid)
		{
			return ModeInstallResult.Fail("Steam location not found — set it in Settings.");
		}
		try
		{
			SetGabLuchiCloudEnabled(effectivePath, enabled: false);
			return ModeInstallResult.Ok();
		}
		catch (Exception ex)
		{
			return ModeInstallResult.Fail(ex.Message);
		}
	}

	public async Task<ModeInstallResult> UpdateCloudRedirectAsync(IProgress<double?>? progress = null, CancellationToken ct = default(CancellationToken))
	{
		string effectivePath = steam.EffectivePath;
		if (effectivePath == null || !steam.IsValid)
		{
			return ModeInstallResult.Fail("Steam location not found — set it in Settings.");
		}
		return await DownloadCloudRedirectDllAsync(effectivePath, progress, ct);
	}

	private async Task<ModeInstallResult> DownloadCloudRedirectDllAsync(string root, IProgress<double?>? progress, CancellationToken ct)
	{
		GithubRelease githubRelease = await FetchCloudRedirectReleaseAsync(forceRefresh: true, ct);
		if (githubRelease == null)
		{
			return ModeInstallResult.Fail("Couldn't reach GitHub — check your connection and try again.");
		}
		GithubAsset asset = FindAsset(githubRelease, "cloud_redirect.dll");
		if (asset == null)
		{
			return ModeInstallResult.Fail("Release is missing cloud_redirect.dll.");
		}
		string staging = Path.Combine(Path.GetTempPath(), "GabLuchi", "cloud", Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(staging);
			string tmp = Path.Combine(staging, "cloud_redirect.dll");
			await DownloadToFileAsync(asset.DownloadUrl, tmp, progress, ct);
			string text = ParseDigest(asset.Digest);
			if (text != null && !Sha256OfFile(tmp).Equals(text, StringComparison.OrdinalIgnoreCase))
			{
				return ModeInstallResult.Fail("cloud_redirect.dll failed verification (sha256 mismatch).");
			}
			try
			{
				string text2 = Path.Combine(root, "cloud_redirect.dll");
				File.Copy(tmp, text2, overwrite: true);
				StampNow(text2);
			}
			catch
			{
				return ModeInstallResult.Fail("Couldn't write cloud_redirect.dll — close Steam and try again.");
			}
			return ModeInstallResult.Ok();
		}
		catch (OperationCanceledException)
		{
			return ModeInstallResult.Fail("Cancelled.");
		}
		catch (Exception ex2)
		{
			return ModeInstallResult.Fail(ex2.Message);
		}
		finally
		{
			try
			{
				Directory.Delete(staging, recursive: true);
			}
			catch
			{
			}
		}
	}

	private static void SetGabLuchiCloudEnabled(string steamRoot, bool enabled)
	{
		string path = Path.Combine(steamRoot, "gabluchi.toml");
		string text = (enabled ? "true" : "false");
		if (!File.Exists(path))
		{
			File.WriteAllText(path, "[cloud]\nenabled = " + text + "\n");
			return;
		}
		List<string> list = File.ReadAllLines(path).ToList();
		int num = list.FindIndex((string l) => IsActiveTableHeader(l, "cloud"));
		if (num < 0)
		{
			if (list.Count > 0)
			{
				if (list[list.Count - 1].Trim().Length > 0)
				{
					list.Add("");
				}
			}
			list.Add("[cloud]");
			list.Add("enabled = " + text);
			File.WriteAllLines(path, list);
			return;
		}
		int num2 = list.FindIndex(num + 1, IsActiveAnyTableHeader);
		if (num2 < 0)
		{
			num2 = list.Count;
		}
		for (int num3 = num + 1; num3 < num2; num3++)
		{
			string text2 = list[num3].TrimStart();
			if (!text2.StartsWith('#') && Regex.IsMatch(text2, "^enabled\\s*="))
			{
				string text3 = list[num3].Substring(0, list[num3].Length - list[num3].TrimStart().Length);
				list[num3] = text3 + "enabled = " + text;
				File.WriteAllLines(path, list);
				return;
			}
		}
		list.Insert(num + 1, "enabled = " + text);
		File.WriteAllLines(path, list);
	}

	private static bool ReadGabLuchiCloudEnabled(string steamRoot)
	{
		string path = Path.Combine(steamRoot, "gabluchi.toml");
		if (!File.Exists(path))
		{
			return false;
		}
		string[] array = File.ReadAllLines(path);
		int num = Array.FindIndex(array, (string l) => IsActiveTableHeader(l, "cloud"));
		if (num < 0)
		{
			return false;
		}
		for (int num2 = num + 1; num2 < array.Length && !IsActiveAnyTableHeader(array[num2]); num2++)
		{
			string text = array[num2].TrimStart();
			if (!text.StartsWith('#'))
			{
				Match match = Regex.Match(text, "^enabled\\s*=\\s*(\\w+)");
				if (match.Success)
				{
					return match.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
				}
			}
		}
		return false;
	}

	private async Task<GithubRelease?> FetchReleaseAsync(ModeDefinition def, bool forceRefresh, CancellationToken ct)
	{
		if (!forceRefresh && _releaseCache.TryGetValue(def.Mode, out (GithubRelease, DateTime) value) && DateTime.UtcNow - value.Item2 < CacheTtl)
		{
			return value.Item1;
		}
		string url = ((def.FixedTag != null) ? $"https://api.github.com/repos/{def.Owner}/{def.Repo}/releases/tags/{def.FixedTag}" : $"https://api.github.com/repos/{def.Owner}/{def.Repo}/releases/latest");
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
				_releaseCache[def.Mode] = (githubRelease, DateTime.UtcNow);
			}
			return githubRelease;
		}
		catch
		{
			return null;
		}
	}

	private static GithubAsset? FindZipAsset(ModeDefinition def, GithubRelease release)
	{
		string wanted = (def.ZipAssetPattern ?? "").Replace("{version}", release.TagName);
		return release.Assets.FirstOrDefault((GithubAsset a) => a.Name.Equals(wanted, StringComparison.OrdinalIgnoreCase) && !a.Name.Contains("Debug", StringComparison.OrdinalIgnoreCase)) ?? release.Assets.FirstOrDefault((GithubAsset a) => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && a.Name.Contains("Release", StringComparison.OrdinalIgnoreCase) && !a.Name.Contains("Debug", StringComparison.OrdinalIgnoreCase));
	}

	private static Dictionary<string, string> ExtractWanted(string zipPath, string[] wanted, string destDir)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		using ZipArchive zipArchive = ZipFile.OpenRead(zipPath);
		foreach (ZipArchiveEntry entry in zipArchive.Entries)
		{
			if (!string.IsNullOrEmpty(entry.Name))
			{
				string text = wanted.FirstOrDefault((string w) => w.Equals(entry.Name, StringComparison.OrdinalIgnoreCase));
				if (text != null && !dictionary.ContainsKey(text))
				{
					string text2 = Path.Combine(destDir, text);
					entry.ExtractToFile(text2, overwrite: true);
					dictionary[text] = text2;
				}
			}
		}
		return dictionary;
	}

	private Task DownloadToFileAsync(string url, string destPath, IProgress<double?>? progress, CancellationToken ct)
	{
		return gh.DownloadAsync(url, destPath, progress, ct);
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

	private static void StampNow(string path)
	{
		try
		{
			DateTime now = DateTime.Now;
			File.SetCreationTime(path, now);
			File.SetLastWriteTime(path, now);
		}
		catch
		{
		}
	}
}
