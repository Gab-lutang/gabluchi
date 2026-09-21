# GabLuchi — Build & Auto-Update Process

This document covers the full build-to-auto-update pipeline so any developer (or AI assistant) can ship a release that auto-updates on user machines.

---

## 1. Project Overview

- **Language**: C# WPF, .NET 8 (`net8.0-windows`)
- **GitHub repo**: `Gab-lutang/gabluchi`
- **Release branch**: `main`
- **Update framework**: Velopack v1.2.0
- **Output type**: WinExe (self-contained, no .NET runtime required on user machines)

---

## 2. Version — Two Files Must Stay in Sync

Version is hardcoded in **two files**. Both must match, or Velopack won't detect the update.

| File | What to set |
|------|-------------|
| `GabLuchi.csproj` (lines 10-12) | `<Version>`, `<AssemblyVersion>`, `<FileVersion>` |
| `Properties/AssemblyInfo.cs` (lines 10-11, 17) | `AssemblyFileVersion`, `AssemblyInformationalVersion`, `AssemblyVersion` |

Example for version `1.0.24`:

**csproj:**
```xml
<Version>1.0.24</Version>
<AssemblyVersion>1.0.24.0</AssemblyVersion>
<FileVersion>1.0.24.0</FileVersion>
```

**AssemblyInfo.cs:**
```csharp
[assembly: AssemblyFileVersion("1.0.24.0")]
[assembly: AssemblyInformationalVersion("1.0.24")]
[assembly: AssemblyVersion("1.0.24.0")]
```

> The csproj has `GenerateAssemblyInfo=False`, so AssemblyInfo.cs is the source of truth. Both files must agree.

---

## 3. Full Clean Build

**ALWAYS do a clean build.** WPF XAML is compiled into the DLL — cached `bin/obj` folders cause stale DLLs with old XAML/code.

```powershell
# Clean ALL build artifacts
Get-ChildItem -Recurse -Directory -Filter "bin" -ErrorAction SilentlyContinue | ForEach-Object { Remove-Item $_.FullName -Recurse -Force }
Get-ChildItem -Recurse -Directory -Filter "obj" -ErrorAction SilentlyContinue | ForEach-Object { Remove-Item $_.FullName -Recurse -Force }
Remove-Item "Release" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "Releases" -Recurse -Force -ErrorAction SilentlyContinue

# Build
dotnet publish -c Release -o Release/publish
```

**Verify the DLL** after building:
```powershell
$bytes = [System.IO.File]::ReadAllBytes("Release\publish\GabLuchi.dll")
$text = [System.Text.Encoding]::UTF8.GetString($bytes)
# Check that removed features are gone:
if ($text -match "RemovedFeature") { Write-Host "FAIL" } else { Write-Host "OK" }
```

---

## 4. Velopack Packaging

```powershell
vpk pack --packId GabLuchi --packVersion X.X.X --packDir "Release\publish" --mainExe GabLuchi.exe
```

This creates files in `Releases/`:
- `GabLuchi-X.X.X-full.nupkg`
- `GabLuchi-win-Setup.exe`
- `GabLuchi-win-Portable.zip`
- `RELEASES`
- `releases.win.json`
- `assets.win.json`

---

## 5. CRITICAL: Strip UTF-8 BOM from RELEASES

`vpk pack` generates `RELEASES` with a UTF-8 BOM (`EF BB BF`). Velopack v1.2.0 **cannot read it** — auto-update will fail silently.

**Strip the BOM after every pack:**
```powershell
$path = "Releases\RELEASES"
$bytes = [System.IO.File]::ReadAllBytes($path)
if ($bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191) {
    $noBom = $bytes[3..($bytes.Length - 1)]
    [System.IO.File]::WriteAllBytes($path, $noBom)
    Write-Host "BOM stripped"
}
```

**Verify `releases.win.json`** contains the correct version:
```powershell
Get-Content "Releases\releases.win.json"
```
Must show matching SHA, filename, and version for the nupkg you just built.

---

## 6. GitHub Release — All 6 Files Required

Every GitHub release **must** include all 6 files. If `releases.win.json` is missing or wrong, auto-update fails.

```powershell
gh release create vX.X.X --repo Gab-lutang/gabluchi --title "vX.X.X" --notes "..." `
  "Releases\GabLuchi-X.X.X-full.nupkg" `
  "Releases\GabLuchi-win-Setup.exe" `
  "Releases\GabLuchi-win-Portable.zip" `
  "Releases\RELEASES" `
  "Releases\releases.win.json" `
  "Releases\assets.win.json"
```

**How Velopack finds updates**: It iterates through GitHub releases (newest → oldest), looking for `releases.win.json` in each. If found, it parses the version and compares to the installed version. If the release asset is missing `releases.win.json`, Velopack skips it.

---

## 7. Auto-Update Architecture

There are **two update paths**: startup check and force update polling.

### Path 1: Startup Check

```
Startup (App.cs)
  ├─ CheckForceUpdateOnStartupAsync()    ← checks /api/update-check on launch
  │    └─ if forceUpdate=true → RunUpdateFlowAsync()
  └─ RunUpdateFlowAsync()
       └─ UpdateService.CheckAndStageAsync()
            ├─ GithubSource uses GitHub PAT from %AppData%\GabLuchi\github_token.txt
            ├─ ProxiedFileDownloader tries: direct GitHub → ghproxy.net → ghfast.top → gh.ddlc.top
            ├─ Downloads GabLuchi-X.X.X-full.nupkg
            └─ Stages the update
       └─ If staged → ApplyAndRestart(["--minimized"])
            ├─ Velopack installs the update
            └─ Restarts the app with new version
```

### Path 2: Force Update (while running)

```
ForceUpdateService (IHostedService, polls every 5 min)
  └─ GET /api/update-check
       ├─ forceUpdate=false → no-op
       └─ forceUpdate=true → RunUpdateFlowAsync() (same flow as above)
```

Triggered by admin running `/force-update` on Discord → sets `force_update=true` in `heartbeat` table.

### GitHub Token (Rate Limit Fix)

Unauthenticated GitHub API calls are limited to **60/hour per IP**. The app uses a Personal Access Token to raise this to **5,000/hour**.

- **Token location**: `%AppData%\GabLuchi\github_token.txt`
- **NEVER commit the token to git** — GitHub push protection will block the push
- `AppConfig.GithubToken` reads from this file at runtime (cached after first read)
- `GithubSource` in `UpdateService.cs` passes the token as the second constructor argument

### System Tray Mode

The app runs as a background process with a system tray icon:

- `ShutdownMode="OnExplicitShutdown"` in `App.xaml` — app doesn't exit when window closes
- Closing the window hides it to tray (`TrayIconHelper.cs`)
- Tray context menu: "Show GabLuchi" / "Exit"
- `--minimized` flag starts directly in tray (no window flash)
- All `IHostedService`s keep running in tray mode (HTTP server, Companion, CefInjector, HealthScanner, ForceUpdate)

### Mandatory Windows Startup

On every launch, `EnsureStartupRegistered()` writes to:
```
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\GabLuchi
  = "C:\...\GabLuchi.exe" --minimized
```
This is mandatory — no toggle. If the user removes it from Task Manager, next launch re-registers it.

### Key files:

| File | Purpose |
|------|---------|
| `GabLuchi/AppConfig.cs` | GitHub repo URL (`GithubReleasesRepos`), GitHub token (`GithubToken` from file), mirror URLs (`GithubDownloadMirrors`) |
| `GabLuchi.Services/UpdateService.cs` | Velopack `UpdateManager` with token, `CheckAndStageAsync()`, `ApplyAndRestart()`, `InstalledVersion` |
| `GabLuchi.Services/ForceUpdateService.cs` | `IHostedService` polling `/api/update-check` every 5 min |
| `GabLuchi.Services/ProxiedFileDownloader.cs` | `IFileDownloader` with mirror fallback via `GithubProxy.Candidates()` |
| `GabLuchi.Services/GithubProxy.cs` | Yields candidate URLs (original + mirrors) for any GitHub URL |
| `GabLuchi.Services/TrayIconHelper.cs` | System tray `NotifyIcon` with Show/Exit menu |
| `GabLuchi/App.cs` | `RunUpdateFlowAsync()`, `CheckForceUpdateOnStartupAsync()`, `EnsureStartupRegistered()`, tray wiring |
| `GabLuchi/Program.cs` | CLI args (`--minimized`), single-instance mutex, named events |

---

## 8. How Users Receive Updates

There are three ways updates are delivered:

### Startup auto-update (all users)
- Every launch → `RunUpdateFlowAsync()` → Velopack checks GitHub releases → if newer version found → downloads, stages, applies on next restart.
- v1.0.17+ works automatically. Older versions have the "chicken-and-egg" bug — users must manually install once.

### Force update (admin-triggered)
- Admin runs `/force-update` on Discord → sets `force_update=true` in `heartbeat` table
- Client's `ForceUpdateService` polls `/api/update-check` every 5 minutes
- When detected → same update flow as startup → auto-restarts with new version
- Useful for pushing critical fixes without waiting for users to restart

### Manual install
- Download Setup.exe from GitHub releases
- Used for first-time installs or when auto-update is broken (e.g., GitHub rate limiting before v1.2.7)

### After auto-update
Velopack stages the download, then calls `ApplyAndRestart(["--minimized"])`. The app restarts with the new version.

---

## 9. Commit & Push Workflow

```powershell
# Stage all changes
git add -A

# Commit with version in message
git commit -m "vX.X.X - description of changes"

# Push to main
git push origin main

# Tag and push tag
git tag vX.X.X
git push origin vX.X.X

# Create GitHub release with all assets
gh release create vX.X.X --repo Gab-lutang/gabluchi --title "vX.X.X" --notes "..." <6 files>
```

---

## 10. Common Pitfalls

### Cached builds
WPF XAML is compiled into the DLL. If you don't clean `bin/obj/publish`, the old XAML stays in the output. **Always do a full clean build.**

### BOM in RELEASES
Velopack can't parse the UTF-8 BOM. If auto-update silently fails, check `Releases/RELEASES` for a BOM header (`EF BB BF` at the start).

### releases.win.json version mismatch
If you reuse the file path without regenerating, it contains the OLD version data. Always verify content before uploading.

### AssemblyInfo.cs version drift
`csproj` and `AssemblyInfo.cs` MUST match. If they don't, the built DLL reports the wrong version and Velopack won't detect the update as newer.

### Tray-locked flag
`ApplyAndRestart` passes `["--minimized"]`. The `--minimized` flag starts the app in tray mode (no window shown).

### GitHub rate limits
Unauthenticated GitHub API calls are limited to 60/hour per IP. The app loads a PAT from `%AppData%\GabLuchi\github_token.txt` (5,000/hour). If the token file is missing, the app falls back to unauthenticated — fine for occasional checks, but will hit rate limits if the app restarts frequently. **Never commit the token to git.**

### GitHub push protection
GitHub blocks pushes containing Personal Access Tokens. If your push is rejected with `GH013: Push cannot contain secrets`, the token is hardcoded in the commit. Always load tokens from external files, never embed them in source code.

### GitHub release missing files
Velopack iterates releases looking for `releases.win.json`. If it's missing from a release, Velopack skips that release entirely and keeps searching older ones. If no release has it, auto-update fails with "No remote full releases found."

---

## 11. Quick Reference — Full Release Script

```powershell
# === VERSION ===
$VER = "1.0.24"
$VER0 = "$VER.0"

# === CLEAN ===
Get-ChildItem -Recurse -Directory -Filter "bin" -EA SilentlyContinue | % { Remove-Item $_.FullName -Recurse -Force }
Get-ChildItem -Recurse -Directory -Filter "obj" -EA SilentlyContinue | % { Remove-Item $_.FullName -Recurse -Force }
Remove-Item "Release" -Recurse -Force -EA SilentlyContinue
Remove-Item "Releases" -Recurse -Force -EA SilentlyContinue

# === VERSION BUMP (edit these files manually) ===
# GabLuchi.csproj: <Version>, <AssemblyVersion>, <FileVersion>
# Properties/AssemblyInfo.cs: AssemblyFileVersion, AssemblyInformationalVersion, AssemblyVersion

# === BUILD ===
dotnet publish -c Release -o Release/publish

# === PACK ===
vpk pack --packId GabLuchi --packVersion $VER --packDir "Release\publish" --mainExe GabLuchi.exe

# === STRIP BOM ===
$p = "Releases\RELEASES"
$b = [IO.File]::ReadAllBytes($p)
if ($b[0]-eq 239 -and $b[1]-eq 187 -and $b[2]-eq 191) {
    [IO.File]::WriteAllBytes($p, $b[3..($b.Length-1)])
}

# === VERIFY ===
Get-Content "Releases\releases.win.json"

# === COMMIT & PUSH ===
git add -A; git commit -m "v$VER - description"; git push origin main
git tag v$VER; git push origin v$VER

# === RELEASE ===
gh release create v$VER --repo Gab-lutang/gabluchi --title "v$VER" --notes "..." `
  "Releases\GabLuchi-$VER-full.nupkg" `
  "Releases\GabLuchi-win-Setup.exe" `
  "Releases\GabLuchi-win-Portable.zip" `
  "Releases\RELEASES" `
  "Releases\releases.win.json" `
  "Releases\assets.win.json"
```

---

## 12. Dev Workflow — Side-by-Side Testing

Dev builds install to a **separate folder** so they never conflict with the production install.

| Build | PackId | Install path | Channel | Velopack JSON |
|---|---|---|---|---|
| Production | `GabLuchi` | `%LocalAppData%\GabLuchi\` | `stable` | `releases.win.json` |
| Dev | `GabLuchi-Dev` | `%LocalAppData%\GabLuchi-Dev\` | `dev` | `releases.dev.json` |

Both auto-update independently from the same GitHub repo. Production users never see dev builds. Dev users never see production builds.

### Branch Strategy

| Branch | Purpose | Who merges |
|---|---|---|
| `main` | Production releases | LO, after dev testing passes |
| `dev` | Testing builds for LO + bro | LO pushes freely |

### Dev Build Script

```powershell
# === VERSION (use -dev.N suffix) ===
$VER = "1.0.24-dev.1"

# === CLEAN ===
Get-ChildItem -Recurse -Directory -Filter "bin" -EA SilentlyContinue | % { Remove-Item $_.FullName -Recurse -Force }
Get-ChildItem -Recurse -Directory -Filter "obj" -EA SilentlyContinue | % { Remove-Item $_.FullName -Recurse -Force }
Remove-Item "Release" -Recurse -Force -EA SilentlyContinue
Remove-Item "Releases" -Recurse -Force -EA SilentlyContinue

# === BUILD ===
dotnet publish -c Release -o Release/publish

# === PACK (note: --packId GabLuchi-Dev, NO --channel flag) ===
# DO NOT use --channel dev — Velopack 1.2.0 doesn't support .WithChannel()
# and it creates RELEASES-dev / releases.dev.json which the updater can't find
vpk pack --packId GabLuchi-Dev --packVersion $VER --packDir "Release\publish" --mainExe GabLuchi.exe

# === STRIP BOM from RELEASES ===
$p = "Release\RELEASES"
if (Test-Path $p) {
    $b = [IO.File]::ReadAllBytes($p)
    if ($b[0]-eq 239 -and $b[1]-eq 187 -and $b[2]-eq 191) {
        [IO.File]::WriteAllBytes($p, $b[3..($b.Length-1)])
    }
}

# === VERIFY ===
Get-Content "Release\releases.win.json"

# === COMMIT & PUSH to dev ===
git add -A; git commit -m "v$VER - description"; git push origin dev

# === TAG ===
git tag v$VER; git push origin v$VER

# === PRE-RELEASE (note: --prerelease flag) ===
# MUST include both RELEASES and releases.win.json for auto-updater to work
gh release create v$VER --repo Gab-lutang/gabluchi --title "v$VER" --prerelease --notes "Dev build" `
  "Release\GabLuchi-Dev-$VER-dev-full.nupkg" `
  "Release\GabLuchi-Dev-dev-Setup.exe" `
  "Release\GabLuchi-Dev-dev-Portable.zip" `
  "Release\RELEASES" `
  "Release\releases.win.json" `
  "Release\assets.win.json"
```

### Dev→Production Promotion

When dev testing passes and you're ready to ship:

```powershell
# Merge dev into main
git checkout main
git merge dev
git push origin main

# Then run the normal production release script (Section 11)
# The production build uses --packId GabLuchi (no suffix) and no --channel flag
```

### Key Rules

- **Dev version format**: `X.X.X-dev.N` (e.g., `1.0.24-dev.1`, `1.0.24-dev.2`)
- **Production version format**: `X.X.X` (e.g., `1.0.24`)
- **Dev releases are tagged `--prerelease`** on GitHub — shows as pre-release, not latest
- **Same GitHub repo** — both dev and prod releases go to the same repo
- **NEVER use `--channel dev` with vpk pack** — Velopack 1.2.0 creates suffixed files (RELEASES-dev) that the updater can't find. Just pack without channel and upload RELEASES + releases.win.json
- **Both users** (LO + bro) install the dev Setup.exe once → auto-updates to future dev releases
- **Production users** are unaffected — they only see `releases.win.json`

### First-Time Dev Setup (for bro)

1. Download the dev pre-release Setup.exe from GitHub
2. Run it — installs to `%LocalAppData%\GabLuchi-Dev\`
3. Done — future dev releases auto-update

To go back to production: uninstall `GabLuchi-Dev`, install from main release.

---

## 13. GabLuchi Connect — Relay Server

The lobby relay server enables multiplayer invites without Steam's broken Spacewar invite system.

### Architecture

```
Host GabLuchi → ws://relay → sends lobby info → gets 6-char code
Friend GabLuchi → ws://relay → sends code → gets host's IP + port
Relay pairs them, exchanges info, disconnects. No data stored.
```

### Relay Server Location

- **Source**: `GabLuchi/ConnectRelay/` (Go project)
- **Hosted on**: Render free tier (separate workspace)
- **URL**: Set in `GabLuchi.Services/ConnectRelayService.cs` → `RelayWsUrl`

### Relay Server Deploy

```bash
# From GabLuchi/ConnectRelay/
docker build -t gabluchi-connect .
# Test locally
docker run -p 8080:8080 gabluchi-connect
# Deploy to Render via GitHub (push ConnectRelay/ to a repo, connect Render)
```

### Relay Server Features

- WebSocket endpoint at `/ws`
- Health check at `/health`
- 6-char hex codes, expire in 5 minutes
- Rate limiting: 10 connections per IP per minute
- Stateless — no database, no logs, no persistence
- ~200 lines of Go

### Client Integration

- `GabLuchi.Services/ConnectRelayService.cs` — WebSocket client
- `GabLuchi.ViewModels/MultiplayerFixViewModel.cs` — Share/Join commands
- `GabLuchi.Views/MultiplayerFixView.xaml` — Connect with Friend UI section

### Privacy

- Relay sees: game name, app ID, IP, port (transient only)
- Relay does NOT see: Steam credentials, game files, personal data
- All data is ephemeral — codes expire in 5 minutes, no logging
- Open source — fully auditable

---

## Tiered Key System (v1.3.0)

### Key Types

| Type | Prefix | Limits | Expiry | DLC/Fixes |
|------|--------|--------|--------|-----------|
| **Paid** | `GABL-XXXX-XXXX-XXXX` | Unlimited | None | Full access |
| **Free** | `FREE-XXXX-XXXX-XXXX` | 2 downloads + 2 multiplayer/week | 7 days | Blocked |

### Backend Changes

| File | What Changed |
|------|-------------|
| `init.js` | `keys.tier` (default 'paid'), `keys.expires_at`, `usage_log` table, `machine_keys` table |
| `store.js` | `getWeeklyUsageCount()`, `logUsage()`, `saveMachineKey()`, `getAltUsers()`, `getFreeKeyForUser()` |
| `bot.js` | `/freekey` command, `POST /api/usage/track`, `/activate` returns tier+expiresAt + alt detection |

### Client Changes

| File | What Changed |
|------|-------------|
| `GabLuchi.Services/UsageService.cs` | **NEW** — tracks tier, expiry, weekly usage counts, `CheckUsageAsync()` calls `/api/usage/track` |
| `GabLuchi.Services/LicenseService.cs` | `IsValidKeyFormat()` accepts `FREE-` prefix, `ActivateAsync()` parses tier+expiresAt |
| `GabLuchi.Models/LicenseActivateResult.cs` | Added `Tier`, `ExpiresAt` properties |
| `GabLuchi.Models/LicenseAccount.cs` | Added `Tier`, `ExpiresAt`, `Keys[]` for account response |
| `GabLuchi.ViewModels/DownloadViewModel.cs` | Usage gate in `DownloadFromSourceAsync()` + `GenerateDlcAsync()` |
| `GabLuchi.Services/PluginAddService.cs` | Usage gate in `DownloadAsync()` |
| `GabLuchi.ViewModels/MultiplayerFixViewModel.cs` | Usage gate in `DownloadAndApply()` |
| `GabLuchi/MainWindow.cs` | DLC Unlocker + Fixes navigation wall for free users, ToastService injected |
| `GabLuchi.ViewModels/DownloadGamesViewModel.cs` | Exposes `IsFreeTier`, `UsageText` for usage counter |
| `GabLuchi.Views/DownloadGamesView.xaml` | Usage counter badge (bottom-right, free tier only) |
| `GabLuchi.ViewModels/LicenseGateViewModel.cs` | New error messages: `free-key-expired`, `alt-detected` |
| `GabLuchi/App.cs` | Registers `UsageService`, `ValidateTierOnStartupAsync()` checks tier on launch |

### Free Tier Behavior

1. User runs `/freekey` on Discord → gets `FREE-XXXX-XXXX-XXXX` key, expires in 7 days
2. Client activates key → backend returns `tier: "free"`, `expiresAt`
3. Client stores tier info in `UsageService`
4. On download: `UsageService.CheckUsageAsync("download")` → backend checks `usage_log` for current week
5. If limit hit (2/week): download blocked, toast shown
6. DLC Unlocker + Fixes: nav items visible but clicking shows "Paid Feature" toast
7. Multiplayer: gated at `DownloadAndApply()`
8. On startup: `ValidateTierOnStartupAsync()` checks expiry → toast if expired
9. Alt detection: if same machine activated by different Discord ID → block + alert modlog

### Database (Supabase)

Run these SQL commands:
```sql
ALTER TABLE keys ADD COLUMN IF NOT EXISTS tier text NOT NULL DEFAULT 'paid';
ALTER TABLE keys ADD COLUMN IF NOT EXISTS expires_at timestamptz;

CREATE TABLE IF NOT EXISTS usage_log (
  id bigserial primary key,
  machine_id text not null,
  discord_user_id text not null,
  action text not null,
  app_id bigint,
  used_at timestamptz not null default now()
);
CREATE INDEX IF NOT EXISTS idx_usage_log_user_week ON usage_log (discord_user_id, used_at);

CREATE TABLE IF NOT EXISTS machine_keys (
  id bigserial primary key,
  machine_id text not null,
  discord_user_id text not null,
  discord_tag text,
  activated_at timestamptz not null default now()
);
CREATE INDEX IF NOT EXISTS idx_machine_keys_machine ON machine_keys (machine_id);
```
