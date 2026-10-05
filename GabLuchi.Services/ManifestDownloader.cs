using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi;
using GabLuchi.Models;
using GabLuchi.Resources;

namespace GabLuchi.Services;

public class ManifestDownloader
{
	private const string AppIdToken = "<appid>";

	/// <summary>Placeholder for a ManifestHub token, which is the AppID run through <see cref="ManifestHubCipher"/>.</summary>
	private const string EncodedIdToken = "<eid>";

	private static readonly string InterimDownloadsFolder = Path.Combine(Path.GetTempPath(), "GabLuchi", "downloads");

	/// <summary>
	/// Our own copy of every manifest we have ever fetched successfully. A game that is already in
	/// here installs with zero network calls, so a reinstall never touches the network at all.
	/// </summary>
	private static readonly string CacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi", "manifest-cache");

	private readonly HttpClient _http = DohHttp.CreateClient(TimeSpan.FromMinutes(10.0));

	private readonly AuthService _auth;

	private readonly LicenseService _license;

	/// <summary>Optional: lets us skip the network entirely for a game that is already unlocked.</summary>
	private readonly SteamService? _steam;

	private readonly LuaDlcEnricher? _dlcEnricher;

	private List<ApiSource>? _sources;

	private bool _sourcesLoaded;

	public ManifestDownloader(AuthService auth, LicenseService license, LuaDlcEnricher? dlcEnricher = null, SteamService? steam = null)
	{
		_auth = auth;
		_license = license;
		_dlcEnricher = dlcEnricher;
		_steam = steam;
	}

	public string? ResolveSourceUrl(string source)
	{
		return ResolveSource(source)?.Url;
	}

	public string? GetSourceUrl(string source, string appid)
	{
		string? template = ResolveSourceUrl(source);
		if (template is null)
		{
			return null;
		}
		string url = template.Replace(AppIdToken, appid);
		return template.Contains(EncodedIdToken, StringComparison.Ordinal)
			? url.Replace(EncodedIdToken, ManifestHubCipher.Encode(appid))
			: url;
	}

	/// <summary>
	/// Name → status for every source the downloader will actually try. The old answer came from
	/// an upstream status server that is gone; the trustworthy list is the one the walk itself
	/// uses, so a row shown here is a fetch we will genuinely attempt, and a miss is decided at
	/// fetch time instead of by a status page.
	/// </summary>
	public Dictionary<string, string> GetSourceStatus()
	{
		LoadSources();
		Dictionary<string, string> status = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (ApiSource source in _sources ?? new List<ApiSource>())
		{
			status[source.Name] = "available";
		}
		return status;
	}

	public async Task<DownloadedFile> DownloadDirectAsync(string url, string fileName, IProgress<double?>? progress, CancellationToken ct = default(CancellationToken))
	{
		using HttpResponseMessage fileRes = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
		if (!fileRes.IsSuccessStatusCode)
		{
			throw new ApiException(ClassifyStatus(fileRes.StatusCode, url), fileRes.StatusCode);
		}
		return await SaveResponseAsync(fileRes, fileName, progress, ct);
	}

	private ApiSource? ResolveSource(string source)
	{
		LoadSources();
		return _sources?.FirstOrDefault((ApiSource s) => string.Equals(s.Name, source, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Every downloadable source, cheapest and least rate-limited first: plain URL templates with
	/// optional placeholders, fetched directly — no gate, no metered hop, nothing in between.
	/// </summary>
	private static readonly (string Name, string UrlTemplate)[] DirectChain =
	[
		(MirrorSourceName, AppConfig.ManifestsMirrorBaseUrl + "/" + AppIdToken + ".zip"),
		(KeylessSourceName, "https://api.luagen.revobd.club/" + AppIdToken + ".zip"),
		// ManifestHub stores keyed zips per AppID (including 2025+ releases the bulk repos miss).
		// Plain AppIDs are refused, so the link carries the ciphered token instead, and the host
		// rate-limits bursts to roughly one request per 8 seconds: WaitManifestHubSlotAsync spaces
		// our own hits. src=0 and src=3 are two independent snapshots of the same library, kept as
		// consecutive entries so one snapshot going stale does not cost the install.
		("ManifestHub", "https://api.manifesthub.uk/download?id=" + EncodedIdToken + "&src=0"),
		("ManifestHub (alt snapshot)", "https://api.manifesthub.uk/download?id=" + EncodedIdToken + "&src=3"),
		// Keyed zip per branch on GitHub, verified for current 2025+ releases. Its snapshot lags
		// ManifestHub slightly, so it rides behind it, but it carries no rate limit and no cipher
		// — a plain GET that simply 404s when the branch does not exist.
		("EQhub", "https://codeload.github.com/CreeperKing3532/EQhub/zip/refs/heads/" + AppIdToken),
		("Sushi", "https://raw.githubusercontent.com/sushi-dev55-alt/sushitools-games-repo-alt/refs/heads/main/" + AppIdToken + ".zip")
	];

	/// <summary>Host-wide pacing for ManifestHub: its edge 429s anything tighter than roughly 8 seconds.</summary>
	private static readonly TimeSpan ManifestHubSpacing = TimeSpan.FromSeconds(10);

	private static readonly object ManifestHubGate = new();

	private static DateTime _lastManifestHubUtc = DateTime.MinValue;

	/// <summary>
	/// Claims the next request slot against ManifestHub's burst limit. The slot is reserved inside
	/// the lock before awaiting, so concurrent installs queue up instead of stampeding together.
	/// </summary>
	private static async Task WaitManifestHubSlotAsync(CancellationToken ct)
	{
		TimeSpan wait;
		lock (ManifestHubGate)
		{
			DateTime now = DateTime.UtcNow;
			DateTime next = _lastManifestHubUtc + ManifestHubSpacing;
			DateTime claim = next > now ? next : now;
			_lastManifestHubUtc = claim;
			wait = claim - now;
		}
		if (wait > TimeSpan.Zero)
		{
			await Task.Delay(wait, ct);
		}
	}

	/// <summary>
	/// One source attempt: pacing slot, optional auth, a single retry after a burst-limit answer,
	/// then keyed validation. Returns the file on a real hit and null on any miss — a miss is a
	/// normal outcome the caller walks away from, never an exception that aborts the walk.
	/// </summary>
	private async Task<DownloadedFile> TryFetchAsync(string url, string source, string appid, bool paced, bool requiresAuth, IProgress<double?> progress, CancellationToken ct)
	{
		for (int attempt = 0; ; attempt++)
		{
			if (paced)
			{
				await WaitManifestHubSlotAsync(ct);
			}
			try
			{
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
				if (requiresAuth)
				{
					string token = await _auth.GetValidAccessTokenAsync();
					request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				}
				using HttpResponseMessage res = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
				if (!res.IsSuccessStatusCode)
				{
					if (attempt == 0 && paced && res.StatusCode == (System.Net.HttpStatusCode)429)
					{
						// The pacing slot already pushes the retry a full interval out, so looping
						// is enough instead of burning the rest of the walk on a burst limit.
						continue;
					}
					return null;
				}
				DownloadedFile file = await SaveResponseAsync(res, appid + ".zip", progress, ct);
				if (!HasKeyedEntry(file.FilePath, appid))
				{
					// A 200 that carries no depot key unlocks nothing: a miss, not a success.
					DeleteStaged(file.FilePath);
					return null;
				}
				await EnrichDlcAsync(file.FilePath, appid, ct);
				StoreInCache(appid, file.FilePath);
				return file;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (ApiException ex) when (attempt == 0 && paced && ex.Status == (System.Net.HttpStatusCode)429)
			{
			}
			catch (Exception)
			{
				return null;
			}
		}
	}



	/// <summary>Our public mirror. Free, unmetered, and under our own control.</summary>
	public const string MirrorSourceName = "GabLuchi Mirror";

	/// <summary>
	/// A keyless upstream that serves a ready-made zip per App ID. Placed immediately after our own
	/// mirror so a game we already host never costs a third-party request, and ahead of the bulk
	/// repos because it returns a single small zip instead of a multi-megabyte archive.
	/// </summary>
	public const string KeylessSourceName = "Manifest Cache (keyless)";

	/// <summary>
	/// Resolves a manifest through the installed lua, then the local cache, then every free
	/// source in turn. This is the path the automatic (FastFetch) flows use; a genuine miss ends
	/// with an honest "no source" error instead of a request to a server we know is gone.
	/// </summary>
	public async Task<DownloadedFile> DownloadManifestChainAsync(string appid, string? gameName, IProgress<double?>? progress, CancellationToken ct = default(CancellationToken))
	{
		// A game whose lua is already installed with real depot keys is unlocked. Rebuilding it
		// would spend a network fetch on a file we already have, and on a machine with a real
		// library that is the normal case rather than an edge case.
		if (InstalledLuaIsUsable(appid))
		{
			return new DownloadedFile(string.Empty, $"{appid}.lua");
		}
		DownloadedFile? cached = TryGetCached(appid);
		if (cached != null)
		{
			return cached;
		}
		string encodedId = ManifestHubCipher.Encode(appid);
		foreach ((string name, string urlTemplate) in DirectChain)
		{
			bool paced = urlTemplate.Contains(EncodedIdToken, StringComparison.Ordinal);
			string url = urlTemplate.Replace(AppIdToken, appid).Replace(EncodedIdToken, encodedId);
			DownloadedFile file = await TryFetchAsync(url, name, appid, paced, requiresAuth: false, progress, ct);
			if (file != null)
			{
				return file;
			}
		}
		throw new ApiException($"No source has a keyed manifest for {appid}.");
	}

	/// <summary>
	/// Adds the DLC ids the fetched lua cannot express on its own. Cosmetic DLC and soundtracks have
	/// no depot, so no keyed line can cover them and they would otherwise stay locked even though the
	/// game installed fine. Purely additive and failure-tolerant: the manifest already carries its
	/// depot keys, so a bad lookup here must never fail an otherwise good install.
	/// </summary>
	private async Task EnrichDlcAsync(string path, string appid, CancellationToken ct)
	{
		if (_dlcEnricher is null || !long.TryParse(appid, out long id))
		{
			return;
		}
		try
		{
			await _dlcEnricher.EnrichDownloadedFileAsync(path, id, ct).ConfigureAwait(false);
		}
		catch
		{
		}
	}

	/// <summary>
	/// True when a lua for this appid is already installed and actually carries depot keys. Such a
	/// game is unlocked, so re-fetching it would spend a network round trip on a file we already
	/// have. This is the common case on a machine with a real library.
	/// </summary>
	public bool InstalledLuaIsUsable(string appid)
	{
		try
		{
			if (!long.TryParse(appid, out long id))
			{
				return false;
			}
			string? luaDir = _steam?.LuaDir;
			if (string.IsNullOrEmpty(luaDir))
			{
				return false;
			}
			string path = Path.Combine(luaDir, $"{appid}.lua");
			if (!File.Exists(path))
			{
				return false;
			}
		LuaContents? contents = LuaFileParser.Parse(path, id);
		if (contents == null || contents.DepotCount == 0)
		{
			return false;
		}
		return AcfHealth.GetState(_steam?.EffectivePath, id) != AcfState.Failed;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// A copy of <paramref name="appid"/>'s zip from our local cache, staged into the temp folder so
	/// the install path can treat it exactly like a freshly downloaded file. Null when not cached.
	/// </summary>
	public DownloadedFile? TryGetCached(string appid)
	{
		try
		{
			string cached = CachePath(appid);
			if (!File.Exists(cached))
			{
				return null;
			}
			if (!HasKeyedEntry(cached, appid))
			{
				return null;
			}
			Directory.CreateDirectory(InterimDownloadsFolder);
			string staged = Path.Combine(InterimDownloadsFolder, $"{appid}-cached.zip");
			File.Copy(cached, staged, overwrite: true);
			return new DownloadedFile(staged, $"{appid}.zip");
		}
		catch
		{
			return null;
		}
	}

	/// <summary>True when we already hold a usable copy, so no download is needed at all.</summary>
	public bool IsCached(string appid)
	{
		try
		{
			return File.Exists(CachePath(appid)) && HasKeyedEntry(CachePath(appid), appid);
		}
		catch
		{
			return false;
		}
	}

	private static string CachePath(string appid) => Path.Combine(CacheFolder, $"{appid}.zip");

	private void StoreInCache(string appid, string zipPath)
	{
		try
		{
			Directory.CreateDirectory(CacheFolder);
			string target = CachePath(appid);
			File.Copy(zipPath, target, overwrite: true);
		}
		catch
		{
			// A cache miss next time is not worth failing an install over.
		}
		// Hand it to the shared cache too, so the next person to ask for this game never has to
		// fetch it again. Fire and forget: a failure here is invisible to this user, who already
		// has the file.
		_ = UploadToSharedCacheAsync(appid, zipPath);
	}

	private async Task UploadToSharedCacheAsync(string appid, string zipPath)
	{
		try
		{
			string? token = _license.Token;
			if (string.IsNullOrWhiteSpace(token))
			{
				return;
			}
			byte[] payload = RepackStored(zipPath) ?? ReadAllBytesOrNull(zipPath);
			if (payload == null)
			{
				return;
			}
			string url = $"{Config.KeyCheckerBase.TrimEnd('/')}/zipcache/{appid}?token={Uri.EscapeDataString(token)}";
			using ByteArrayContent content = new ByteArrayContent(payload);
			content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
			using HttpResponseMessage res = await _http.PostAsync(url, content);
		}
		catch
		{
		}
	}

	private static byte[]? ReadAllBytesOrNull(string path)
	{
		try
		{
			return File.ReadAllBytes(path);
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Rebuilds a manifest zip with STORED (uncompressed) entries, or returns null to send the
	/// original bytes.
	/// </summary>
	/// <remarks>
	/// The shared cache validates an upload by looking for a depot key in the raw bytes, which
	/// only works when the entries are stored. A deflated archive keeps its text compressed, so a
	/// perfectly valid manifest would be rejected as keyless. Every zip in our own mirror is
	/// stored, so normalising here keeps one representation in the cache and stops good uploads
	/// from being refused. Archives that cannot be read are passed through untouched.
	/// </remarks>
	private static byte[]? RepackStored(string zipPath)
	{
		try
		{
			using FileStream input = File.OpenRead(zipPath);
			using ZipArchive source = new ZipArchive(input, ZipArchiveMode.Read);
			bool needsRepack = false;
			foreach (ZipArchiveEntry entry in source.Entries)
			{
				if (entry.Length != entry.CompressedLength)
				{
					needsRepack = true;
					break;
				}
			}
			if (!needsRepack)
			{
				return null;
			}
			using MemoryStream buffer = new MemoryStream();
			using (ZipArchive target = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
			{
				foreach (ZipArchiveEntry entry in source.Entries)
				{
					ZipArchiveEntry copy = target.CreateEntry(entry.FullName, CompressionLevel.NoCompression);
					copy.LastWriteTime = entry.LastWriteTime;
					using Stream src = entry.Open();
					using Stream dst = copy.Open();
					src.CopyTo(dst);
				}
			}
			return buffer.ToArray();
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// True when the staged file carries at least one depot key for this app. A manifest with no
	/// keys installs but unlocks nothing, so it must never be reported as a success or cached.
	/// </summary>
	public static bool HasKeyedEntry(string path, string appid)
	{
		try
		{
			if (!File.Exists(path))
			{
				return false;
			}
			if (!long.TryParse(appid, out long id))
			{
				return false;
			}
			LuaContents? contents;
			if (IsZip(path))
			{
				using ZipArchive archive = ZipFile.OpenRead(path);
				ZipArchiveEntry? entry = archive.Entries.FirstOrDefault((ZipArchiveEntry e) => e.Name.EndsWith(".lua", StringComparison.OrdinalIgnoreCase));
				if (entry == null)
				{
					return false;
				}
				string extracted = Path.Combine(Path.GetTempPath(), $"gabluchi_check_{id}_{Guid.NewGuid():N}.lua");
				try
				{
					entry.ExtractToFile(extracted, overwrite: true);
					contents = LuaFileParser.Parse(extracted, id);
				}
				finally
				{
					try { File.Delete(extracted); } catch { }
				}
			}
			else
			{
				contents = LuaFileParser.Parse(path, id);
			}
			return contents != null && contents.DepotCount > 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsZip(string path)
	{
		try
		{
			using FileStream stream = File.OpenRead(path);
			Span<byte> buffer = stackalloc byte[4];
			return stream.Read(buffer) == 4 && buffer[0] == 80 && buffer[1] == 75 && buffer[2] == 3 && buffer[3] == 4;
		}
		catch
		{
			return false;
		}
	}

	public static void DeleteStaged(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}

	public async Task<DownloadedFile> DownloadManifestAsync(string appid, string source, string? gameName, IProgress<double?>? progress, CancellationToken ct = default(CancellationToken))
	{
		ApiSource? resolved = ResolveSource(source);
		if (resolved == null || string.IsNullOrWhiteSpace(resolved.Url))
		{
			throw new ApiException($"No download URL configured for source '{source}'.");
		}
		// The picked source first: the user asked for it by name, and it usually has the game.
		// Its URL is fetched directly — the license hop and the upstream server it used to point
		// at are both gone — with the ciphered token swapped in where the template needs one.
		bool paced = resolved.Url.Contains(EncodedIdToken, StringComparison.Ordinal);
		string url = resolved.Url.Replace(AppIdToken, appid).Replace(EncodedIdToken, ManifestHubCipher.Encode(appid));
		DownloadedFile picked = await TryFetchAsync(url, source, appid, paced, resolved.RequiresAuth, progress, ct);
		if (picked != null)
		{
			return picked;
		}
		// Miss: walk every other free source instead of failing the whole add on one dead answer.
		// The picked source itself is skipped — it was already tried above.
		string encodedId = ManifestHubCipher.Encode(appid);
		foreach ((string name, string urlTemplate) in DirectChain)
		{
			if (string.Equals(urlTemplate, resolved.Url, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			bool chainPaced = urlTemplate.Contains(EncodedIdToken, StringComparison.Ordinal);
			string chainUrl = urlTemplate.Replace(AppIdToken, appid).Replace(EncodedIdToken, encodedId);
			DownloadedFile file = await TryFetchAsync(chainUrl, name, appid, chainPaced, requiresAuth: false, progress, ct);
			if (file != null)
			{
				return file;
			}
		}
		throw new ApiException($"No source has a keyed manifest for {appid}.");
	}

	/// <summary>
	/// Turns a status code into something a user can act on. A free source answers "we do not
	/// carry this game" with a bare 404, a burst limiter with a 429, and a network block with a
	/// 403 — a raw code explains none of them.
	/// </summary>
	public static string ClassifyStatus(System.Net.HttpStatusCode status, string source)
	{
		switch ((int)status)
		{
			case 403:
				return Strings.Add_Err_SourceBlocked;
			case 429:
				return Strings.Add_Err_QuotaExhausted;
			case 404:
				return Strings.Add_Err_SourceMissing;
			default:
				return $"{Strings.Add_Err_Download} ({(int)status})";
		}
	}

	private static async Task<DownloadedFile> SaveResponseAsync(HttpResponseMessage res, string fallbackName, IProgress<double?>? progress, CancellationToken ct)
	{
		string fileName = res.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? fallbackName;
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			fileName = fileName.Replace(oldChar, '_');
		}
		Directory.CreateDirectory(InterimDownloadsFolder);
		string filePath = Path.Combine(InterimDownloadsFolder, fileName);
		long? total = res.Content.Headers.ContentLength;
		await using (Stream src = await res.Content.ReadAsStreamAsync(ct))
		{
			await using (FileStream dst = File.Create(filePath))
			{
				byte[] buffer = new byte[81920];
				long written = 0L;
				while (true)
				{
					int read = await src.ReadAsync(buffer, ct);
					if (read <= 0)
					{
						break;
					}
					await dst.WriteAsync(buffer.AsMemory(0, read), ct);
					written += read;
					progress?.Report((total.HasValue && total.GetValueOrDefault() > 0) ? new double?((double)written / (double)total.Value) : ((double?)null));
				}
			}
		}
		return new DownloadedFile(filePath, fileName);
	}

	private void LoadSources()
	{
		if (_sourcesLoaded)
		{
			return;
		}
		_sourcesLoaded = true;
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
					List<ApiSource> list = new List<ApiSource>();
					foreach (JsonElement item in value.EnumerateArray())
					{
						JsonElement value2;
						string name = (item.TryGetProperty("name", out value2) ? (value2.GetString() ?? "") : "");
						JsonElement value3;
						string url = (item.TryGetProperty("url", out value3) ? (value3.GetString() ?? "") : "");
						JsonElement value4;
						int successCode = (item.TryGetProperty("success_code", out value4) ? value4.GetInt32() : 200);
						JsonElement value5;
						bool enabled = !item.TryGetProperty("enabled", out value5) || value5.GetBoolean();
						JsonElement value6;
						bool requiresAuth = item.TryGetProperty("auth", out value6) && value6.GetBoolean();
						// The dead upstream (167.235.229.108) never gets back in through this door,
					// whatever an old api.json sitting next to the exe might still list.
					if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(url) && enabled && !url.Contains("167.235.229.108", StringComparison.OrdinalIgnoreCase))
						{
							list.Add(new ApiSource(name, url, successCode, requiresAuth));
						}
					}
					if (list.Count > 0)
					{
						_sources = list;
						return;
					}
				}
			}
			catch
			{
			}
		}
		// The crack-backed set: our own mirror, the keyless luagen, ManifestHub's cipher-gated
		// library, EQhub for games those databases have not caught up with yet, and Sushi as the
		// bulk fallback. The old upstream (Ryuu/Luie on 167.235.229.108) is gone; nothing here
		// points at it or at the license gate that used to sit in front of it.
		_sources = new List<ApiSource>
		{
			new ApiSource(MirrorSourceName, AppConfig.ManifestsMirrorBaseUrl + "/" + AppIdToken + ".zip", 200),
			new ApiSource(KeylessSourceName, "https://api.luagen.revobd.club/" + AppIdToken + ".zip", 200),
			new ApiSource("ManifestHub", "https://api.manifesthub.uk/download?id=" + EncodedIdToken + "&src=0", 200),
			new ApiSource("EQhub", "https://codeload.github.com/CreeperKing3532/EQhub/zip/refs/heads/" + AppIdToken, 200),
			new ApiSource("Sushi", "https://raw.githubusercontent.com/sushi-dev55-alt/sushitools-games-repo-alt/refs/heads/main/" + AppIdToken + ".zip", 200)
		};
	}
}
