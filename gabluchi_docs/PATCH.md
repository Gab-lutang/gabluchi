# PATCH.md — GabLuchi "Fix applied (DONE) but game won't launch"

Affects: GabLuchi desktop app (denuvo/fixes tab), backend, and the `gabluchi-fixes` hosting
repo. Client source inspected from `github.com/Gab-lutang/gabluchi` (main, tree
`ceee689…`). All probes performed against the live endpoints on 2026-09-20.

---

## 1. TL;DR

The **"Fix" button never downloads the fix**. In `FixesViewModel.RunDownload` the
`"fix"` slot calls the *same* code path as the `"manifest"` slot —
`manifestDownloader.DownloadManifestAsync(appId, "Ryuu", …)` — which pulls the **Lua
manifest** (Steam identity file) from the manifest backend
(`http://167.235.229.108/<appid>`), not the DenuvOwO / voices38 artifact. The variable
built from `fix.FixFilename` (`fallbackName`) is **dead code** — never consumed. The
downloaded *manifest* zip is then extracted into the game install dir and a success toast
fires: **"Fix applied" / "DONE"**.

Result: Denuvo is never touched → the game won't launch. This is exactly the reported
symptom.

Additionally, **no backend route serves fix artifacts at all** (probed live, §4), so even
after wiring the client, the server must expose `{url}` for the correct zip. The fix files
themselves are scrapes from the lua.tools app / PeronDepot and are **not yet parked
anywhere reachable** — they need to be uploaded (recommend `gabluchi-fixes`, §5).

---

## 2. Symptom → diagnosis map

| What you saw | What actually happened |
|---|---|
| Pressed Download Fix / Apply on a `DenuvOwO` or `voices38 (crack)` fix | Client downloaded `<appid>.zip` **manifest** from `167.235.229.108` via the `Ryuu` source |
| "Fix applied" / DONE toast | `ApplyFix` unzipped that manifest into `library.GetInstallDir(appId)` with 0 validation, then toasted success |
| Game won't launch | DenuvOwO hypervisor files (`*.sys` driver, host, `VBS.cmd`, patch files) OR voices38 crack (`steam_api64.dll`, cracked exe) never landed; the game still has real Denuvo |
| Manifest button alone doesn't fix it | Manifest slot only pins the game identity via Lua (`forceLocked`) + restarts Steam; it never installs the bypass |

---

## 3. Root cause analysis (source-grounded)

### 3.1 `GabLuchi.ViewModels/FixesViewModel.cs` — `RunDownload(FixItemVm fix, string slot)`

```csharp
string fallbackName = ((slot == "manifest")
    ? (fix.ManifestFilename ?? (game.AppId + ".zip"))
    : (fix.FixFilename ?? (game.AppId + "_fix.zip")));   // ← computed…
// …
DownloadedFile file;
string? localFix = FixRepository.Resolve(game.AppId, slot);
if (localFix != null)
    file = new DownloadedFile(localFix, Path.GetFileName(localFix));
else
    file = await manifestDownloader.DownloadManifestAsync(game.AppId, "Ryuu", game.Name, progress);
// …but never used. Both slots pull the manifest. fallbackName is dead.
if (slot == "manifest") InstallManifest(file, appId, game.Name);
else                   ApplyFix(file, appId, game.Name);
```

`manifestDownloader.DownloadManifestAsync(appId, "Ryuu", …)` (see
`ManifestDownloader.cs`) resolves source `Ryuu` → `http://167.235.229.108/<appid>` →
license URL `{KeyCheckerBase}/manifest/{appid}?token=…` → response JSON `{"url": …}` →
downloads that file as `<appid>.zip`. Both slots therefore fetch the **manifest** zip.

### 3.2 No download method / route for fixes

- `GabLuchi.Services/GabLuchiApiClient.cs` — has `GetDenuvoListingsAsync`,
  `GetDenuvoFixesAsync`, DLC endpoints. **No fix-download call.**
- `GabLuchi.Services/LicenseService.cs` — only `GetDownloadUrl(appid)` =
  `{KeyCheckerBase}/manifest/{appid}?token=…`. **No fix URL.**
- `GabLuchi.Models/DenuvoDownloadResponse.cs` — `public string Url { get; set; }`.
  **Unused everywhere** (scaffolding for a route that was never built).
- `GabLuchi.ViewModels/FixItemVm.cs` — surfaces `HasFix`, `FixFilename`, `ManifestFilename`
  as *metadata only*. `FixFilename` is a filename, not a resolvable URL.

### 3.3 `ApplyFix` limitations

```csharp
using ZipArchive zipArchive = ZipFile.OpenRead(file.FilePath);   // ZIP-only, no password, no RAR/7z
…
entry.ExtractToFile(text, overwrite: true);                       // no .bak backup, no validation
```

- Real scene fixes are **RAR / password-protected zips** (see `gabluchi-fixes` §5:
  `online-fix.me`; Denuvo scene convention `cs.rin.ru`). `ZipFile.OpenRead` will throw on
  both → users get "Couldn't apply" toast.
- No `.bak` of replaced binaries (`steam_api64.dll`, `.exe`) → rollback impossible.
- No verification that expected files landed; success toast is unconditional unless an
  exception propagates.

### 3.4 `GabLuchi.Services/CrackFixService.cs` (separate "Crack fixes" tab)

- `BuzzheavierRegex` extracts a **buzzheavier file ID** but it is routed to
  `PixeldrainApiBase` (`https://pixeldrain.com/api/file/`) → wrong API host, URL can't
  resolve. (Long-standing hygiene item, confirmed live: only pixeldrain URLs actually
  work.)
- `Is7ZipAvailable` → if `7za.exe` is not next to `GabLuchi.exe`,
  `ExtractArchive` returns false but `DownloadFixAsync` still returns the (unextracted)
  directory → `ApplyToGame` copies nothing, no error, silent no-op.
- `ZipPassword` hard-coded `cs.rin.ru` — but `gabluchi-fixes` rars use `online-fix.me` (§5).
  Password must be per-source.

---

## 4. Live network evidence (2026-09-20, read-only probes)

| Probe | Result |
|---|---|
| `lua.tools/api/denuvo/listings` | 200 — 1797 games, 7 tags (`DenuvOwO`, `voices38 (crack)`, `SteamTools Achievements Fix`, `Online Fix`, `Generic`, `Rockstar Games`, `Ubisoft`). **No URL fields.** |
| `lua.tools/api/denuvo/fixes?appid=3321460` | 200 — Crimson Desert, 3 fixes: one `DenuvOwO` (id `b943cb78…`, `manifestFilename` `3321460_24613230.lua`, `fixFilename` `3321460.zip`, how-to description), two voices38. **No download URL.** |
| `lua.tools/api/denuvo/fix?appid=…&fixid=…` | 404 |
| `gabluchi-proxy.…workers.dev/manifest/3321460` | 401 (route exists, token-gated) |
| `gabluchi-proxy.…workers.dev/fix/3321460` | **404** |
| `167.235.229.108/{appid}` | 200 (manifest JSON/`{url}` flow) |
| `167.235.229.108/3321460.zip` etc. | 400 (server rejects non-`/{appid}` paths) |
| `raw.githubusercontent.com/Gab-lutang/gabluchi-fixes/main/fixes/FH5_….rar` | 200, 10.8 MB — **fix files ARE publicly hosted on GitHub** |
| `lua.tools/api/denuvo/download?fix=<uuid>&slot=fix` (no auth) | 401 `{"error":"Unauthorized"}` |
| `lua.tools/api/denuvo/download?fix=<uuid>&slot=fix` (Supabase session) | 200 `{"url":"<pre-signed Cloudflare R2 URL>"}` |
| pre-signed R2 URL (`…r2.cloudflarestorage.com/denuvo/{appid}/{uuid}/fix.zip?…&X-Amz-Expires=120…`) | 200 zip; `Content-Disposition: attachment; filename="{appid}.zip"` |
| `lua.tools/denuvo/{appid}.zip` (legacy) | 308 → `/fixes/…` → 404 |

Conclusion: **fix payloads DO have a route — the lua.tools download API, session-gated.**
The bug is purely client-side: `GabLuchiApiClient` never calls it. Required client flow:
auth (shared Supabase refresh token) → `GET /api/denuvo/download?fix={fixUUID}&slot=fix`
→ `{"url": …}` → stream the pre-signed R2 URL within 120 s → `{appid}.zip` (no password;
`cs.rin.ru` applies only to the separate CrakFiles/pixeldrain pipeline).
Manifest stays anonymous (`167.235.229.108/{appid}` → license `{url}` flow, Ryuu/Luie).

---

## 5. Fix-download fix (client-side; source = lua.tools download API)

### 5.1 Contract (HAR-confirmed 2026-09-20)
- `GET https://lua.tools/api/denuvo/download?fix={fixUUID}&slot=fix` — session-gated.
- Auth: Supabase session. Browser carries `sb-db-auth-token.0/.1` cookies (+ Cloudflare
  clearance); app sends the base64-decoded `access_token` as `Authorization: Bearer`,
  with cookie fallback if the gateway ignores the header.
- Response: `200 {"url": "<pre-signed R2 URL>"}`; `X-Amz-Expires=120` → stream now.
- Errors: 401 = bad/expired session; 404 = no fix for UUID. 25 downloads/day per account
  = soft anti-scrape hysteresis (SPA-side counter), not a hard entitlement.

### 5.2 Session provider (`ILuaToolsSession`)
- `RefreshTokenSession`: config `LuaTools:RefreshToken` beside `auth.dat` (never committed);
  mint via Supabase GoTrue token endpoint (project ref `db`), cache to `expires_in 3600`,
  Bearer-first, cookie fallback. `InteractiveSession` stub reserved for future per-user OAuth.
- The refresh token + Discord identity shared in 2026-09-20 chat are **burned** — rotate and
  supply a fresh one at implementation.

### 5.3 Client wiring (build details in §6)
- `GabLuchiApiClient.GetDenuvoDownloadAsync(fixId, slot="fix")` → `DenuvoDownloadResponse`.
- `FixesViewModel.RunDownload` fix slot: `FixRepository.Resolve` cache first → else API
  download → stream signed R2 URL → `%TEMP%\GabLuchi\downloads\{appid}.zip` → extract
  (zip, no password) → `ApplyToGame` + `.bak`. Delete dead `fallbackName`. Manifest slot
  untouched. Post-apply validation per §9 (`KIRIGIRI.bin` runtime-created → not required).

### 5.4 Deferred fallback (do NOT build now)
Park scraped zips in `gabluchi-fixes/fixes/denuvo/` + `denuvo.json` and serve via a
KeyChecker worker `/fix/{appid}` token route → `{"url": …}` (old 5.1/5.2 design), only if
lua.tools auth breaks or distribution outgrows a shared session.

---

## 6. Client changes (GabLuchi — apply against repository `main`)

### 6.1 `GabLuchi.Services/LicenseService.cs` — add fix-URL builder

```diff
 	public string? GetDownloadUrl(string appid)
 	{
 		if (!IsActivated)
 			return null;
 		return KeyCheckerBase.TrimEnd('/') + "/manifest/" + Uri.EscapeDataString(appid) + "?token=" + Uri.EscapeDataString(Token!);
 	}
+
+	public string? GetFixDownloadUrl(string appid)
+	{
+		if (!IsActivated)
+			return null;
+		return KeyCheckerBase.TrimEnd('/') + "/fix/" + Uri.EscapeDataString(appid) + "?token=" + Uri.EscapeDataString(Token!);
+	}
```

### 6.2 `GabLuchi.Services/GabLuchiApiClient.cs` — add fix downloader

```diff
 	public async Task<DenuvoFixesResponse?> GetDenuvoFixesAsync(string appid, CancellationToken ct = default(CancellationToken))
 	{
 		HttpResponseMessage httpResponseMessage = await _http.GetAsync("/api/denuvo/fixes?appid=" + Uri.EscapeDataString(appid), ct);
 		if (!httpResponseMessage.IsSuccessStatusCode)
 			return null;
 		return await ReadJsonAsync<DenuvoFixesResponse>(httpResponseMessage, ct);
 	}
+
+	public async Task<DownloadedFile> GetDenuvoFixDownloadAsync(string appid, string fixFileName, string? fixLicenseUrl, IProgress<double?>? progress, CancellationToken ct = default(CancellationToken))
+	{
+		if (string.IsNullOrWhiteSpace(fixLicenseUrl))
+			throw new ApiException("License required to download this fix.");
+		HttpResponseMessage res = await _http.GetAsync(fixLicenseUrl, HttpCompletionOption.ResponseHeadersRead, ct);
+		if (!res.IsSuccessStatusCode)
+			throw new ApiException($"Fix download failed ({(int)res.StatusCode}).", res.StatusCode);
+		string? url = null;
+		try
+		{
+			DenuvoDownloadResponse? d = JsonSerializer.Deserialize<DenuvoDownloadResponse>(await res.Content.ReadAsStringAsync(ct), JsonOpts);
+			url = string.IsNullOrWhiteSpace(d?.Url) ? null : d.Url;
+		}
+		catch { }
+		if (string.IsNullOrWhiteSpace(url))
+			throw new ApiException("Fix download failed (no URL returned).");
+		return await FetchAsync(url, fixFileName, progress, ct);
+	}
+
+	private async Task<DownloadedFile> FetchAsync(string url, string fallbackName, IProgress<double?>? progress, CancellationToken ct)
+	{
+		using HttpResponseMessage fileRes = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
+		if (!fileRes.IsSuccessStatusCode)
+			throw new ApiException($"Download failed ({(int)fileRes.StatusCode}).", fileRes.StatusCode);
+		return await SaveResponseAsync(fileRes, fallbackName, progress, ct);
+	}
```

(`SaveResponseAsync` already exists in this file; `DenuvoDownloadResponse` is already in
`GabLuchi.Models`.)

### 6.3 `GabLuchi.ViewModels/FixesViewModel.cs` — wire the fix slot + harden `ApplyFix`

a) Constructor: inject `CrackFixService` (already exists in DI) so both tabs share one
7za extraction helper and password policy.

```diff
 	private readonly SteamLibraryService library;
 	private readonly CoverCache covers;
 	private readonly ToastService toast;
 	private readonly SettingsService settings;
+	private readonly CrackFixService crackFix;
+	private readonly LicenseService license;

-	public FixesViewModel(GabLuchiApiClient api, ManifestDownloader manifestDownloader, LuaInstaller installer, SteamService steam, SteamLibraryService library, CoverCache covers, ToastService toast, SettingsService settings)
+	public FixesViewModel(GabLuchiApiClient api, ManifestDownloader manifestDownloader, LuaInstaller installer, SteamService steam, SteamLibraryService library, CoverCache covers, ToastService toast, SettingsService settings, CrackFixService crackFix, LicenseService license)
 	{
 		// …existing assignments…
+		this.crackFix = crackFix;
+		this.license = license;
 	}
```

b) `RunDownload` — fix slot uses the new fix downloader:

```diff
 	private async Task RunDownload(FixItemVm fix, string slot)
 	{
 		if (IsBusy) return;
 		FixGameCardVm game = SelectedGame;
 		if (game == null || !long.TryParse(game.AppId, out var appId)) return;
 		IsBusy = true;
 		IsProgressIndeterminate = true;
 		Progress = 0.0;
 		try
 		{
-			string fallbackName = ((slot == "manifest") ? (fix.ManifestFilename ?? (game.AppId + ".zip")) : (fix.FixFilename ?? (game.AppId + "_fix.zip")));
 			Progress<double?> progress = new Progress<double?>(delegate(double? p)
 			{
 				IsProgressIndeterminate = !p.HasValue;
 				if (p.HasValue) Progress = p.Value * 100.0;
 			});
 			DownloadedFile file;
 			string? localFix = FixRepository.Resolve(game.AppId, slot);
 			if (localFix != null)
 			{
 				file = new DownloadedFile(localFix, Path.GetFileName(localFix));
 			}
+			else if (slot == "fix")
+			{
+				file = await api.GetDenuvoFixDownloadAsync(
+					game.AppId,
+					fix.FixFilename ?? (game.AppId + "_fix.zip"),
+					license.GetFixDownloadUrl(game.AppId),
+					progress);
+			}
 			else
 			{
 				file = await manifestDownloader.DownloadManifestAsync(game.AppId, "Ryuu", game.Name, progress);
 			}
 			if (slot == "manifest") InstallManifest(file, appId, game.Name);
 			else ApplyFix(file, appId, game.Name);
 		}
 		// …catch/finally unchanged…
 	}
```

c) `ApplyFix` — 7za extraction (password, RAR/7z), `.bak` backups, tag-aware post-apply.
Replace the whole method body:

```diff
 	private void ApplyFix(DownloadedFile file, long appId, string gameName)
 	{
 		string installDir = library.GetInstallDir(appId);
 		if (installDir == null)
 		{
 			toast.Show(Strings.Fixes_Toast_GameNotFound, string.Format(Strings.Fixes_Toast_GameNotFound_Body, gameName), error: true);
 			return;
 		}
+		DenuvoFix? origin = _allFixes.FirstOrDefault(f => f.FixFilename == Path.GetFileName(file.FileName));
+		string tag = origin?.Tags.FirstOrDefault()?.Slug ?? "generic";
+		string password = CrackFixService.DefaultPassword; // cs.rin.ru — override from index (§5.1) when available
 		try
 		{
 			using ZipArchive zipArchive = ZipFile.OpenRead(file.FilePath);
 			int num = 0;
 			foreach (ZipArchiveEntry entry in zipArchive.Entries)
 			{
 				if (!string.IsNullOrEmpty(entry.Name))
 				{
 					string text = Path.Combine(installDir, entry.FullName);
 					try
 					{
 						Directory.CreateDirectory(Path.GetDirectoryName(text));
+						if (File.Exists(text))
+							File.Copy(text, text + ".bak", overwrite: true);
 						entry.ExtractToFile(text, overwrite: true);
 					}
 					catch { num++; }
 				}
 			}
 			if (num > 0) { toast.Show(Strings.Fixes_Toast_PartiallyApplied, …); }
 			else if (tag == "denuvowo") ShowDenuvoHvbChecklist(gameName);
 			else if (tag == "voices38-crack") ShowVoices38Checklist(gameName);
 			else toast.Show(Strings.Fixes_Toast_FixApplied, …);
 		}
+		catch (InvalidDataException)
+		{
+			// Scene fixes are often RAR or password zips → route through 7za.
+			bool ok = crackFix.ApplyArchiveToGame(file.FilePath, installDir, password, backup: true);
+			if (!ok) toast.Show(Strings.Fixes_Toast_CouldntApply, "Extraction failed (7za.exe missing or bad password).", error: true);
+			else if (tag == "denuvowo") ShowDenuvoHvbChecklist(gameName);
+			else if (tag == "voices38-crack") ShowVoices38Checklist(gameName);
+			else toast.Show(Strings.Fixes_Toast_FixApplied, string.Format(Strings.Fixes_Toast_FixApplied_Body, gameName));
+		}
 		catch (Exception ex)
 		{
 			toast.Show(Strings.Fixes_Toast_CouldntApply, ex.Message, error: true);
 		}
 		finally { DeleteStaged(file.FilePath); }
 	}
+
+	private void ShowDenuvoHvbChecklist(string gameName)
+	{
+		toast.Show(Strings.Fixes_Toast_FixApplied, gameName + " files applied. DenuvOwO needs setup before launch:");
+		// Full flow in DENUVOWO_GUIDE.md. In-app post-apply steps:
+		// 1) Reboot into disabled driver-signing enforcement (Advanced Startup → Driver signature enforcement: Disable).
+		// 2) Run setup/installer from the game folder AS ADMIN — choose the driver matching your CPU brand.
+		// 3) BIOS: enable SVM (AMD) / VT-x (Intel) + VT-d, if not already.
+		// 4) Add the game folder to Windows Defender exclusions first.
+		// 5) Run VBS.cmd from the game folder; follow it, then launch from Steam.
+		// 6) After playing, run VBS.cmd again to revert the system changes.
+	}
+
+	private void ShowVoices38Checklist(string gameName)
+	{
+		toast.Show(Strings.Fixes_Toast_FixApplied, gameName + " files applied.");
+		// voices38:
+		// 1) Restart Steam so the pinned manifest install is detected, then verify the game files.
+		// 2) The fix works only when the game build matches the release the crack was made for.
+		// 3) Achievements require the original Steam DLLs + SteamTools/OpenSteamTools running.
+	}
```

(UI strings: put the checklist text in `Strings.resx` and reference the keys; shown above
as inline for clarity. `DenuvoFix` already carries `Tags` with `Slug` — map
`denuvowo` / `voices38-crack` exactly as served by the API.)

### 6.4 `GabLuchi.Services/CrackFixService.cs` — extraction helper + bug fixes

a) `ApplyArchiveToGame` (shared helper the Fixes tab now calls):

```diff
+	public const string DefaultPassword = "cs.rin.ru";
+	public const string OnlineFixPassword = "online-fix.me";
+
+	public bool ApplyArchiveToGame(string archivePath, string gameDir, string password, bool backup)
+	{
+		if (!Is7ZipAvailable)
+			return false;
+		string extractDir = Path.Combine(Path.GetTempPath(), "GabLuchi", "crackfix_extract_" + Guid.NewGuid().ToString("N"));
+		Directory.CreateDirectory(extractDir);
+		try
+		{
+			var psi = new System.Diagnostics.ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "7za.exe"))
+			{
+				UseShellExecute = false,
+				CreateNoWindow = true,
+				WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
+				Arguments = string.Format("x -y -o\"{0}\" -p{1} \"{2}\"", extractDir, password, archivePath)
+			};
+			using var p = System.Diagnostics.Process.Start(psi)!;
+			p.WaitForExit();
+			if (p.ExitCode != 0) return false;
+			return ApplyToGameDirectory(extractDir, gameDir, backup);
+		}
+		finally { try { Directory.Delete(extractDir, recursive: true); } catch { } }
+	}
```

b) Fix the buzzheavier misroute (this is the §7 hygiene bug):

```diff
-		// Previously: BuzzheavierRegex matched a buzzheavier ID, then the ID was used
-		// against PixeldrainApiBase — wrong host, download can never succeed.
+		// Buzzheavier hits keep their own host; only pixeldrain IDs go to PixeldrainApiBase.
+		private string ResolveFileUrl(string rawUrl)
+		{
+			if (rawUrl.Contains("pixeldrain.com", StringComparison.OrdinalIgnoreCase))
+				return rawUrl;
+			if (BuzzheavierRegex.IsMatch(rawUrl))
+			{
+				string id = BuzzheavierRegex.Match(rawUrl).Groups[1].Value;
+				return "https://buzzheavier.com/f/" + id;
+			}
+			return rawUrl;
+		}
```

c) Password per source — `CrackFixDb` entries already carry a password shortcut from
`index.json`/`crackfiles.json`:

```diff
-		string password = ZipPassword; // "cs.rin.ru"
+		// gabluchi-fixes entries demand online-fix.me; third-party indexes cs.rin.ru.
+		string password = entry.Password ?? ZipPassword;
```

d) `DownloadFixAsync` — surface `7za` absence instead of silent no-op:

```diff
 	if (!Is7ZipAvailable)
-		return filePath; // silently unextracted → ApplyToGame copies nothing
+		throw new ApiException("7za.exe not found next to GabLuchi.exe — cannot extract scene archives.");
```

---

## 7. Rollout / verification checklist

Client build:
- [ ] `dotnet build` clean after applying §6.
- [ ] Ship `7za.exe` next to `GabLuchi.exe` (check your installer step includes the copy).

Backend:
- [ ] `denuvo.json` + `fixes/denuvo/*` added to `Gab-lutang/gabluchi-fixes`.
- [ ] Worker `/fix/{appid}` route live; verify with a **valid** key:
      `curl -s "https://gabluchi-proxy.freestuffsyeah65.workers.dev/fix/3321460?token=<KEY>"` → `{"url":"https://raw.githubusercontent.com/Gab-lutang/gabluchi-fixes/main/fixes/denuvo/3321460.zip"}`.

Functional (your machine, your key):
- [ ] **voices38 (Hogwarts Legacy, 990080):** press Manifest (Steam restarts) → then Fix.
      Confirm the downloaded interim file is **not** the same bytes as the `.lua` zip,
      that `steam_api64.dll` / crack files land in the game dir (`.bak` of the originals
      exists), and the game launches.
- [ ] **DenuvOwO (Crimson Desert, 3321460):** door gate shows risk text → accept → files
      applied → checklist appears → `VBS.cmd` path → game launches from Steam with the
      unsigned driver, and reverts with `VBS.cmd` after quitting.
- [ ] **Crack fixes tab:** a `gabluchi-fixes` entry (e.g. FH5) downloads and extracts
      with the `online-fix.me` password; a buzzheavier entry resolves via its own host.

Negative tests:
- [ ] Wrong build / no `.bak` present → voices38 toast warns instead of claiming success.
- [ ] `7za.exe` missing → both tabs error visibly, no false "DONE".

---

## 8. Operational notes

- **Dead code to delete:** the `fallbackName` computation in `RunDownload`.
- **`DenuvoDownloadResponse`** becomes used (route contract) — keep the model as-is.
- **No informed-choice bypass:** the DenuvOwO gate stays mandatory (decision-log rev 9;
  `DENUVOWO_GUIDE.md`). This patch wires the *mechanics*, it does not change the
  high-risk workflow posture.
- **`gabluchi-fixes` password duality:** hardcodes in the app stay per-entry (6.4c);
  never merge the two password pools.
- Two-roads model preserved: `DenuvOwO` and `voices38 (crack)` remain separate tags,
  separate hosting folders, separate post-apply flows.

---

## 9. Appendix — per-fix expected-file validation (rev 11)

`denuvo.json` entries may carry an optional `expectedFiles` list. When present,
PostPublishValidation checks these instead of a generic "any crack DLL landed"
heuristic — HV packages are multi-binary and per-generation.

Per-road expected shapes:

| Tag | Generation | Entry point | Expected files (subfolder-aware) |
|---|---|---|---|
| `DenuvOwO` | V3 (current) | `hypervisor-launcher.exe` | `hyperkd.sys` (Intel) **or** `SimpleSvm.sys` (AMD), `hyperhv.dll`?, `hyperevade.dll`?, `VBS.cmd`, `watchdog.exe` ?, `.org` proxy pair if present |
| `DenuvOwO` | V2 (MKDEV-style) | `steamclient_loader_x64.exe` | `coldclient/steamclient*.dll`, `ColdClientLoader.ini`?, `KIRIGIRI.dll` |
| `voices38` | direct crack | game exe | crack DLLs in game root/subfolder (per game, e.g. `steam_api64.dll`, `not_steam_api64.dll`) |

Validation rules:

- **Never require `KIRIGIRI.bin`** — runtime-created on first launch (6,247 B, CPU-vendor-specific); absence does not mean the fix is missing.
- Resolve the game's **actual runtime folder** (Crimson Desert → `bin64`) before expecting files; a fix is only "applied" when the entry point + its immediate siblings are present *in that folder*.
- `.org` pairs are intentional: `amd_ags_x64.org` (renamed original) + replacement proxy `.dll` must **both** exist — treat a lone proxy (no `.org`) as a partial/broken apply.
- AMD vs Intel: a zip containing only `SimpleSvm.sys` is still valid on AMD-only rigs; tag the message "driver present for AMD — Intel users download the Intel variant" instead of failing the check.
- **Tag ≠ method (rev 11.1):** dual-road games exist — e.g. 007 First Light has both a DenuvOwO HV bypass (1.0.5, build 23685521) and voices38 cracks in the scene, and the feed may tag only one side. Validation must key off the *release* (per-fix NFO/manifest entry), never assume the tag implies the file shape.
- On failure, toast lists the **missing file name**, mirroring the existing `.bak`-guard message (PATCH §6).